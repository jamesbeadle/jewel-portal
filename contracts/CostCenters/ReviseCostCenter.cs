using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CostCenters;

/// <summary>
/// Revises a cost code in the global cost-center master. Setting IsActive = false
/// retires the code (it drops out of dropdowns and the Financials view) without
/// deleting it, so historical allocations keep resolving.
/// </summary>
public sealed record ReviseCostCenter(
    string CostCenterId,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive) : ICommand<CostCenter>;
