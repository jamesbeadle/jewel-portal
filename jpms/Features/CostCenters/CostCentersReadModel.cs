using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.CostCenters;

public sealed class CostCentersReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<CostCenter>? costCenters;

    public CostCentersReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<CostCenter> Current => costCenters ?? Array.Empty<CostCenter>();

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        costCenters = await queries.AskAsync(new ListCostCenters(), cancellationToken);
        OnChanged?.Invoke();
    }
}
