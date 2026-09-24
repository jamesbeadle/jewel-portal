using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// POST /api/site-photos/archive — JSON <c>{ photos: [{ sitePhotoId, reason, note }], projectId,
/// periodEnd }</c>. Sets pool photos aside from the weekly report and keeps them; answers one
/// outcome per photo.
/// </summary>
public sealed class ArchiveSitePhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ArchiveSitePhotosAuthorisation authorisation;
    private readonly ArchiveSitePhotosValidation validation;
    private readonly ICommandHandler<ArchiveSitePhotos, SitePhotoArchivingResult> handler;

    public ArchiveSitePhotosEndpoint(
        SignedInUserResolver users,
        ArchiveSitePhotosAuthorisation authorisation,
        ArchiveSitePhotosValidation validation,
        ICommandHandler<ArchiveSitePhotos, SitePhotoArchivingResult> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ArchiveSitePhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-photos/archive")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var posted = await request.ReadFromJsonAsync<ArchiveSitePhotos>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A body naming the site photos is required.");

        var command = posted with { ArchivedByEmail = signedInUser.Email };
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
