
namespace Jewel.JPMS.Api.Features.Procurement.Documents;

/// <summary>
/// Everything the purchase-order PDF prints, resolved once by <see cref="WorkOrderPoDocumentBuilder"/>
/// so <see cref="WorkOrderPoRenderer"/> is a pure function of this model — the same division of
/// labour as RequestDocumentBuilder/RequestDocumentRenderer. The fields mirror exactly what the
/// portal's PurchaseOrderSheet component receives from its hosts (the Work Orders tab PO page and
/// the subcontractor portal), so the emailed PDF and the printed sheet say the same things.
/// </summary>
public sealed record WorkOrderPoDocumentModel(
    WorkOrder Order,
    IReadOnlyList<WorkOrderLine> Lines,
    string SupplierName,
    string SupplierContactName,
    IReadOnlyList<string> SupplierAddressLines,
    string ProjectName,
    IReadOnlyList<string> SiteAddressLines,
    string ApprovedByName,
    int PaymentTermsDays)
{
    /// <summary>Attachment file name — reference-led so the supplier's download folder reads.</summary>
    public string FileName => $"{Order.Reference} Purchase Order - Jewel Bespoke Build.pdf";
}
