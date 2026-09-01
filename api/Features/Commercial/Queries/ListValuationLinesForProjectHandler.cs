using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListValuationLinesForProjectHandler : IQueryHandler<ListValuationLinesForProject, IReadOnlyList<ValuationLineItem>>
{
    private readonly JpmsContext context;
    public ListValuationLinesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationLineItem>> HandleAsync(ListValuationLinesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ValuationLineItems.AsNoTracking()
            .Where(line => line.ProjectId == query.ProjectId)
            .OrderBy(line => line.ElementType)
            .ThenBy(line => line.DisplayOrder)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
