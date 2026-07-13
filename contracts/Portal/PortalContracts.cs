using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Portal;

/// <summary>What a signed-in subcontractor sees of their own record: the directory entry plus
/// their compliance documents (with expiry status computed client-side as elsewhere).</summary>
public sealed record SubcontractorPortalRecord(
    Subcontractor Record,
    IReadOnlyList<ComplianceDocument> ComplianceDocuments);

/// <summary>
/// GET /api/portal/my/record. SubcontractorId is resolved SERVER-SIDE from the session
/// (Gates/SubcontractorScope) — the client sends the query empty and can never ask for another
/// company's record. The property exists only so the API-side handler receives the resolved id.
/// </summary>
public sealed record GetMyPortalRecord(string SubcontractorId = "") : IQuery<SubcontractorPortalRecord?>;

/// <summary>A work order as the subcontractor sees it: the order, the project's display name
/// (resolved server-side — portal sessions can't read the projects list) and the priced lines.</summary>
public sealed record PortalWorkOrder(
    WorkOrder Order,
    string ProjectName,
    IReadOnlyList<WorkOrderLine> Lines);

/// <summary>
/// GET /api/portal/my/work-orders — the caller's issued work orders (Released and later; Drafts
/// are internal). SubcontractorId is resolved server-side from the session, as with
/// GetMyPortalRecord.
/// </summary>
public sealed record ListMyWorkOrders(string SubcontractorId = "") : IQuery<IReadOnlyList<PortalWorkOrder>>;

/// <summary>
/// POST /api/portal/my/work-orders/{workOrderId}/variation-requests — the subcontractor proposes
/// and prices a change against one of their own work orders. SubcontractorId is resolved
/// server-side from the session; anything the client sends in it is ignored.
/// </summary>
public sealed record RaiseMyVariationRequest(
    string WorkOrderId,
    string Title,
    string Description,
    decimal ProposedValue,
    string SubcontractorId = "") : ICommand<SubcontractorVariationRequest>;

/// <summary>POST /api/portal/my/variation-requests/{variationRequestId}/withdraw — only while the
/// request is still open (Submitted/UnderReview), and only the raising subcontractor's own.</summary>
public sealed record WithdrawMyVariationRequest(
    string VariationRequestId,
    string SubcontractorId = "") : ICommand<SubcontractorVariationRequest>;

/// <summary>GET /api/portal/my/variation-requests — the caller's requests, newest first, with
/// project/work-order display fields resolved server-side.</summary>
public sealed record ListMyVariationRequests(string SubcontractorId = "") : IQuery<IReadOnlyList<SubcontractorVariationRequest>>;
