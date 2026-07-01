using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>Cancels a Variation Order. (Does not reverse already-written CVR/valuation entries.)</summary>
public sealed record CancelVariationOrder(string VariationOrderId) : ICommand<VariationOrder>;
