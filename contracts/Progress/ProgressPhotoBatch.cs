using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>What became of one image in a batch: stored as a new photo, skipped because the update
/// already holds a photo with the same content (or an earlier image in the same batch did), or
/// failed on its own — a failed image never fails the batch.</summary>
public enum ProgressPhotoIntakeResult
{
    Stored = 0,
    Duplicate = 1,
    Failed = 2
}

public sealed record ProgressPhotoIntakeOutcome(
    string FileName,
    ProgressPhotoIntakeResult Result,
    string? ProgressPhotoId,
    string Detail);

/// <summary>The answer to a photo batch: the update as it now stands, and an outcome per image
/// posted, in the order they were posted.</summary>
public sealed record ProgressPhotoBatchResult(
    ProgressUpdate Update,
    IReadOnlyList<ProgressPhotoIntakeOutcome> Outcomes)
{
    public int StoredCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Stored);
    public int DuplicateCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Duplicate);
    public int FailedCount => Outcomes.Count(outcome => outcome.Result == ProgressPhotoIntakeResult.Failed);
}
