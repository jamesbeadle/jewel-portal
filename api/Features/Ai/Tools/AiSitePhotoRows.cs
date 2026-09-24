namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>How one pool photograph reads over the connector — the same row for the list and the
/// fingerprint match, archive included.</summary>
internal static class AiSitePhotoRows
{
    public static object Row(SitePhoto photo) => new
    {
        photo.SitePhotoId,
        photo.FileName,
        photo.ContentType,
        photo.FileSizeBytes,
        photo.ContentHash,
        photo.UploadedByEmail,
        photo.UploadedAt,
        isFiled = photo.IsFiled,
        filedToProjectId = photo.FiledToProjectId,
        filedToProgressUpdateId = photo.FiledToProgressUpdateId,
        photo.FiledAt,
        isArchived = photo.IsArchived,
        archive = photo.Archive is not { } archive ? null : new
        {
            reason = archive.Reason.ToString(),
            archive.Note,
            archive.ProjectId,
            archive.PeriodEnd,
            archive.ArchivedByEmail,
            archive.ArchivedAt
        }
    };
}
