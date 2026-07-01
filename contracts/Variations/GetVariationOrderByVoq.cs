using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

// Returns the Variation Order raised from a given VOQ, or null if it has not been approved yet.
public sealed record GetVariationOrderByVoq(string VariationOrderQuoteId) : IQuery<VariationOrder?>;
