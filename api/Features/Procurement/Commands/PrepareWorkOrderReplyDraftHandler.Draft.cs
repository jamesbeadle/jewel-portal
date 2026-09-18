using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Documents;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed partial class PrepareWorkOrderReplyDraftHandler
{
    // Recipients are NOT resolved here — a reply inherits the original conversation's participants
    // from Graph, which is the point: the purchase order lands in the existing thread. The builder
    // resolved everything the PDF prints (supplier, project, paid position), mirroring the PO page.
    private async Task<MailboxReplyDraftMessage> PurchaseOrderReplyAsync(
        PrepareWorkOrderReplyDraft command,
        WorkOrderEntity order,
        WorkOrderPoDocumentModel model,
        string bucket,
        CancellationToken cancellationToken) =>
        new(command.MailboxMessageId,
            command.HtmlCoverNote,
            new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", WorkOrderPoRenderer.Render(model)) },
            await CategoriesAsync(order, bucket, cancellationToken));

    // Categories on the draft = what the SENT copy should carry, so it self-files: the pathway of
    // the company the order is placed with (Supplier for a merchant, else Subcontractor —
    // CompanyPathways, 2026-09-15), the order's own record tag, and — when the order came from
    // awarding a tender — the source package's tag (same set as SendWorkOrderPoEmailHandler).
    private async Task<IReadOnlyList<string>> CategoriesAsync(
        WorkOrderEntity order, string bucket, CancellationToken cancellationToken)
    {
        var categories = new List<string>
        {
            TriageCategories.Marker,
            bucket,
            TriageCategories.ForRecord(await WorkOrderTags.StemAsync(context, order, cancellationToken))
        };
        if (string.IsNullOrWhiteSpace(order.BidPackageId)) return categories;
        var package = await context.BidPackages.FindAsync(new object[] { order.BidPackageId! }, cancellationToken);
        if (package is not null) categories.Add(TriageCategories.ForRecord(package.Reference));
        return categories;
    }
}
