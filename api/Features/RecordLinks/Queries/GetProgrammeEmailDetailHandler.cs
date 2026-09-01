using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

/// <summary>
/// Full body + attachments for one email in a project's programme communications, read live and
/// sanitised before it leaves the server. Mirrors GetRequestEmailDetailHandler with the scheduling
/// bucket in place of the request: the message must be among the emails currently carrying the
/// bucket's tag (matched by Graph id or internetMessageId), otherwise an empty body is returned.
/// That membership check is what lets this endpoint sit behind the ordinary internal gate — unlike
/// the triage detail endpoint, it cannot be used to read arbitrary mailbox messages.
/// </summary>
public sealed class GetProgrammeEmailDetailHandler : IQueryHandler<GetProgrammeEmailDetail, MailboxMessageDetail>
{
    private readonly RecordEmailReader emails;
    private readonly IIntakeMessageReader reader;
    private readonly InboundEmailBodyBuilder bodyBuilder;

    public GetProgrammeEmailDetailHandler(
        RecordEmailReader emails, IIntakeMessageReader reader, InboundEmailBodyBuilder bodyBuilder)
    {
        this.emails = emails;
        this.reader = reader;
        this.bodyBuilder = bodyBuilder;
    }

    public async Task<MailboxMessageDetail> HandleAsync(GetProgrammeEmailDetail query, CancellationToken cancellationToken)
    {
        var empty = new MailboxMessageDetail(query.MessageId, "", false, Array.Empty<IntakeAttachment>());
        if (string.IsNullOrEmpty(query.ProjectId) || string.IsNullOrEmpty(query.MessageId))
            return empty;

        // Membership check: the email must currently carry the programme bucket's tag. Also resolves
        // the live Graph id — the list may have been rendered a while ago, so re-find by
        // internetMessageId when the Graph id no longer matches.
        var tagged = await emails.ForRecordAsync(RecordType.Scheduling, query.ProjectId, cancellationToken);
        var match = tagged.FirstOrDefault(e =>
            string.Equals(e.Id, query.MessageId, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(query.InternetMessageId)
                && string.Equals(e.InternetMessageId, query.InternetMessageId, StringComparison.Ordinal)));
        if (match is null)
            return empty;

        var content = await reader.GetAsync(match.Id, cancellationToken);
        if (content is null)
            return empty;

        var body = await bodyBuilder.BuildAsync(match.Id, content, cancellationToken);
        var attachments = content.Attachments
            .Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType, a.Id))
            .ToList()
            .AsReadOnly();

        return new MailboxMessageDetail(match.Id, body, content.IsHtml, attachments);
    }
}
