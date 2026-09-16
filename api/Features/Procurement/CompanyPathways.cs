using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// The communication pathway a record's mail files under when the record is raised WITH A
/// COMPANY: a work order (since 2026-09-15 — Nigel: the purchase order a merchant gets is the
/// same record as the one a trade gets, one Work Orders tab, one WO number sequence, one PO PDF)
/// and a defect (since 2026-09-16 — the same DEF-#### is a trade's workmanship or a merchant's
/// faulty goods). The pathway is not implied by the record TYPE: it follows the COMPANY, which is
/// exactly what a pathway means ("who the correspondence is with"). A Supplier-category directory
/// record files under JPMS/Supplier; every other category — the default Subcontractor, plus any
/// mis-filed Client/Architect/Other record an old row happens to point at — and a record with no
/// company yet file under JPMS/Subcontractor, as every work order and defect did before.
///
/// This is the record-level rule TriageCategories.BucketFor(RecordType) cannot express; it reaches
/// the link layer through <see cref="LinkableRecord.Pathway"/>, which WorkOrderLinkProvider and
/// DefectLinkProvider fill from here, and the outbound PO mail through <see cref="BucketAsync"/>.
/// </summary>
internal static class CompanyPathways
{
    /// <summary>The pathway (bucket) category for a record raised with a company of this category.</summary>
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
