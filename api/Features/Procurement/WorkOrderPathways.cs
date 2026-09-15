using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// The communication pathway a work order's mail files under. Since 2026-09-15 a work order is
/// raised from the Supplier pane as well as the Subcontractor pane (Nigel: the purchase order a
/// merchant gets is the same record as the one a trade gets — one Work Orders tab, one WO number
/// sequence, one PO PDF — so there is no separate "purchase order" feature to build). The pathway
/// is therefore no longer implied by the record TYPE: it follows the COMPANY the order is placed
/// with, which is exactly what a pathway means ("who the correspondence is with"). A
/// Supplier-category directory record files under JPMS/Supplier; every other category — the
/// default Subcontractor, plus any mis-filed Client/Architect/Other record an old order happens
/// to point at — files under JPMS/Subcontractor, as every work order did before.
///
/// This is the record-level rule TriageCategories.BucketFor(RecordType) cannot express; it reaches
/// the link layer through <see cref="LinkableRecord.Pathway"/>, which WorkOrderLinkProvider fills
/// from here, and the outbound PO mail through <see cref="BucketAsync"/>.
/// </summary>
internal static class WorkOrderPathways
{
    /// <summary>The pathway (bucket) category for an order placed with a company of this category.</summary>
    public static string BucketFor(DirectoryCategory? category) =>
        category == DirectoryCategory.Supplier ? TriageCategories.Supplier : TriageCategories.Subcontractor;

    /// <summary>The same as the short label the audit trail and the UI use ("Supplier" / "Subcontractor").</summary>
    public static string LabelFor(DirectoryCategory? category) =>
        category == DirectoryCategory.Supplier ? "Supplier" : "Subcontractor";

    /// <summary>The bucket for an order, resolving its company's category. An order whose company
    /// is missing from the directory files under Subcontractor — the historic default.</summary>
    public static async Task<string> BucketAsync(JpmsContext context, WorkOrderEntity order, CancellationToken cancellationToken) =>
        BucketFor(await CategoryAsync(context, order.SubcontractorId, cancellationToken));

    /// <summary>The short pathway label for an order — what its audit rows carry.</summary>
    public static async Task<string> LabelAsync(JpmsContext context, WorkOrderEntity order, CancellationToken cancellationToken) =>
        LabelFor(await CategoryAsync(context, order.SubcontractorId, cancellationToken));

    /// <summary>The directory category of a company, or null when the id names no record.</summary>
    public static async Task<DirectoryCategory?> CategoryAsync(JpmsContext context, string subcontractorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subcontractorId)) return null;
        var category = await context.Subcontractors.AsNoTracking()
            .Where(s => s.SubcontractorId == subcontractorId)
            .Select(s => (int?)s.Category)
            .FirstOrDefaultAsync(cancellationToken);
        return category is { } value ? (DirectoryCategory)value : null;
    }
}
