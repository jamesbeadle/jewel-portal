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

    /// <summary>False until the first fetch has landed. The read accessors answer with an empty
    /// list in the meantime, which reads as a master with no codes in it — so a view that renders
    /// cost-centre names or offers them in a dropdown has to wait on this.</summary>
    bool IsLoaded { get; }

    event Action? OnChange;

    Task<IReadOnlyList<CostCenter>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<CostCenter> AddAsync(AddCostCenter command, CancellationToken cancellationToken = default);
    Task<CostCenter> ReviseAsync(ReviseCostCenter command, CancellationToken cancellationToken = default);
}
