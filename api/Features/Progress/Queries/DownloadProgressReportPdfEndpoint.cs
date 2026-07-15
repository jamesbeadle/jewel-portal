using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Progress.Documents;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

/// <summary>
/// GET /api/progress-reports/{progressReportId}/pdf — renders and streams the client-facing
/// progress report. Regenerated from the register on every download, so it always reflects the
/// report (and its selected updates) as they stand; nothing is stored.
/// </summary>
public sealed class DownloadProgressReportPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    // Only raster formats PDFsharp can embed; anything else (HEIC, PDFs, videos) is skipped
    // rather than corrupting the document.
    private static readonly string[] EmbeddableContentTypes =
        { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp" };

    public DownloadProgressReportPdfEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProgressPhotoStore photoStore)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
    }

    [Function(nameof(DownloadProgressReportPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "progress-reports/{progressReportId}/pdf")] HttpRequest request,
        string progressReportId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var report = await context.ProgressReports
            .FirstOrDefaultAsync(row => row.ProgressReportId == progressReportId, cancellationToken);
        if (report is null) return new NotFoundObjectResult($"Progress report {progressReportId} not found.");

        var project = await context.Projects
            .FirstOrDefaultAsync(row => row.ProjectId == report.ProjectId, cancellationToken);
        if (project is null) return new NotFoundObjectResult($"Project {report.ProjectId} not found.");

        var selections = await context.ProgressReportSelections
            .Where(row => row.ProgressReportId == progressReportId)
            .OrderBy(row => row.SortOrder)
            .ToListAsync(cancellationToken);
        var selectedIds = selections.Select(row => row.ProgressUpdateId).ToList();

        var updatesById = await context.ProgressUpdates
            .Where(row => selectedIds.Contains(row.ProgressUpdateId))
            .ToDictionaryAsync(row => row.ProgressUpdateId, cancellationToken);
        var photosByUpdate = (await context.ProgressPhotos
                .Where(row => selectedIds.Contains(row.ProgressUpdateId))
                .OrderBy(row => row.SortOrder)
                .ToListAsync(cancellationToken))
            .GroupBy(row => row.ProgressUpdateId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var documentUpdates = new List<ProgressReportDocumentUpdate>();
        foreach (var updateId in selectedIds)
        {
            if (!updatesById.TryGetValue(updateId, out var update)) continue;

            var photos = new List<ProgressReportDocumentPhoto>();
            if (photosByUpdate.TryGetValue(updateId, out var photoRows))
            {
                foreach (var photoRow in photoRows)
                {
                    if (!EmbeddableContentTypes.Contains(photoRow.ContentType, StringComparer.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrWhiteSpace(photoRow.BlobRef)) continue;

                    var blob = await photoStore.OpenAsync(photoRow.BlobRef, cancellationToken);
                    if (blob is null) continue;

                    await using var content = blob.Content;
                    using var buffer = new MemoryStream();
                    await content.CopyToAsync(buffer, cancellationToken);
                    photos.Add(new ProgressReportDocumentPhoto(buffer.ToArray()));
                }
            }

            documentUpdates.Add(new ProgressReportDocumentUpdate(
                update.Title, update.Description, update.WorkDate, photos));
        }

        var model = new ProgressReportDocumentModel(
            report.Title,
            project.Name,
            project.Reference,
            project.ClientName,
            report.PeriodStart,
            report.PeriodEnd,
            report.Introduction,
            report.WorkCompleted,
            report.UpcomingWorks,
            report.CreatedByEmail,
            DateTimeOffset.UtcNow,
            documentUpdates);

        var pdf = ProgressReportRenderer.Render(model);

        var fileName = SanitiseFileName($"{project.Reference} - Progress Report - {report.Title}.pdf");
        return new FileContentResult(pdf, "application/pdf") { FileDownloadName = fileName };
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
