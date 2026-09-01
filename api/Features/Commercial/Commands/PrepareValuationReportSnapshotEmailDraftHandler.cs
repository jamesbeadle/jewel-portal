using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Drafts the valuation-report email in the shared mailbox — nothing is sent (same convention as
// PrepareSubcontractorStatementEmailDraft: the human sends from Outlook). Addressed To the
// project's Client and Architect contacts — the snapshot is the only client-facing form of the
// report, so it travels on the client pathway; the projects@ mailbox is cc'd automatically at the
// Graph-client chokepoint. The frozen report is attached as a PDF via the shared builder, so the
// attachment is byte-for-byte what the download endpoint streams.
public sealed class PrepareValuationReportSnapshotEmailDraftHandler
    : ICommandHandler<PrepareValuationReportSnapshotEmailDraft, ValuationReportSnapshotEmailDraft>
{
    private readonly ValuationReportSnapshotPdfBuilder builder;
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient mailbox;

    public PrepareValuationReportSnapshotEmailDraftHandler(
        ValuationReportSnapshotPdfBuilder builder,
        JpmsContext context,
        IMailboxGraphClient mailbox)
    {
        this.builder = builder; this.context = context; this.mailbox = mailbox;
    }

    public async Task<ValuationReportSnapshotEmailDraft> HandleAsync(
        PrepareValuationReportSnapshotEmailDraft command, CancellationToken cancellationToken)
    {
        var pdf = await builder.BuildAsync(command.ValuationReportSnapshotId, cancellationToken);

        // The client side of the correspondence profile: Client and Architect rows with an email.
        // Deduped by address in case the same person is on the profile twice.
        var clientSideRoles = new[] { (int)ProjectContactRole.Client, (int)ProjectContactRole.Architect };
        var contacts = await context.ProjectContacts.AsNoTracking()
            .Where(contact => contact.ProjectId == pdf.ProjectId
                && clientSideRoles.Contains(contact.Role)
                && contact.Email != "")
            .OrderBy(contact => contact.Role).ThenBy(contact => contact.Name)
            .ToListAsync(cancellationToken);

        var recipients = contacts
            .GroupBy(contact => contact.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new MailboxDraftRecipient(group.Key, group.First().Name))
            .ToList();

        if (recipients.Count == 0)
            throw new InvalidOperationException(
                "The project has no client or architect contact with an email address — add one to the project's contacts before drafting the valuation email.");

        var message = new MailboxDraftMessage(
            To: recipients,
            Subject: command.Subject,
            HtmlBody: command.HtmlBody,
            Attachments: new[] { new MailboxDraftAttachment(pdf.FileName, "application/pdf", pdf.Content) },
            Categories: new List<string> { TriageCategories.Marker, TriageCategories.Client });

        var draft = await mailbox.CreateDraftAsync(message, cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The draft couldn't be created in the shared mailbox. Check the mailbox connection and try again.");

        return new ValuationReportSnapshotEmailDraft(
            pdf.Snapshot.ValuationReportSnapshotId,
            pdf.Snapshot.Label,
            command.Subject,
            recipients.Select(recipient => recipient.Email).ToList(),
            draft.WebLink,
            DraftMessageId: draft.Id);
    }
}
