using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Approves a selected VOQ and raises a Variation Order. In one transaction it creates the VO, writes
/// a Variation line into the Valuation Report, records a QS accrual on the CVR and commits the value
/// to the cost-centre budget, then marks the VOQ Approved. Value defaults to the VOQ's estimate.
/// </summary>
public sealed record ApproveVariationOrderQuote(
    string VariationOrderQuoteId,
    string CostCode,
    string ApprovedByEmail,
    decimal? Value = null) : ICommand<VariationOrder>;
