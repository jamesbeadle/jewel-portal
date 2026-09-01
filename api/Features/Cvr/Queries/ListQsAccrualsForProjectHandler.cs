using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListQsAccrualsForProjectHandler : IQueryHandler<ListQsAccrualsForProject, IReadOnlyList<QsAccrual>>
{
    private readonly JpmsContext context;
    public ListQsAccrualsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<QsAccrual>> HandleAsync(ListQsAccrualsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.QsAccruals.AsNoTracking().Where(a => a.ProjectId == query.ProjectId).OrderByDescending(a => a.SignedOffAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
