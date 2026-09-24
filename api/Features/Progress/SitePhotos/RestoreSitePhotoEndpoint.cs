using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>POST /api/site-photos/{sitePhotoId}/restore — back out of the archive.</summary>
public sealed class RestoreSitePhotoEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RestoreSitePhotoAuthorisation authorisation;
    private readonly ICommandHandler<RestoreSitePhoto, Acknowledgement> handler;

    public RestoreSitePhotoEndpoint(
        SignedInUserResolver users,
        RestoreSitePhotoAuthorisation authorisation,
        ICommandHandler<RestoreSitePhoto, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(RestoreSitePhoto))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-photos/{sitePhotoId}/restore")] HttpRequest request,
        string sitePhotoId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RestoreSitePhoto(sitePhotoId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException guard)
        {
            return new NotFoundObjectResult(guard.Message);
        }
    }
}
