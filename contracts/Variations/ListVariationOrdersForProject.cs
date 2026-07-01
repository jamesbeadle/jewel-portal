using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

public sealed record ListVariationOrdersForProject(string ProjectId) : IQuery<IReadOnlyList<VariationOrder>>;
