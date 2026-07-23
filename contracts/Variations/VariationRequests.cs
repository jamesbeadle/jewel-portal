using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>GET /api/projects/{projectId}/variation-requests — subcontractor-raised variation
/// requests for the internal review queue.</summary>
public sealed record ListVariationRequestsForProject(string ProjectId) : IQuery<IReadOnlyList<SubcontractorVariationRequest>>;

/// <summary>
/// POST /api/variation-requests/{variationRequestId}/accept — accepts a subcontractor's variation
/// request and creates the variation order from it, in Quoting with the tender already recorded:
/// the sub's proposed value is the estimate (EstimatedValue) and the sub the
/// SelectedSubcontractorId. The order then runs the normal lifecycle — issued to the client,
/// approved via ApproveVariationOrder (valuation + CVR writes unchanged). AcceptedByEmail is
/// stamped server-side from the session.
/// </summary>
public sealed record AcceptVariationRequest(
    string VariationRequestId,
    string AcceptedByEmail = "") : ICommand<VariationOrder>;

/// <summary>POST /api/variation-requests/{variationRequestId}/reject — rejects with a reason the
/// subcontractor sees in the portal. RejectedByEmail is stamped server-side.</summary>
public sealed record RejectVariationRequest(
    string VariationRequestId,
    string Reason,
    string RejectedByEmail = "") : ICommand<SubcontractorVariationRequest>;
