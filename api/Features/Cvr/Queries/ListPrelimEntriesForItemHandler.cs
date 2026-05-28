using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListPrelimEntriesForItemHandler : IQueryHandler<ListPrelimEntriesForItem, IReadOnlyList<PrelimForecastEntry>>
{
    private readonly JpmsContext context;
    public ListPrelimEntriesForItemHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PrelimForecastEntry>> HandleAsync(ListPrelimEntriesForItem query, CancellationToken cancellationToken)
    {
        var entities = await context.PrelimForecastEntries.Where(p => p.PrelimItemId == query.PrelimItemId).OrderBy(p => p.WeekNumber).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
