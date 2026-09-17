using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

/// <summary>
/// Programme → Communications "Reply in thread": sends the reply written in the portal from the
/// projects mailbox. Graph's createReplyAll keeps it in the original conversation — "RE:" subject,
/// thread headers, quoted history, original recipients — with the written reply above the quoted
/// history. No record is created in the background: the email is already filed in the programme
/// bucket, so the message simply carries the bucket's tag ("JPMS/SCH-&lt;projectRef&gt;") and the
/// thread's existing pathway, and the sent copy groups straight back into the Communications list.
/// The reply is scoped to the bucket — the email must currently carry the programme tag, so this
/// cannot reply to arbitrary mailbox messages. Staging, the send, the degrade back to a draft and
/// the audit row are the dispatcher's (OutboundEmailDispatcher).
/// </summary>
public sealed class SendProgrammeReplyHandler : ICommandHandler<SendProgrammeReply, ProgrammeReplyOutcome>
{
    private const string StagingRefused =
        "The reply couldn't be staged in the projects mailbox, so nothing was sent. The original "
        + "email may no longer be there, or the mailbox connection failed — check and try again.";

    private readonly RecordProviderRegistry providers;
    private readonly RecordEmailReader emails;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendProgrammeReplyHandler(
        RecordProviderRegistry providers,
        RecordEmailReader emails,
        OutboundEmailDispatcher dispatcher)
    {
        this.providers = providers;
        this.emails = emails;
        this.dispatcher = dispatcher;
    }

    public async Task<ProgrammeReplyOutcome> HandleAsync(SendProgrammeReply command, CancellationToken cancellationToken)
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

        // The written reply becomes the body sitting above the quoted history Graph supplies.
        // Plain text from the portal textarea is HTML-encoded line by line so nothing in it can
        // inject markup. No attachment; tagged so the sent copy groups back into the
        // Communications list.
        var message = new MailboxReplyDraftMessage(
            match.Id,
            HtmlCoverNote: ComposeHtmlPipeline.FromPlainText(reply),
            Attachments: Array.Empty<MailboxDraftAttachment>(),
            Categories: new[] { TriageCategories.Marker, tag, pathway });

        var filing = new OutboundEmailFiling(
            Audit.AuditTrail.PathwayLabel(pathway),
            StagingRefused,
            command.ProjectId,
            RecordType.Scheduling,
            command.ProjectId,
            bucket.Reference);

        var dispatch = await dispatcher.DispatchReplyAsync(message, filing, command.SaveAsDraftOnly, cancellationToken);
        return new ProgrammeReplyOutcome(
            command.ProjectId,
            dispatch.Subject,
            dispatch.To ?? Array.Empty<string>(),
            dispatch.WebLink,
            Cc: dispatch.Cc,
            Sent: dispatch.Sent,
            FailureNote: dispatch.FailureNote);
    }
}
