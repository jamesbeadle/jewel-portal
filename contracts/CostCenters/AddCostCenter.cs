using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CostCenters;

/// <summary>
/// Adds a cost code to the global cost-center master. Pass SortOrder = 0 to
/// append after the current last code.
/// </summary>
public sealed record AddCostCenter(
    string Code,
    string Name,
    int SortOrder = 0) : ICommand<CostCenter>;
