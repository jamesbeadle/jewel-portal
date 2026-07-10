using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListValuationReportSnapshotsForProjectHandler : IQueryHandler<ListValuationReportSnapshotsForProject, IReadOnlyList<ValuationReportSnapshot>>
{
    private readonly JpmsContext context;
    public ListValuationReportSnapshotsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationReportSnapshot>> HandleAsync(ListValuationReportSnapshotsForProject query, CancellationToken cancellationToken)
    {
        var snapshots = await context.ValuationReportSnapshots
            .Where(snapshot => snapshot.ProjectId == query.ProjectId)
            .OrderByDescending(snapshot => snapshot.TakenAt)
            .ToListAsync(cancellationToken);
        return snapshots.Select(snapshot => snapshot.ToModel()).ToList();
    }
}
