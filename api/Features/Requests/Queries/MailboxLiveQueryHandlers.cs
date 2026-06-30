using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>The triage queue, read live from the Inbox (messages not tagged triaged). No database.</summary>
public sealed class ListInboxMessagesHandler : IQueryHandler<ListInboxMessages, MailboxPage>
{
    private readonly IMailboxGraphClient graph;
    public ListInboxMessagesHandler(IMailboxGraphClient graph) { this.graph = graph; }

    public Task<MailboxPage> HandleAsync(ListInboxMessages query, CancellationToken cancellationToken) =>
        graph.ListInboxAsync(query.Cursor, query.Take, cancellationToken);
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
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType))
            .ToList()
            .AsReadOnly();

        return new MailboxMessageDetail(query.MessageId, body, content.IsHtml, attachments);
    }

    private static string Sanitise(string html) => new HtmlSanitizer().Sanitize(html);
}
