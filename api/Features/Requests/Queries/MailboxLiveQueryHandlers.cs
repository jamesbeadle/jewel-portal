using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>The triage queue, read live from the Inbox (messages not tagged triaged). No database.
/// Before the page is returned it is swept: a queued email whose conversation already carries a
/// record's tag (a reply to an already-triaged thread, or a thread whose sent copy was tagged)
/// inherits that tag and is dropped from the page — replies to triaged threads never sit in triage
/// waiting for a manual sync. The sweep is best-effort and cached, so a clean queue costs little.</summary>
public sealed class ListInboxMessagesHandler : IQueryHandler<ListInboxMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    public ListInboxMessagesHandler(IMailboxGraphClient graph, RecordThreadTagger threadTagger)
    { this.graph = graph; this.threadTagger = threadTagger; }

    public async Task<MailboxPage> HandleAsync(ListInboxMessages query, CancellationToken cancellationToken)
    {
        var page = await graph.ListInboxAsync(query.Cursor, query.Take, cancellationToken);

        var swept = await threadTagger.SweepQueuePageAsync(page.Items, cancellationToken);
        if (swept.Count == 0)
            return page;

        // Drop the swept conversations' messages locally rather than re-reading: the writes are
        // verified, but the category-filtered list is eventually consistent and could briefly
        // return them again. Total shrinks by the number dropped.
        var remaining = page.Items.Where(m => !swept.Contains(m.ConversationId)).ToList();
        return new MailboxPage(remaining, page.NextCursor, Math.Max(0, page.Total - (page.Items.Count - remaining.Count)));
    }
}

/// <summary>The discarded pile, read live from the Inbox (messages tagged discarded).</summary>
public sealed class ListDiscardedMessagesHandler : IQueryHandler<ListDiscardedMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListDiscardedMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListDiscardedMessages query, CancellationToken cancellationToken) =>
        graph.ListDiscardedAsync(query.Cursor, query.Take, cancellationToken);
}

/// <summary>Every tagged email (JPMS marker), or — when a Tag is given — just that one workflow.</summary>
public sealed class ListTaggedMessagesHandler : IQueryHandler<ListTaggedMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListTaggedMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListTaggedMessages query, CancellationToken cancellationToken) =>
        query.Tags is { Count: > 0 } tags
            ? graph.ListByTagsAsync(tags, query.Cursor, query.Take, cancellationToken)
            : graph.ListTaggedAsync(query.Cursor, query.Take, cancellationToken);
}

/// <summary>An email's whole thread (every mailbox message sharing its Graph conversation id — the
/// mailbox's own sent replies included, unsent drafts excluded), read live and regardless of tags —
/// backs the triage detail pane's thread panel, where later replies inform how the older messages
/// should be triaged.</summary>
public sealed class ListConversationMessagesHandler : IQueryHandler<ListConversationMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListConversationMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListConversationMessages query, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(query.ConversationId)
            ? Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0))
            : graph.ListConversationAsync(query.ConversationId, cancellationToken);
}

/// <summary>
/// Full body + attachments for one mailbox message, read live and sanitised before it leaves the
/// server. Reuses the existing on-demand message reader; the id is fresh (the list was just rendered)
/// so no re-find is needed — if it can't be read we return an empty body rather than failing.
/// </summary>
public sealed class GetMailboxMessageDetailHandler : IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail>
{
    private readonly IIntakeMessageReader reader;
    public GetMailboxMessageDetailHandler(IIntakeMessageReader reader) { this.reader = reader; }

    public async Task<MailboxMessageDetail> HandleAsync(GetMailboxMessageDetail query, CancellationToken cancellationToken)
    {
        var content = string.IsNullOrEmpty(query.MessageId)
            ? null
            : await reader.GetAsync(query.MessageId, cancellationToken);

        if (content is null)
            return new MailboxMessageDetail(query.MessageId, "", false, Array.Empty<IntakeAttachment>());

        var body = content.IsHtml ? Sanitise(content.Body) : content.Body;
        var attachments = content.Attachments
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType, a.Id))
            .ToList()
            .AsReadOnly();

        return new MailboxMessageDetail(query.MessageId, body, content.IsHtml, attachments);
    }

    private static string Sanitise(string html) => new HtmlSanitizer().Sanitize(html);
}
