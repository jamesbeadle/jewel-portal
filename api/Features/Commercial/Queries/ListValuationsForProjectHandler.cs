using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListValuationsForProjectHandler : IQueryHandler<ListValuationsForProject, IReadOnlyList<Valuation>>
{
    private readonly JpmsContext context;
    public ListValuationsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Valuation>> HandleAsync(ListValuationsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Valuations.AsNoTracking().Where(v => v.ProjectId == query.ProjectId).OrderByDescending(v => v.IssuedAt ?? DateTimeOffset.MinValue).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
