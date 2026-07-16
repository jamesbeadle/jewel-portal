using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Revises the value of a live (Approved or Issued) Variation Order. The new value is written
/// through to the same commercial records the approval wrote: the Variation line on the Valuation
/// Report, the CVR (as a delta QS accrual) and the cost-centre budget commitment. The reviser is
/// stamped from the signed-in user server-side.
/// </summary>
public sealed record ReviseVariationOrderValue(
    string VariationOrderId,
    decimal Value,
    string RevisedByEmail = "") : ICommand<VariationOrder>;
