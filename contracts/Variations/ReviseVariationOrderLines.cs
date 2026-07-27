using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Re-states the priced line build-up of an APPROVED variation order — add, edit or remove lines
/// without un-approving it. The order's value becomes the lines' sum and its primary cost code the
/// first line's. The commercial records move by the difference: a delta QS accrual on the CVR and
/// each cost centre's committed budget adjusted by its own change.
///
/// A line carrying a ValuationLineItemId is RE-PRICED in place, keeping the claim history standing
/// against it; one without is added; a report line no submitted row claims is dropped. So a
/// variation can be re-priced after it has been claimed against: settled claims keep the money they
/// were certified at and the claim in progress is re-based onto the new figures. Refused before
/// approval (use the approve panel), while the latest claim is preapproved (its money is already
/// locked), and when a line carrying settled value would be dropped altogether — re-price that line
/// to nothing instead. The reviser is stamped server-side.
/// </summary>
public sealed record ReviseVariationOrderLines(
    string VariationOrderId,
    IReadOnlyList<VariationLineInput> Lines,
    string RevisedByEmail = "") : ICommand<VariationOrder>;
