using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// POST /api/variation-orders/{variationOrderId}/work-order — issues the NEW work order that
/// instructs an approved variation order (existing orders are never uplifted; a variation can
/// require one or more new orders, so repeat issues are allowed deliberately). The order is
/// created Released for the VO's subcontractor at the VO's value, with the next sequential
/// per-project number. IssuedByEmail is stamped server-side from the session.
/// </summary>
public sealed record IssueWorkOrderForVariationOrder(
    string VariationOrderId,
    string IssuedByEmail = "") : ICommand<WorkOrder>;