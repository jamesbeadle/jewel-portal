using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// What the public acceptance page shows — the purchase order exactly as the PO PDF prints it
/// (the same fields PurchaseOrderSheet takes) plus the directory contact the link was sent to, so
/// the page can pre-fill the name the accepter signs with. Served by GET work-orders/accept/{token};
/// an unknown token is a 404 with no detail. Whether the order can still be accepted is the order's
/// own reading (<see cref="WorkOrder.IsAwaitingAcceptance"/>): accepted orders show who and when,
/// completed or cancelled ones say the order is closed.
/// </summary>
public sealed record WorkOrderAcceptanceView(
    WorkOrder Order,
    IReadOnlyList<WorkOrderLine> Lines,
    string SupplierName,
    string SupplierContactName,
    string SupplierContactEmail,
    IReadOnlyList<string> SupplierAddressLines,
    string ProjectName,
    IReadOnlyList<string> SiteAddressLines,
    string ApprovedByName,
    int PaymentTermsDays);

/// <summary>
/// The accepter's signature on the public page — the name they confirm or type before pressing
/// Accept. POST work-orders/accept/{token}. The email is never taken from the body: the order is
/// stamped with the directory contact's address the link was sent to.
/// </summary>
public sealed record WorkOrderAcceptanceSignature(string? Name);
