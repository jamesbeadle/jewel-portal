using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListCvrSnapshotsForProjectHandler : IQueryHandler<ListCvrSnapshotsForProject, IReadOnlyList<CvrSnapshot>>
{
    private readonly JpmsContext context;
    public ListCvrSnapshotsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CvrSnapshot>> HandleAsync(ListCvrSnapshotsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.CvrSnapshots.Where(s => s.ProjectId == query.ProjectId).OrderByDescending(s => s.SnapshotAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
