using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

public sealed record ListVoqsForProject(string ProjectId) : IQuery<IReadOnlyList<VariationOrderQuote>>;
