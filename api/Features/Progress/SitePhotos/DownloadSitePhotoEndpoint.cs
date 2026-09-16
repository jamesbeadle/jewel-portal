using Jewel.JPMS.Api.Features.Progress.Storage;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>GET /api/site-photos/{sitePhotoId}/file[?inline=1] — streams a pool photo's stored
/// file through the API, exactly as a progress photo is served.</summary>
public sealed class DownloadSitePhotoEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public DownloadSitePhotoEndpoint(SignedInUserResolver users, JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
    }

    [Function(nameof(DownloadSitePhotoEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site-photos/{sitePhotoId}/file")] HttpRequest request,
        string sitePhotoId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var photo = await context.SitePhotos.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SitePhotoId == sitePhotoId, cancellationToken);
        if (photo is null || string.IsNullOrWhiteSpace(photo.BlobRef))
            return new NotFoundObjectResult("No file is stored for this photo.");

        var blob = await photoStore.OpenAsync(photo.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");

        var result = new FileStreamResult(blob.Content, string.IsNullOrWhiteSpace(photo.ContentType) ? blob.ContentType : photo.ContentType)
        {
            EnableRangeProcessing = true
        };
        if (!QueryFlags.IsSet(request, "inline"))
            result.FileDownloadName = string.IsNullOrWhiteSpace(photo.FileName) ? sitePhotoId : photo.FileName;
        return result;
    }
}
