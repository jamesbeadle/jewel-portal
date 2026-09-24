using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Sets pool photographs aside from the weekly report, each with its reason, as the
/// dump of one project's week when the run knows it. Archived photos are kept — never filed,
/// never deleted — and a filed one is refused per photo, never for the batch.</summary>
public sealed record ArchiveSitePhotos(
    IReadOnlyList<SitePhotoToArchive> Photos,
    string? ProjectId,
    DateOnly? PeriodEnd,
    string ArchivedByEmail) : ICommand<SitePhotoArchivingResult>;

public sealed record SitePhotoToArchive(string SitePhotoId, SitePhotoArchiveReason Reason, string Note);

public enum SitePhotoArchiving
{
    Archived = 0,
    AlreadyArchived = 1,
    AlreadyFiled = 2,
    NotFound = 3
}

public sealed record SitePhotoArchivingOutcome(string SitePhotoId, SitePhotoArchiving Result, string Detail);

public sealed record SitePhotoArchivingResult(IReadOnlyList<SitePhotoArchivingOutcome> Outcomes)
{
    public int ArchivedCount => Outcomes.Count(outcome => outcome.Result == SitePhotoArchiving.Archived);
}

/// <summary>Brings an archived photograph back to the waiting view, so it can be filed.</summary>
public sealed record RestoreSitePhoto(string SitePhotoId) : ICommand<Acknowledgement>;
