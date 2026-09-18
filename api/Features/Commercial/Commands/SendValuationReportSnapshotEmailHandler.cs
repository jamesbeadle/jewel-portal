using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Emails the frozen valuation report to the project's client side from the shared projects
/// mailbox. The snapshot is the only client-facing form of the report, so the thread is born on the
/// Client pathway and files itself under the snapshot; the PDF comes from the shared builder, so
/// the attachment is byte-for-byte what the download endpoint streams.
///
/// The message is <see cref="ValuationSnapshotEmailComposer"/>'s, shared with the preview, so what
/// a person reads before pressing Send is what leaves. Staging, sending, the degrade to a draft and
/// the audit row are the dispatcher's.
/// </summary>
public sealed class SendValuationReportSnapshotEmailHandler
    : ICommandHandler<SendValuationReportSnapshotEmail, ValuationReportSnapshotEmailOutcome>
{
    private const string StagingRefused =
        "The valuation email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private readonly ValuationSnapshotEmailComposer composer;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendValuationReportSnapshotEmailHandler(
        ValuationSnapshotEmailComposer composer, OutboundEmailDispatcher dispatcher)
    {
        this.composer = composer;
        this.dispatcher = dispatcher;
    }

    public async Task<ValuationReportSnapshotEmailOutcome> HandleAsync(
        SendValuationReportSnapshotEmail command, CancellationToken cancellationToken)
    {
        var composed = await composer.ComposeAsync(
            new RecordEmailDraft(
                command.ValuationReportSnapshotId, Subject: command.Subject, BodyHtml: command.HtmlBody),
            cancellationToken);

        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client),
            StagingRefused,
            composed.ProjectId,
            RecordType.ValuationReportSnapshot,
            command.ValuationReportSnapshotId,
            composed.Reference);

        var dispatch = await dispatcher.DispatchAsync(
            composed.Message, filing, command.SaveAsDraftOnly, cancellationToken);

        return new ValuationReportSnapshotEmailOutcome(
            command.ValuationReportSnapshotId,
            composed.Reference,
            composed.Message.Subject,
            composed.Message.To.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            dispatch.MessageId,
            dispatch.Sent,
            dispatch.FailureNote);
    }
}
