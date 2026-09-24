using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>Why a pool photo cannot go onto an update: already on it, already on another, or
/// archived as not for the report. Null when it may be filed.</summary>
internal static class SitePhotoFilingRefusals
{
    public static SitePhotoFilingOutcome? For(SitePhotoEntity poolPhoto, string progressUpdateId)
    {
        if (poolPhoto.FiledToProgressUpdateId == progressUpdateId)
            return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.AlreadyOnUpdate, poolPhoto.FiledToProgressPhotoId,
                "Already filed onto this update.");
        if (poolPhoto.FiledToProgressUpdateId is not null)
            return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.AlreadyFiledElsewhere, poolPhoto.FiledToProgressPhotoId,
                $"Already filed onto progress update {poolPhoto.FiledToProgressUpdateId} — a photo goes onto one update only.");
        if (poolPhoto.ArchivedAt is not null)
            return new SitePhotoFilingOutcome(poolPhoto.SitePhotoId, SitePhotoFiling.Archived, null,
                "Archived as not for the report — restore it (restore_site_photo, or Restore on the Site photos page) before filing.");
        return null;
    }
}
