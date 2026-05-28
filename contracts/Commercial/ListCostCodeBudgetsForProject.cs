using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record ListCostCodeBudgetsForProject(string ProjectId) : IQuery<IReadOnlyList<CostCodeBudget>>;
