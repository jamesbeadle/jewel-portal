using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListPrelimEntriesForItemHandler : IQueryHandler<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>>
{
    private readonly JpmsContext context;
    public ListPrelimEntriesForItemHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PrelimForecastEntry>> HandleAsync(ListPrelimEntriesForItem query, CancellationToken cancellationToken)
    {
        var entities = await context.PrelimForecastEntries.AsNoTracking().Where(p => p.PrelimItemId == query.PrelimItemId).OrderBy(p => p.WeekNumber).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
