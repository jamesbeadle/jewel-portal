using Ganss.Xss;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Full body + attachments for one email in a request's conversation, read live and sanitised before
/// it leaves the server. The conversation list only carries Graph's short bodyPreview snippet (which
/// truncates long emails and drops the quoted thread), so the full content is fetched here on demand
/// when a reader expands a message.
///
/// Scoped to the request: the message must be among the emails currently tagged to the request
/// (matched by Graph id or internetMessageId), otherwise an empty body is returned. That check is
/// what lets this endpoint sit behind the ordinary signed-in gate — unlike the triage detail
/// endpoint, it cannot be used to read arbitrary mailbox messages.
/// </summary>
public sealed class GetRequestEmailDetailHandler : IQueryHandler<GetRequestEmailDetail, MailboxMessageDetail>
{
    private readonly RequestEmailReader emails;
    private readonly IIntakeMessageReader reader;

    public GetRequestEmailDetailHandler(RequestEmailReader emails, IIntakeMessageReader reader)
    {
        this.emails = emails;
        this.reader = reader;
    }

    public async Task<MailboxMessageDetail> HandleAsync(GetRequestEmailDetail query, CancellationToken cancellationToken)
    {
        var empty = new MailboxMessageDetail(query.MessageId, "", false, Array.Empty<IntakeAttachment>());
        if (string.IsNullOrEmpty(query.RequestId) || string.IsNullOrEmpty(query.MessageId))
            return empty;

        // Membership check: the email must currently carry the request's tag. Also resolves the live
        // Graph id — the conversation may have been rendered a while ago, so re-find by
        // internetMessageId when the Graph id no longer matches.
        var tagged = await emails.ForRequestAsync(query.RequestId, cancellationToken);
        var match = tagged.FirstOrDefault(e =>
            string.Equals(e.Id, query.MessageId, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(query.InternetMessageId)
                && string.Equals(e.InternetMessageId, query.InternetMessageId, StringComparison.Ordinal)));
        if (match is null)
            return empty;

        var content = await reader.GetAsync(match.Id, cancellationToken);
        if (content is null)
            return empty;

        var body = content.IsHtml ? new HtmlSanitizer().Sanitize(content.Body) : content.Body;
        var attachments = content.Attachments
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType))
            .ToList()
            .AsReadOnly();

        return new MailboxMessageDetail(match.Id, body, content.IsHtml, attachments);
    }
}
