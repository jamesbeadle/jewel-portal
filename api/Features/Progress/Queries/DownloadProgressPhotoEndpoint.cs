using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Queries;

/// <summary>
/// GET /api/progress-photos/{progressPhotoId}/file — streams the stored image for a photo.
/// The container is private, so the file is proxied through the API (never a public URL).
/// Inline requests (?inline=1) feed the in-app thumbnails/viewer; without the flag the response
/// carries a download filename.
/// </summary>
public sealed class DownloadProgressPhotoEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public DownloadProgressPhotoEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        IProgressPhotoStore photoStore)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
    }

    [Function(nameof(DownloadProgressPhotoEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "progress-photos/{progressPhotoId}/file")] HttpRequest request,
        string progressPhotoId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var photo = await context.ProgressPhotos
            .FirstOrDefaultAsync(row => row.ProgressPhotoId == progressPhotoId, cancellationToken);
        if (photo is null || string.IsNullOrWhiteSpace(photo.BlobRef))
            return new NotFoundObjectResult("No file is stored for this photo.");

        var blob = await photoStore.OpenAsync(photo.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var inline = request.Query.TryGetValue("inline", out var inlineValue)
            && (inlineValue == "1" || string.Equals(inlineValue, "true", StringComparison.OrdinalIgnoreCase));

        var result = new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(photo.ContentType) ? blob.ContentType : photo.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!inline)
            result.FileDownloadName = string.IsNullOrWhiteSpace(photo.FileName) ? progressPhotoId : photo.FileName;
        return result;
    }
}
