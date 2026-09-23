using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Thread.Queries;

/// <summary>
/// GET /api/hs-records/{hsRecordId}/comments — the thread; GET /api/hs-records/photos/{hsRecordPhotoId}/file
/// [?inline=1] — a photograph streamed through the API, exactly as a progress photo is served.
/// Both read for whoever reads the register.
/// </summary>
public sealed class HsRecordThreadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;
    private readonly IQueryHandler<ListHsRecordComments, IReadOnlyList<HsRecordComment>> thread;

    public HsRecordThreadEndpoints(
        SignedInUserResolver users, JpmsContext context, IProgressPhotoStore photoStore,
        IQueryHandler<ListHsRecordComments, IReadOnlyList<HsRecordComment>> thread)
    {
        this.users = users;
        this.context = context;
        this.photoStore = photoStore;
        this.thread = thread;
    }

    [Function(nameof(ListHsRecordComments))]
    public async Task<IActionResult> Thread(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records/{hsRecordId}/comments")] HttpRequest request, string hsRecordId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return new OkObjectResult(await thread.HandleAsync(new ListHsRecordComments(hsRecordId), cancellationToken));
    }

    [Function("DownloadHsRecordPhoto")]
    public async Task<IActionResult> Photo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-records/photos/{hsRecordPhotoId}/file")] HttpRequest request,
        string hsRecordPhotoId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var photo = await context.HsRecordPhotos.AsNoTracking()
            .FirstOrDefaultAsync(row => row.HsRecordPhotoId == hsRecordPhotoId, cancellationToken);
        if (photo is null) return new NotFoundObjectResult("No such photograph.");
        var blob = await photoStore.OpenAsync(photo.BlobRef, cancellationToken);
        if (blob is null) return new NotFoundObjectResult("The stored file could not be found.");
        var result = new FileStreamResult(blob.Content, photo.ContentType) { EnableRangeProcessing = true };
        if (!QueryFlags.IsSet(request, "inline")) result.FileDownloadName = photo.FileName;
        return result;
    }
}
