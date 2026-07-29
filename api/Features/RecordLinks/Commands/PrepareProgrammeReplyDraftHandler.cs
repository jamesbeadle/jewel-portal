using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

/// <summary>
/// Programme → Communications "Reply in thread": stages the reply written in the portal as an
/// Outlook draft on the email in the projects mailbox. Graph's createReplyAll keeps the draft in
/// the original conversation — "RE:" subject, thread headers, quoted history, original recipients —
/// with the written reply sitting above the quoted history as the draft's body. Same mechanics as
/// the triage ReplyInThreadFromMessageHandler, but no record is created in the background: the
/// email is already filed in the programme bucket, so the draft simply carries the bucket's tag
/// ("JPMS/SCH-&lt;projectRef&gt;") and the thread's existing pathway, and the sent copy groups
/// straight back into the Communications list. The reply is scoped to the bucket — the email must
/// currently carry the programme tag, so this cannot stage replies on arbitrary mailbox messages.
/// Nothing is sent — a person reviews, adjusts recipients if needed, and sends from the mailbox
/// itself; the DraftCreated audit row records who staged it and where the draft went.
/// </summary>
public sealed class PrepareProgrammeReplyDraftHandler : ICommandHandler<PrepareProgrammeReplyDraft, ProgrammeReplyDraft>
{
    private readonly RecordProviderRegistry providers;
    private readonly RecordEmailReader emails;
    private readonly IMailboxGraphClient graph;
    private readonly Audit.AuditTrail audit;

    public PrepareProgrammeReplyDraftHandler(
        RecordProviderRegistry providers,
        RecordEmailReader emails,
        IMailboxGraphClient graph,
        Audit.AuditTrail audit)
    {
        this.providers = providers;
        this.emails = emails;
        this.graph = graph;
        this.audit = audit;
    }

    public async Task<ProgrammeReplyDraft> HandleAsync(PrepareProgrammeReplyDraft command, CancellationToken cancellationToken)
    {
        // The written reply is the whole point of this action — an empty draft helps no one.
        var reply = command.ReplyBody?.Trim() ?? "";
        if (reply.Length == 0)
            throw new InvalidOperationException("Write the reply before creating the draft.");

        // Resolve the project's programme bucket for its tag ("SCH-<projectRef>").
        if (!providers.TryGet(RecordType.Scheduling, out var provider))
            throw new InvalidOperationException("Programme communications are not available.");
        var bucket = await provider.FindAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        // Membership check: the email must currently carry the programme tag. Also resolves the
        // live Graph id — the list may have been rendered a while ago, so re-find by
        // internetMessageId when the Graph id no longer matches.
        var tagged = await emails.ForRecordAsync(RecordType.Scheduling, command.ProjectId, cancellationToken);
        var match = tagged.FirstOrDefault(e =>
            string.Equals(e.Id, command.MessageId, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(command.InternetMessageId)
                && string.Equals(e.InternetMessageId, command.InternetMessageId, StringComparison.Ordinal)));
        if (match is null)
            throw new InvalidOperationException(
                "That email is no longer in this project's programme communications — refresh and try again.");

        var tag = TriageCategories.ForRecord(bucket.TagReference);

        // Keep the reply in the pathway the thread is already filed under (the client wall stays
        // intact); programme correspondence defaults to Client when the thread has no pathway yet.
        var pathway = match.Bucket ?? TriageCategories.BucketFor(RecordType.Scheduling)!;

        // Stage the reply draft with the written reply as its body, sitting above the quoted
        // history Graph supplies — the reader reviews and presses Send in Outlook, nothing more.
        // Plain text from the portal textarea is HTML-encoded line by line so nothing in it can
        // inject markup into the draft. No attachment; tagged so the sent copy groups back into
        // the Communications list.
        var created = await graph.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(
                match.Id,
                HtmlCoverNote: ToHtml(reply),
                Attachments: Array.Empty<MailboxDraftAttachment>(),
                Categories: new[] { TriageCategories.Marker, tag, pathway }),
            cancellationToken);
        if (created is null)
            throw new InvalidOperationException(
                "The reply draft couldn't be created in the projects mailbox. The original email may " +
                "no longer be there, or the mailbox connection failed — check and try again.");

        // Audit: the drafted reply, with its webLink, so it can be found in Outlook.
        await audit.WriteAsync(
            AuditEventType.DraftCreated,
            $"Programme reply drafted to \"{match.Subject}\" — awaiting review and send.",
            pathway: PathwayLabel(pathway),
            projectId: command.ProjectId,
            recordType: RecordType.Scheduling,
            recordId: command.ProjectId,
            recordReference: bucket.Reference,
            emailMessageId: created.Id,
            internetMessageId: match.InternetMessageId,
            webLink: created.WebLink,
            cancellationToken: cancellationToken);

        return new ProgrammeReplyDraft(
            command.ProjectId,
            created.Subject,
            created.To,
            created.WebLink,
            Cc: created.Cc);
    }

    // Portal textarea (plain text) -> draft HTML: encode each line, join with <br>, and leave a
    // blank line before the quoted history the cover note is prepended to.
    private static string ToHtml(string reply) =>
        "<div>"
        + string.Join("<br>", reply.Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";

    // "JPMS/Client" -> "Client": the audit column stores the pathway's display stem, as the
    // request-reply path does with its literal "Client".
    private static string PathwayLabel(string bucketTag) =>
        bucketTag.StartsWith(TriageCategories.WorkflowPrefix, StringComparison.OrdinalIgnoreCase)
            ? bucketTag[TriageCategories.WorkflowPrefix.Length..]
            : bucketTag;
}
