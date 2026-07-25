using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Re-states the priced line build-up of an APPROVED variation order — add, edit or remove lines
/// without un-approving it. The new set replaces the variation's lines on the Valuation Report; the
/// order's value becomes their sum and its primary cost code the first line's. The commercial
/// records move by the difference: a delta QS accrual on the CVR and each cost centre's committed
/// budget adjusted by its own change. Refused before approval (use the approve panel) and once value
/// has been claimed against the variation (sort the claim first). The reviser is stamped server-side.
/// </summary>
public sealed record ReviseVariationOrderLines(
    string VariationOrderId,
    IReadOnlyList<VariationLineInput> Lines,
    string RevisedByEmail = "") : ICommand<VariationOrder>;
