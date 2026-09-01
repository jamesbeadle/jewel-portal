using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClaimPeriodsForProjectHandler
    : IQueryHandler<ListClaimPeriodsForProject, IReadOnlyList<ClaimPeriod>>
{
    private readonly JpmsContext context;

    public ListClaimPeriodsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClaimPeriod>> HandleAsync(
        ListClaimPeriodsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ClaimPeriods.AsNoTracking()
            .Where(period => period.ProjectId == query.ProjectId)
            .OrderBy(period => period.PeriodNumber)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
