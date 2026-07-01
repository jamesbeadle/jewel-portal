using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>Marks an approved Variation Order as formally Issued (instructed).</summary>
public sealed record IssueVariationOrder(string VariationOrderId) : ICommand<VariationOrder>;
