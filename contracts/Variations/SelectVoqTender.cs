using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Records the winning bid package + subcontractor (and the agreed value) on a VOQ and moves it to
/// Selected. This is the tender the eventual Variation Order will be raised from.
/// </summary>
public sealed record SelectVoqTender(
    string VariationOrderQuoteId,
    string BidPackageId,
    string SubcontractorId,
    decimal? EstimatedValue = null) : ICommand<VariationOrderQuote>;
