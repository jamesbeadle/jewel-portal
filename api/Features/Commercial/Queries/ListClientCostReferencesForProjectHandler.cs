using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClientCostReferencesForProjectHandler
    : IQueryHandler<ListClientCostReferencesForProject, IReadOnlyList<ClientCostReference>>
{
    private readonly JpmsContext context;
    public ListClientCostReferencesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClientCostReference>> HandleAsync(
        ListClientCostReferencesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ClientCostReferences.AsNoTracking()
            .Where(reference => reference.ProjectId == query.ProjectId)
            .OrderBy(reference => reference.CostCode)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
