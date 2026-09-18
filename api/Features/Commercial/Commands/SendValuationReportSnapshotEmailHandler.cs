using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Emails the frozen valuation report to the project's client side from the shared projects
/// mailbox. The snapshot is the only client-facing form of the report, so the thread is born on the
/// Client pathway and files itself under the snapshot; the PDF comes from the shared builder, so the
/// attachment is byte-for-byte what the download endpoint streams. Staging, sending, the degrade to
/// a draft and the audit row are the dispatcher's (OutboundEmailDispatcher).
/// </summary>
public sealed class SendValuationReportSnapshotEmailHandler
    : ICommandHandler<SendValuationReportSnapshotEmail, ValuationReportSnapshotEmailOutcome>
{
    private const string StagingRefused =
        "The valuation email couldn't be staged in the shared mailbox, so nothing was sent. "
        + "Check the mailbox connection and try again.";

    private static readonly int[] ClientSideRoles =
        { (int)ProjectContactRole.Client, (int)ProjectContactRole.Architect };

    private readonly ValuationReportSnapshotPdfBuilder builder;
    private readonly JpmsContext context;
    private readonly OutboundEmailDispatcher dispatcher;

    public SendValuationReportSnapshotEmailHandler(
        ValuationReportSnapshotPdfBuilder builder, JpmsContext context, OutboundEmailDispatcher dispatcher)
    {
        this.builder = builder; this.context = context; this.dispatcher = dispatcher;
    }

    public async Task<ValuationReportSnapshotEmailOutcome> HandleAsync(
        SendValuationReportSnapshotEmail command, CancellationToken cancellationToken)
    {
        var pdf = await builder.BuildAsync(command.ValuationReportSnapshotId, cancellationToken);
        var recipients = await ClientSideRecipientsAsync(pdf.ProjectId, cancellationToken);

        // The statement's own tag, spelt exactly as the register spells it, so the sent copy and
        // the client's reply to it file under this statement rather than only under Client.
        var recordTag = await ValuationSnapshotTags.StemAsync(
            context, pdf.ProjectId, pdf.Snapshot.Number, cancellationToken);

        var message = new MailboxDraftMessage(
            To: recipients,
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: new[] { new MailboxDraftAttachment(pdf.FileName, "application/pdf", pdf.Content) },
            Categories: new List<string>
            {
                TriageCategories.Marker,
                TriageCategories.ForRecord(recordTag),
                TriageCategories.Client
            });

        var filing = new OutboundEmailFiling(
            AuditTrail.PathwayLabel(TriageCategories.Client),
            StagingRefused,
            pdf.ProjectId,
            RecordType.ValuationReportSnapshot,
            pdf.Snapshot.ValuationReportSnapshotId,
            pdf.Snapshot.Label);

        var dispatch = await dispatcher.DispatchAsync(message, filing, command.SaveAsDraftOnly, cancellationToken);
        return new ValuationReportSnapshotEmailOutcome(
            pdf.Snapshot.ValuationReportSnapshotId,
            pdf.Snapshot.Label,
            command.Subject,
            recipients.Select(recipient => recipient.Email).ToList(),
            dispatch.WebLink,
            dispatch.MessageId,
            dispatch.Sent,
            dispatch.FailureNote);
    }

    /// <summary>The client side of the correspondence profile — Client and Architect rows with an
    /// email, deduped by address in case the same person is on the profile twice.</summary>
    private async Task<List<MailboxDraftRecipient>> ClientSideRecipientsAsync(string projectId, CancellationToken cancellationToken)
    {
        var contacts = await context.ProjectContacts.AsNoTracking()
            .Where(contact => contact.ProjectId == projectId && ClientSideRoles.Contains(contact.Role) && contact.Email != "")
            .OrderBy(contact => contact.Role).ThenBy(contact => contact.Name)
            .ToListAsync(cancellationToken);

        var recipients = contacts
            .GroupBy(contact => contact.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new MailboxDraftRecipient(group.Key, group.First().Name))
            .ToList();

        if (recipients.Count > 0) return recipients;
        throw new InvalidOperationException(
            "The project has no client or architect contact with an email address — add one to the project's contacts before emailing the valuation.");
    }
}
