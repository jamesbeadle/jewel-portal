using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>DELETE /api/site-photos/{sitePhotoId}.</summary>
public sealed class DeleteSitePhotoEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteSitePhotoAuthorisation authorisation;
    private readonly ICommandHandler<DeleteSitePhoto, Acknowledgement> handler;

    public DeleteSitePhotoEndpoint(
        SignedInUserResolver users,
        DeleteSitePhotoAuthorisation authorisation,
        ICommandHandler<DeleteSitePhoto, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteSitePhoto))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "site-photos/{sitePhotoId}")] HttpRequest request,
        string sitePhotoId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteSitePhoto(sitePhotoId);
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
