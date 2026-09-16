using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The connector's reads of the company-wide site photo pool (2026-09-16, James: "a big dumping
/// ground for photos and any project … jeremy can do his normal weekly report mcp stuff and it
/// does the matching"). A tool call carries words, so photographs reach the portal by a person
/// dropping them on the Site photos page; the assistant then finds them by FINGERPRINT — the
/// SHA-256 of the file on the laptop, which is the SHA-256 the pool stored — and files them onto
/// the right day's progress update with file_site_photos. No image bytes ever go through the model.
/// </summary>
internal static class AiSitePhotoTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public const string ListSitePhotos = "list_site_photos";
    public const string MatchSitePhotos = "match_site_photos";

    public const string HashingInstruction =
        "A fingerprint is the SHA-256 of the file's bytes as lower-case hex — on a Mac "
        + "`shasum -a 256 *.jpg` in the folder, on Windows `Get-FileHash -Algorithm SHA256`. "
        + "Hash the ORIGINAL files (the WhatsApp export's, the ones the site manager dropped into "
        + "the pool) — a re-saved or resized copy has a different hash and matches nothing.";

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            ListSitePhotos,
            "The company-wide site photo pool: every photograph a person has dropped on the Site "
            + "photos page (Projects → Site photos) before anyone said which project or day it "
            + "belongs to, newest first — id, file name, size, its SHA-256 fingerprint, who dropped "
            + "it and when, and where it has been filed (project + progress update) or null while "
            + "it is still unfiled. Pass unfiledOnly to see what is waiting. Filing is by "
            + "fingerprint: hash the week's files on the laptop, match_site_photos tells you which "
            + "pool photos they are, file_site_photos puts them on the day's update.",
            AiToolSchema.Object(
                ("unfiledOnly", "boolean", "Only photos no progress update has claimed yet.", false)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            ListSitePhotosAsync),

        new(
            MatchSitePhotos,
            "Which pool photos a set of fingerprints are. Answers one row per hash asked, in the "
            + "order asked: the pool photo (id, file name, filed-to update or null) or null when the "
            + "pool does not hold that file. THE STEP BEFORE DRAFTING A WEEKLY REPORT from a "
            + "WhatsApp export folder: hash every image in the folder, call this once with all the "
            + "hashes, then file_site_photos the found ones onto each day's progress update by "
            + "the day the export puts them on. A missing hash means the site manager has not "
            + "dropped that file in the pool yet — say which file names, never re-encode or paste "
            + "the image. " + HashingInstruction,
            AiToolSchema.Object(
                ("contentHashes", "array", "SHA-256 fingerprints (lower-case hex, 64 chars) of the files to look up — up to a week's worth in one call.", true)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            MatchSitePhotosAsync)
    };

    private static async Task<string> ListSitePhotosAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var unfiledOnly = AiToolSchema.Flag(input, "unfiledOnly") ?? false;
        var photos = await context.Services
            .GetRequiredService<IQueryHandler<ListSitePhotos, IReadOnlyList<SitePhoto>>>()
            .HandleAsync(new ListSitePhotos(unfiledOnly), ct);
        return Serialise(new
        {
            ok = true,
            unfiledOnly,
            count = photos.Count,
            unfiled = photos.Count(photo => !photo.IsFiled),
            photos = photos.Select(Row)
        });
    }

    private static async Task<string> MatchSitePhotosAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var hashes = AiToolSchema.Texts(input, "contentHashes");
        if (hashes is null) return Fail("contentHashes must be a non-empty array of SHA-256 fingerprints. " + HashingInstruction);

        var matches = await context.Services
            .GetRequiredService<IQueryHandler<MatchSitePhotos, SitePhotoMatches>>()
            .HandleAsync(new MatchSitePhotos(hashes), ct);
        return Serialise(new
        {
            ok = true,
            found = matches.FoundCount,
            missing = matches.MissingCount,
            matches = matches.Matches.Select(match => new
            {
                match.ContentHash,
                photo = match.Photo is null ? null : Row(match.Photo)
            }),
            note = matches.MissingCount == 0
                ? "Every file is in the pool."
                : "A missing hash is a file nobody has dropped in the pool yet (or a copy that was re-saved): ask for it to be dropped on the Site photos page, then match again."
        });
    }

    private static object Row(SitePhoto photo) => new
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
        photo.FiledAt
    };
}
