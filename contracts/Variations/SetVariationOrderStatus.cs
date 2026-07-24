using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Moves a variation order between the side-effect-free stages of its lifecycle — Quoting, Issued,
/// Awaiting AI and (from an unapproved state) Rejected — the status pill's dropdown. Entering Issued stamps
/// IssuedAt ("sent to the client"); moving back to Quoting clears it. Rejecting here stamps
/// RejectedAt; un-rejecting clears it. Approval is deliberately out of scope: approving writes
/// through to the Valuation Report, CVR and cost-centre budget, so it must go through
/// ApproveVariationOrder; likewise an APPROVED variation can only leave Approved via
/// RejectVariationOrder (which reverses those writes) or ReturnVariationOrderToQuoting (which
/// reverses them as a record correction).
/// </summary>
public sealed record SetVariationOrderStatus(string VariationOrderId, VariationOrderStatus Status) : ICommand<VariationOrder>;
