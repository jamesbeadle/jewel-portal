using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

public sealed class ListProgressUpdatesForProjectHandler
    : IQueryHandler<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>>
{
    private readonly JpmsContext context;

    public ListProgressUpdatesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProgressUpdate>> HandleAsync(
        ListProgressUpdatesForProject query, CancellationToken cancellationToken)
    {
        var updates = await context.ProgressUpdates
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.WorkDate ?? row.CreatedAt)
            .ThenByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);

        var photosByUpdate = (await context.ProgressPhotos
                .Where(row => row.ProjectId == query.ProjectId)
                .OrderBy(row => row.SortOrder)
                .ToListAsync(cancellationToken))
            .GroupBy(row => row.ProgressUpdateId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return updates
            .Select(update => update.ToModel(
                photosByUpdate.TryGetValue(update.ProgressUpdateId, out var photos)
                    ? photos.Select(photo => photo.ToModel()).ToList()
                    : new List<ProgressPhoto>()))
            .ToList();
    }
}
