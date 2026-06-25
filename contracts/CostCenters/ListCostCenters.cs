using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CostCenters;

// Global query — the cost-center master is not scoped to a project.
public sealed record ListCostCenters() : IQuery<IReadOnlyList<CostCenter>>;
