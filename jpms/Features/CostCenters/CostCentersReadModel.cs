using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.CostCenters;

public sealed class CostCentersReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<CostCenter>? costCenters;
    private IReadOnlyList<CostCenter>? allCostCenters;
    private IReadOnlyList<CostCenter>? alphabetical;

    public CostCentersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>Active cost codes in master (SortOrder) order — what the Financials tab consumes.</summary>
    public IReadOnlyList<CostCenter> Current => costCenters ?? Array.Empty<CostCenter>();

    /// <summary>Active cost codes in alphabetical order (by code, then name) — what select boxes consume.</summary>
    public IReadOnlyList<CostCenter> Alphabetical => alphabetical ??= Current
        .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
        .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        .ToList()
        .AsReadOnly();

    /// <summary>Every cost code including retired ones — what the admin page consumes.</summary>
    public IReadOnlyList<CostCenter> All => allCostCenters ?? Array.Empty<CostCenter>();

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        costCenters = await queries.AskAsync(new ListCostCenters(), cancellationToken);
        alphabetical = null;
        OnChanged?.Invoke();
    }

    /// <summary>One fetch keeps both views coherent: All is the raw list, Current the active subset.</summary>
    public async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        allCostCenters = await queries.AskAsync(new ListCostCenters(IncludeInactive: true), cancellationToken);
        costCenters = allCostCenters.Where(c => c.IsActive).ToList().AsReadOnly();
        alphabetical = null;
        OnChanged?.Invoke();
    }
}
