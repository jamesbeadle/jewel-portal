using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>
/// What a link reaches, read from the register: the folder ids it covers, and — the containment
/// check that makes the whole design safe — whether a requested revision belongs to a drawing in
/// one of them, on the link's own project. A revision outside is nothing to this link.
/// </summary>
public sealed class SiteDrawingReach
{
    private readonly JpmsContext context;

    public SiteDrawingReach(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DrawingFolderEntity>> ProjectFoldersAsync(SiteDrawingLinkEntity link, CancellationToken cancellationToken) =>
        await context.DrawingFolders.AsNoTracking()
            .Where(folder => folder.ProjectId == link.ProjectId)
            .ToListAsync(cancellationToken);

    public async Task<DrawingRevisionEntity?> RevisionWithinAsync(SiteDrawingLinkEntity link, string revisionId, CancellationToken cancellationToken)
    {
        var revision = await context.DrawingRevisions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == revisionId, cancellationToken);
        if (revision is null || string.IsNullOrWhiteSpace(revision.BlobRef)) return null;

        var drawing = await context.Drawings.AsNoTracking()
            .FirstOrDefaultAsync(row => row.DrawingId == revision.DrawingId, cancellationToken);
        var isOnTheLinksProject = drawing is not null && drawing.ProjectId == link.ProjectId;
        if (!isOnTheLinksProject || drawing!.DrawingFolderId is not { } folderId) return null;

        var folders = await ProjectFoldersAsync(link, cancellationToken);
        var reached = SiteDrawingFolders.Reached(link, folders);
        return reached.Contains(folderId) ? revision : null;
    }
}
