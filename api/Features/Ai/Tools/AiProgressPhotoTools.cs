using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The connector's way of putting photographs on a progress update (2026-09-16, the FD's
/// weekly-report spec, change 2). A tool call carries words, not files, so the images come from a
/// SOURCE the portal can already reach — the attachments on an email tagged to a record, or a
/// document filed on the project — by the source_id list_sources hands out. Photographs dropped
/// into the chat never reach the portal; the site WhatsApp export goes through the Progress
/// tab's "Import WhatsApp week" instead. Same intake as the page's form: JPEG, PNG and HEIC, up
/// to fifty per call, deduplicated on content, resized on store, one outcome per image.
/// </summary>
internal static partial class AiProgressPhotoTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public const string AddProgressPhotos = "add_progress_photos";

    public static IReadOnlyList<AiTool> Build() => new AiTool[]
    {
        new(
            AddProgressPhotos,
            "WRITE: attach photographs to a progress update, several at a time — the counterpart to "
            + "delete_progress_photo. Each image is fetched by its source_id (list_sources: an "
            + "attachment on an email tagged to a record, or a document filed on the project), "
            + "prepared (HEIC converted, turned upright, shrunk to display size), skipped when the "
            + "update already holds the same image by content, and stored after the update's current "
            + "photos in the order given. Up to fifty per call; a failed image never fails the "
            + "batch — the answer says, per image, whether it was stored, a duplicate or failed, with "
            + "the stored photo ids. Images pasted into the chat cannot be forwarded: ask the user to "
            + "email them to the projects mailbox (then pass the attachment source_ids) or to use "
            + "Import WhatsApp week on the Progress tab.",
            AiToolSchema.Object(
                ("progressUpdateId", "string", "The update's id — list_progress or create_progress_update gives it.", true),
                ("sourceIds", "array", "The images' source_ids from list_sources, in the order they should appear.", true)),
            AiToolKind.Write,
            ProgressRoles.Contributors,
            AddProgressPhotosAsync)
    };

    private static async Task<string> AddProgressPhotosAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var progressUpdateId = (AiToolSchema.Text(input, "progressUpdateId") ?? "").Trim();
        if (progressUpdateId.Length == 0) return Fail("progressUpdateId is required.");
        var sourceIds = AiToolSchema.Texts(input, "sourceIds");
        if (sourceIds is null) return Fail("sourceIds must be a non-empty array of source ids from list_sources.");
        if (sourceIds.Count > ProgressPhotoLimits.MaxImagesPerBatch)
            return Fail($"{sourceIds.Count} images were given; a batch takes at most {ProgressPhotoLimits.MaxImagesPerBatch}.");

        var update = await context.Db.ProgressUpdates.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProgressUpdateId == progressUpdateId, ct);
        if (update is null) return Fail($"No progress update exists with id \"{progressUpdateId}\".");

        var fetched = await FetchAllAsync(context, sourceIds, ct);
        if (fetched.Images.Count == 0)
            return Serialise(new { ok = false, error = "None of the sources could be fetched.", notFetched = fetched.Failures });

        var added = await ProgressPhotoBatches.AddAsync(
            context.Services.GetRequiredService<ProgressPhotoIntake>(),
            context.Services.GetRequiredService<AddProgressPhotosValidation>(),
            context.Services.GetRequiredService<ICommandHandler<AddProgressPhotos, ProgressUpdate>>(),
            update.ProjectId, progressUpdateId, context.User.Email, fetched.Images, ct);
        if (added.Failure is not null) return Serialise(new { ok = false, failure = added.Failure, notFetched = fetched.Failures });

        var batch = added.Batch!;
        return Serialise(new
        {
            ok = true,
            progressUpdateId,
            photoCount = batch.Update.Photos.Count,
            stored = batch.StoredCount,
            duplicates = batch.DuplicateCount,
            failed = batch.FailedCount + fetched.Failures.Count,
            outcomes = batch.Outcomes,
            notFetched = fetched.Failures
        });
    }
}
