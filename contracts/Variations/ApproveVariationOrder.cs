using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Approves a variation order — the client's instruction to proceed. In one transaction it mints
/// the V-ref, records the agreed value and cost code on the order, writes a Variation line into
/// the Valuation Report, records a QS accrual on the CVR and commits the value to the cost-centre
/// budget, then marks the order Approved. Value defaults to the order's estimate. Allowed from
/// Quoting (a manual/internal approval) as well as Issued — the normal route is Issued, once the
/// client has the priced order in front of them.
/// </summary>
public sealed record ApproveVariationOrder(
    string VariationOrderId,
    string CostCode,
    string ApprovedByEmail,
    decimal? Value = null) : ICommand<VariationOrder>;
