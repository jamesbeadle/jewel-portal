using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListTimesheetsForProjectHandler : IQueryHandler<ListTimesheetsForProject, IReadOnlyList<Timesheet>>
{
    private readonly JpmsContext context;
    public ListTimesheetsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Timesheet>> HandleAsync(ListTimesheetsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Timesheets.Where(t => t.ProjectId == query.ProjectId).OrderByDescending(t => t.WorkedOn).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
