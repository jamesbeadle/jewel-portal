using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>
/// The pure half of a batch: which images are prepared and stored, which are duplicates, which
/// fail to prepare. Deduplication is on the content hash of the file as received — against what
/// the update already holds and against earlier images in the same batch — never on the file name.
/// </summary>
internal static class ProgressPhotoBatchPlan
{
    public sealed record Step(PreparedProgressPhoto? Prepared, ProgressPhotoIntakeOutcome Outcome);

    public static IReadOnlyList<Step> Build(IReadOnlyList<IncomingProgressPhoto> images, IReadOnlyDictionary<string, string> hashesAlreadyHeld)
    {
        var seen = new Dictionary<string, string>(hashesAlreadyHeld, StringComparer.Ordinal);
        var steps = new List<Step>();
        foreach (var image in images)
        {
            steps.Add(Plan(image, seen));
        }
        return steps;
    }

    private static Step Plan(IncomingProgressPhoto image, Dictionary<string, string> seen)
    {
        var contentHash = ProgressPhotoContentHash.Of(image.Bytes);
        if (seen.TryGetValue(contentHash, out var holder))
            return new Step(null, new ProgressPhotoIntakeOutcome(image.FileName, ProgressPhotoIntakeResult.Duplicate, null,
                $"Same content as {holder} — not stored again."));

        try
        {
            var prepared = ProgressPhotoPreparation.Prepare(image);
            seen[contentHash] = image.FileName;
            return new Step(prepared, new ProgressPhotoIntakeOutcome(image.FileName, ProgressPhotoIntakeResult.Stored, null, "Prepared."));
        }
        catch (InvalidDataException ex)
        {
            return new Step(null, new ProgressPhotoIntakeOutcome(image.FileName, ProgressPhotoIntakeResult.Failed, null, ex.Message));
        }
    }
}
