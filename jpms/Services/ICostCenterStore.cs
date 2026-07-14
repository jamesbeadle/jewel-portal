using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICostCenterStore
{
    /// <summary>Active cost codes in master (SortOrder) order — for read-side views.</summary>
    IReadOnlyList<CostCenter> Active();

    /// <summary>Active cost codes in alphabetical order (by code, then name) — for dropdowns.</summary>
    IReadOnlyList<CostCenter> ActiveAlphabetical();

    /// <summary>Every cost code including retired ones — for the admin page.</summary>
    IReadOnlyList<CostCenter> All();

    event Action? OnChange;

    Task<IReadOnlyList<CostCenter>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<CostCenter> AddAsync(AddCostCenter command, CancellationToken cancellationToken = default);
    Task<CostCenter> ReviseAsync(ReviseCostCenter command, CancellationToken cancellationToken = default);
}
