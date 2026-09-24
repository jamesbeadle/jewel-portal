using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// view_photos (the FD's ask, 24 Sep 2026): the assistant SEES a site photograph — one from the
/// site photo pool or one filed on a progress update — so the week's sift, the archive and the
/// report's choice of photographs happen inside the portal, never from laptop copies. One id
/// comes back as the photograph; several as one numbered contact sheet (<see cref="PhotoContactSheet"/>).
/// </summary>
internal static class AiPhotoViewTools
{
    public const string ViewPhotos = "view_photos";

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            ViewPhotos,
            "Shows photographs as an image you can see: a sitePhotoId from list_site_photos / "
            + "match_site_photos, or a progressPhotoId from get_contractors_report "
            + "(photosOnSelectedUpdates[]) — the two may be mixed. One id comes back as the "
            + $"photograph; up to {PhotoContactSheet.MostPhotos} come back as ONE contact sheet, four "
            + "across, left to right then top to bottom in the order asked, with the legend naming "
            + "which number is which id. Look before you archive (archive_site_photos) or leave a "
            + "photograph out of a report (update_contractors_report excludedPhotoIds).",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["photoIds"] = new
                    {
                        type = "array", items = new { type = "string" },
                        description = $"Site photo or progress photo ids, 1 to {PhotoContactSheet.MostPhotos}."
                    }
                },
                required = new[] { "photoIds" }
            },
            AiToolKind.Read,
            ProgressRoles.Readers,
            ViewPhotosAsync)
    };

    private static async Task<string> ViewPhotosAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var asked = AiToolSchema.Texts(input, "photoIds");
        if (asked is null || asked.Count > PhotoContactSheet.MostPhotos)
            return Fail($"photoIds must list 1 to {PhotoContactSheet.MostPhotos} site photo or progress photo ids.");

        var found = await StoredPhotos.FindAsync(context.Db, asked, ct);
        var store = context.Services.GetRequiredService<IProgressPhotoStore>();
        var shown = new List<(StoredPhoto Photo, byte[] Bytes)>();
        foreach (var photo in found)
        {
            if (await BytesOfAsync(store, photo.BlobRef, ct) is { } bytes) shown.Add((photo, bytes));
        }
        if (shown.Count == 0) return Fail("None of those ids is a stored site photo or progress photo.");

        var missing = asked.Except(shown.Select(item => item.Photo.Id)).ToList();
        var sheet = PhotoContactSheet.Build(shown.Select(item => item.Bytes).ToList());
        return AiImageToolResult.Build(Legend(shown.Select(item => item.Photo).ToList(), missing), PhotoContactSheet.MediaType, sheet);
    }

    private static async Task<byte[]?> BytesOfAsync(IProgressPhotoStore store, string blobRef, CancellationToken ct)
    {
        var blob = await store.OpenAsync(blobRef, ct);
        if (blob is null) return null;
        await using var content = blob.Content;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, ct);
        return copy.ToArray();
    }

    private static string Legend(IReadOnlyList<StoredPhoto> shown, IReadOnlyList<string> missing)
    {
        var numbered = shown.Select((photo, index) => $"{index + 1} = {photo.Id} ({photo.FileName})");
        var legend = string.Join(" · ", numbered);
        return missing.Count == 0 ? legend : $"{legend} · not found: {string.Join(", ", missing)}";
    }

    private static string Fail(string message) => JsonSerializer.Serialize(new { ok = false, error = message });
}
