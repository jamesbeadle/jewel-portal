using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>
/// POST /api/progress-updates/{progressUpdateId}/site-photos — JSON <c>{ sitePhotoIds: [...] }</c>.
/// Files pool photos onto the update, in the order given, after its current photos. Answers a
/// <see cref="SitePhotoFilingResult"/>: the update as it now stands and an outcome per photo.
/// </summary>
public sealed class FileSitePhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly FileSitePhotosAuthorisation authorisation;
    private readonly FileSitePhotosValidation validation;
    private readonly ICommandHandler<FileSitePhotos, SitePhotoFilingResult> handler;

    public FileSitePhotosEndpoint(
        SignedInUserResolver users,
        FileSitePhotosAuthorisation authorisation,
        FileSitePhotosValidation validation,
        ICommandHandler<FileSitePhotos, SitePhotoFilingResult> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(FileSitePhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "progress-updates/{progressUpdateId}/site-photos")] HttpRequest request,
        string progressUpdateId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);

        var posted = await request.ReadFromJsonAsync<FileSitePhotos>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A body naming the site photos is required.");

        var command = posted with { ProgressUpdateId = progressUpdateId, FiledByEmail = signedInUser.Email };
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

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
