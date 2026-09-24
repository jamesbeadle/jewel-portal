using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

public sealed class ListSitePhotosHandler : IQueryHandler<ListSitePhotos, IReadOnlyList<SitePhoto>>
{
    private readonly JpmsContext context;

    public ListSitePhotosHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SitePhoto>> HandleAsync(ListSitePhotos query, CancellationToken cancellationToken)
    {
        var rows = context.SitePhotos.AsNoTracking();
        if (query.UnfiledOnly) rows = rows.Where(row => row.FiledToProgressUpdateId == null && row.ArchivedAt == null);
        if (query.ArchivedOnly) rows = rows.Where(row => row.ArchivedAt != null);

        var entities = await rows
            .OrderByDescending(row => row.UploadedAt)
            .ThenBy(row => row.FileName)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList();
    }
}
