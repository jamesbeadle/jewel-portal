using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed partial class SendWorkOrderPoEmailHandler
{
    /// <summary>The supplier receives the purchase order itself, not only its summary (2026-09-09,
    /// the accountant's ask) — the same branded PDF the PO page prints.</summary>
    private async Task<MailboxDraftAttachment> PurchaseOrderPdfAsync(string workOrderId, CancellationToken cancellationToken)
    {
        var model = await WorkOrderPoDocumentBuilder.BuildAsync(context, workOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {workOrderId} not found.");
        return new MailboxDraftAttachment(model.FileName, "application/pdf", WorkOrderPoRenderer.Render(model));
    }

    /// <summary>What the SENT copy carries so it self-files: the pathway of the company the order is
    /// placed with, the order's own record tag, and — when the order came from awarding a tender —
    /// the source package's tag, so the thread also reads alongside the tender correspondence.</summary>
    private async Task<List<string>> CategoriesAsync(WorkOrderEntity order, SubcontractorEntity supplier, CancellationToken cancellationToken)
    {
        var categories = new List<string>
        {
            TriageCategories.Marker,
            CompanyPathways.BucketFor((DirectoryCategory)supplier.Category),
            TriageCategories.ForRecord(await WorkOrderTags.StemAsync(context, order, cancellationToken))
        };
        if (string.IsNullOrWhiteSpace(order.BidPackageId)) return categories;
        var package = await context.BidPackages.FindAsync(new object[] { order.BidPackageId! }, cancellationToken);
        if (package is not null) categories.Add(TriageCategories.ForRecord(package.Reference));
        return categories;
    }
}
