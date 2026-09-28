using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.SiteAccess;

internal static class SiteDrawingLinkEntityMapping
{
    public static SiteDrawingLink ToModel(this SiteDrawingLinkEntity entity) =>
        new(entity.SiteDrawingLinkId, entity.ProjectId, entity.DrawingFolderId, entity.IncludeSubFolders,
            entity.Label, entity.CreatedByEmail, entity.CreatedAt, entity.ExpiresAt, entity.RevokedAt,
            entity.ScanCount, entity.LastScannedAt);
}
