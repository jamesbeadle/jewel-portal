using Jewel.JPMS.Api.Features.Progress.Photos;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// POST /api/site-photos — multipart/form-data upload of up to
/// <see cref="ProgressPhotoLimits.MaxImagesPerBatch"/> image files (JPEG, PNG, HEIC) into the
/// company-wide pool. Answers one outcome per image — stored, already in the pool, or failed
/// on its own. The Site photos page's drop zone; the assistant never uploads here (a tool call
/// carries words), it matches and files what a person dropped.
/// </summary>
public sealed class UploadSitePhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SitePhotoIntake intake;

    public UploadSitePhotosEndpoint(SignedInUserResolver users, SitePhotoIntake intake)
    {
        this.users = users;
        this.intake = intake;
    }

    [Function(nameof(UploadSitePhotosEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-photos")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Contributors.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var read = await ProgressPhotoFormReader.ReadAsync(form, cancellationToken);
        if (read.Refusal is not null) return new BadRequestObjectResult(read.Refusal);

        var result = await intake.TakeAsync(read.Images, signedInUser.Email, cancellationToken);
        return new OkObjectResult(result);
    }
}
