using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingFoldersForProjectHandler
    : IQueryHandler<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>>
{
    private readonly JpmsContext context;

    public ListDrawingFoldersForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DrawingFolder>> HandleAsync(
        ListDrawingFoldersForProject query, CancellationToken cancellationToken)
    {
        var folders = await context.DrawingFolders.AsNoTracking()
            .Where(folder => folder.ProjectId == query.ProjectId)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);

        return folders.Select(folder => folder.ToModel()).ToList().AsReadOnly();
    }
}
