using Jewel.JPMS.Api.Features.Progress.Photos;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiProgressPhotoTools
{
    private sealed record Fetched(IReadOnlyList<IncomingProgressPhoto> Images, IReadOnlyList<object> Failures);

    private static async Task<Fetched> FetchAllAsync(AiToolContext context, IReadOnlyList<string> sourceIds, CancellationToken ct)
    {
        var images = new List<IncomingProgressPhoto>();
        var failures = new List<object>();
        foreach (var sourceId in sourceIds)
        {
            var source = await AiSourceTools.FetchBytesAsync(context, sourceId, ct);
            if (source.Bytes is null)
            {
                failures.Add(new { sourceId, reason = source.Failure ?? "Could not be fetched." });
                continue;
            }
            images.Add(new IncomingProgressPhoto(source.FileName ?? sourceId, source.ContentType, source.Bytes));
        }
        return new Fetched(images, failures);
    }
}
