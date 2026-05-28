using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCodeBudgetsForProjectHandler : IQueryHandler<ListCostCodeBudgetsForProject, IReadOnlyList<CostCodeBudget>>
{
    private readonly JpmsContext context;
    public ListCostCodeBudgetsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CostCodeBudget>> HandleAsync(ListCostCodeBudgetsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CostCodeBudgets.Where(b => b.ProjectId == query.ProjectId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
