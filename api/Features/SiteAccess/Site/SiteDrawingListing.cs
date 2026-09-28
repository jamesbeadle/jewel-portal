using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>Composes the page a scan opens, at scan time — so what the same poster shows moves
/// with the register as revisions land and are approved, with no reprint.</summary>
public sealed class SiteDrawingListing
{
    private readonly JpmsContext context;
    private readonly SiteDrawingReach reach;

    public SiteDrawingListing(JpmsContext context, SiteDrawingReach reach)
    {
        this.context = context;
        this.reach = reach;
    }

    public async Task<SiteDrawingsPage> ReadAsync(SiteDrawingLinkEntity link, CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == link.ProjectId, cancellationToken);
        var folders = await reach.ProjectFoldersAsync(link, cancellationToken);
        var reachedFolderIds = SiteDrawingFolders.Reached(link, folders);
        var drawings = await context.Drawings.AsNoTracking()
            .Where(row => row.ProjectId == link.ProjectId && row.DrawingFolderId != null && reachedFolderIds.Contains(row.DrawingFolderId))
            .ToListAsync(cancellationToken);
        var drawingIds = drawings.Select(drawing => drawing.DrawingId).ToList();
        var revisions = await context.DrawingRevisions.AsNoTracking()
            .Where(row => drawingIds.Contains(row.DrawingId))
            .ToListAsync(cancellationToken);

        var shownAt = SiteClock.InUkTime(DateTimeOffset.UtcNow);
        return new SiteDrawingsPage(
            project?.Name ?? "",
            link.Label,
            shownAt.ToString("dd MMM yyyy HH:mm"),
            SiteDrawingRows.Of(drawings, revisions, folders));
    }
}
