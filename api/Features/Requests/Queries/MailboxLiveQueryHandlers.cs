using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>The triage queue, read live from the Inbox (messages not tagged triaged). No database.
/// Every untagged Inbox message queues — including a new reply to a thread that was already triaged:
/// each new arrival is its own triage decision (only triaging spreads a tag, and only across the
/// thread as it exists at that moment). Before the page is returned it is annotated: a queued email
/// whose conversation already carries record tags gets those tags as ThreadTags, so the UI can hint
/// "reply to an already-linked thread" without deciding anything on the triager's behalf. The
/// lookup is read-only, best-effort and cached, so a clean queue costs little.</summary>
public sealed class ListInboxMessagesHandler : IQueryHandler<ListInboxMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AutoReplySweeper autoReplies;
    public ListInboxMessagesHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger, AutoReplySweeper autoReplies)
    { this.graph = graph; this.threadTagger = threadTagger; this.autoReplies = autoReplies; }

    public async Task<MailboxPage> HandleAsync(ListInboxMessages query, CancellationToken cancellationToken)
    {
        var page = await ListSweptAsync(query, cancellationToken);

        var threadTags = await threadTagger.LookupThreadTagsAsync(page.Items, cancellationToken);
        if (threadTags.Count == 0)
            return page;

        var annotated = page.Items
            .Select(m => threadTags.TryGetValue(m.ConversationId, out var tags) ? m with { ThreadTags = tags } : m)
            .ToList();
        return new MailboxPage(annotated, page.NextCursor, page.Total);
    }

    // Automatic replies are discarded as they are met and the page re-read, because the inbox
    // cursor is an offset into the untagged set — serving the swept page as listed would skip
    // real emails on the next page. Bounded: a discard that keeps failing leaves its email in
    // the queue for a human rather than looping.
    private const int MaxSweepPasses = 3;

    private async Task<MailboxPage> ListSweptAsync(ListInboxMessages query, CancellationToken cancellationToken)
    {
        var page = await graph.ListInboxAsync(query.Cursor, query.Take, query.NewestFirst, cancellationToken);
        for (var pass = 0; pass < MaxSweepPasses; pass++)
        {
            var discarded = await autoReplies.SweepAsync(page, cancellationToken);
            if (discarded == 0) return page;
            page = await graph.ListInboxAsync(query.Cursor, query.Take, query.NewestFirst, cancellationToken);
        }
        return page;
    }
}

/// <summary>The discarded pile, read live from the Inbox (messages tagged discarded).</summary>
public sealed class ListDiscardedMessagesHandler : IQueryHandler<ListDiscardedMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListDiscardedMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListDiscardedMessages query, CancellationToken cancellationToken) =>
        graph.ListDiscardedAsync(query.Cursor, query.Take, query.NewestFirst, cancellationToken);
}

/// <summary>Every tagged email (JPMS marker), or — when a Tag is given — just that one workflow.</summary>
public sealed class ListTaggedMessagesHandler : IQueryHandler<ListTaggedMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListTaggedMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListTaggedMessages query, CancellationToken cancellationToken) =>
        query.Tags is { Count: > 0 } tags
            ? graph.ListByTagsAsync(tags, query.Cursor, query.Take, query.NewestFirst, cancellationToken)
            : graph.ListTaggedAsync(query.Cursor, query.Take, query.NewestFirst, cancellationToken);
}

/// <summary>An email's whole thread (every mailbox message sharing its Graph conversation id — the
/// mailbox's own sent replies included, unsent drafts excluded), read live and regardless of tags —
/// backs the triage detail pane's thread panel, where later replies inform how the older messages
/// should be triaged.</summary>
public sealed class ListConversationMessagesHandler : IQueryHandler<ListConversationMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListConversationMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public async Task<MailboxPage> HandleAsync(ListConversationMessages query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.ConversationId))
            return new MailboxPage(Array.Empty<MailboxMessage>(), null, 0);
        var byConversation = await graph.ListConversationAsync(query.ConversationId, cancellationToken);
        if (byConversation.Items.Count > 1)
            return byConversation;
        return await ConversationBySubject.FindAsync(graph, byConversation, query.Subject, cancellationToken);
    }
}

/// <summary>
/// Full body + attachments for one mailbox message, read live and sanitised before it leaves the
/// server. Reuses the existing on-demand message reader; the id is fresh (the list was just rendered)
/// so no re-find is needed — if it can't be read we return an empty body rather than failing.
/// </summary>
public sealed class GetMailboxMessageDetailHandler : IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>
{
    private readonly IIntakeMessageReader reader;
    private readonly MailboxIntakeOptions options;
    private readonly InboundEmailBodyBuilder bodyBuilder;
    public GetMailboxMessageDetailHandler(
        IIntakeMessageReader reader, MailboxIntakeOptions options, InboundEmailBodyBuilder bodyBuilder)
    { this.reader = reader; this.options = options; this.bodyBuilder = bodyBuilder; }

    public async Task<MailboxMessageDetail> HandleAsync(GetMailboxMessageDetail query, CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(query.MessageId)
            ? null
            : await reader.GetAsync(query.MessageId, cancellationToken);

        if (content is null)
            return new MailboxMessageDetail(query.MessageId, "", false, Array.Empty<IntakeAttachment>());

        var body = await bodyBuilder.BuildAsync(query.MessageId, content, cancellationToken);
        var attachments = content.Attachments
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType, a.Id))
            .ToList()
            .AsReadOnly();

        return new MailboxMessageDetail(
            query.MessageId, body, content.IsHtml, attachments,
            content.FromEmail, content.FromName, content.To, content.Cc, content.ReplyTo, content.Subject,
            MailboxAddress: string.IsNullOrWhiteSpace(options.Mailbox) ? null : options.Mailbox);
    }
}
