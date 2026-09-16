using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

// The defect page's "Send to supplier" / "Chase supplier", performed server-side: the same
// wording (DefectSupplierEmails), the same envelope and the same filing the page's composer
// posts, through the one SendMailboxEmail handler — so the sent copy carries the defect's tag on
// the supplier's side and DefectSupplierSendRecorder stamps the defect exactly as it does for a
// send from the page.
public sealed class SendDefectToSupplierHandler : ICommandHandler<SendDefectToSupplier, ComposeOutcome>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<SendMailboxEmail, ComposeOutcome> sender;

    public SendDefectToSupplierHandler(JpmsContext context, ICommandHandler<SendMailboxEmail, ComposeOutcome> sender)
    {
        this.context = context;
        this.sender = sender;
    }

    public async Task<ComposeOutcome> HandleAsync(SendDefectToSupplier command, CancellationToken cancellationToken)
    {
        var entity = await context.Defects.AsNoTracking().FirstOrDefaultAsync(row => row.DefectId == command.DefectId, cancellationToken)
            ?? throw new InvalidOperationException("Defect not found.");
        var supplier = await DefectSupplierLookup.OneAsync(context, entity.SubcontractorId, cancellationToken);
        var defect = entity.ToModel(supplier);
        if (string.IsNullOrWhiteSpace(defect.SupplierEmail))
            throw new InvalidOperationException($"{defect.Reference} has no supplier email to send to — set its supplier (update_defect with a subcontractorId from search_directory) first.");

        var project = await context.Projects.AsNoTracking()
            .Where(row => row.ProjectId == entity.ProjectId)
            .Select(row => new { row.Name, row.Reference })
            .FirstOrDefaultAsync(cancellationToken);
        var wording = DefectSupplierEmails.Next(defect, project?.Name, project?.Reference, supplier?.ContactName);

        return await sender.HandleAsync(new SendMailboxEmail(
            ReplyToMessageId: null,
            ReplyToInternetMessageId: null,
            To: new[] { new ComposeRecipient(wording.To, supplier?.ContactName) },
            Cc: Array.Empty<ComposeRecipient>(),
            Bcc: Array.Empty<ComposeRecipient>(),
            Subject: string.IsNullOrWhiteSpace(command.Subject) ? wording.Subject : command.Subject.Trim(),
            Body: string.IsNullOrWhiteSpace(command.Body) ? wording.Body : command.Body,
            BodyIsHtml: false,
            Attachments: Array.Empty<ComposeAttachmentRef>(),
            SaveAsDraftOnly: command.SaveAsDraftOnly,
            Pathway: CompanyPathways.LabelFor(CategoryOf(supplier)),
            MarkThreadHandled: false,
            LinkRecordType: RecordType.Defect,
            LinkRecordId: defect.DefectId,
            SenderEmail: command.SentByEmail), cancellationToken);
    }

    private static DirectoryCategory? CategoryOf(SubcontractorEntity? supplier) =>
        supplier is null ? null : (DirectoryCategory)supplier.Category;
}
