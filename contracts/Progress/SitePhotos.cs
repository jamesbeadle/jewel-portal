using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>The pool as it stands, newest upload first; <paramref name="UnfiledOnly"/> narrows it
/// to what is still waiting — neither filed nor archived — and <paramref name="ArchivedOnly"/>
/// to what the weekly-report run set aside.</summary>
public sealed record ListSitePhotos(bool UnfiledOnly = false, bool ArchivedOnly = false) : IQuery<IReadOnlyList<SitePhoto>>;

/// <summary>Which pool photos a set of fingerprints are. One answer per hash asked, in the order
/// asked; a hash the pool does not hold answers with a null photo.</summary>
public sealed record MatchSitePhotos(IReadOnlyList<string> ContentHashes) : IQuery<SitePhotoMatches>;

public sealed record SitePhotoMatch(string ContentHash, SitePhoto? Photo);

public sealed record SitePhotoMatches(IReadOnlyList<SitePhotoMatch> Matches)
{
    public int FoundCount => Matches.Count(match => match.Photo is not null);
    public int MissingCount => Matches.Count - FoundCount;
}

/// <summary>Files pool photos onto an existing progress update, after its current photos, in the
/// order given. A photo goes onto one update only; a second filing of the same photo is refused
/// per photo, never for the batch.</summary>
public sealed record FileSitePhotos(
    string ProgressUpdateId,
    IReadOnlyList<string> SitePhotoIds,
    string FiledByEmail) : ICommand<SitePhotoFilingResult>;

public enum SitePhotoFiling
{
    Filed = 0,
    AlreadyOnUpdate = 1,
    AlreadyFiledElsewhere = 2,
    NotFound = 3,
    Failed = 4,
    Archived = 5
}

public sealed record SitePhotoFilingOutcome(
    string SitePhotoId,
    SitePhotoFiling Result,
    string? ProgressPhotoId,
    string Detail);

public sealed record SitePhotoFilingResult(ProgressUpdate Update, IReadOnlyList<SitePhotoFilingOutcome> Outcomes)
{
    public int FiledCount => Outcomes.Count(outcome => outcome.Result == SitePhotoFiling.Filed);
}

/// <summary>Removes a pool photo and its stored file. A photo already filed keeps its copy on the
/// update — the pool row and the pool file go, nothing else.</summary>
public sealed record DeleteSitePhoto(string SitePhotoId) : ICommand<Acknowledgement>;

/// <summary>What became of one file dropped into the pool: stored, skipped because the pool
/// already holds the same bytes, or refused on its own.</summary>
public sealed record SitePhotoUploadOutcome(
    string FileName,
    ProgressPhotoIntakeResult Result,
    string? SitePhotoId,
    string Detail);

public sealed record SitePhotoUploadResult(IReadOnlyList<SitePhotoUploadOutcome> Outcomes)
{
    public int StoredCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Stored);
    public int DuplicateCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Duplicate);
    public int FailedCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Failed);
}
