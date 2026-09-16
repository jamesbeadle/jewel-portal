using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>POST /api/site-photos/match — JSON <c>{ contentHashes: [...] }</c>: which pool photos a
/// set of SHA-256 fingerprints are. A read that takes a body, because a week's worth of hashes
/// does not fit a query string.</summary>
public sealed class MatchSitePhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<MatchSitePhotos, SitePhotoMatches> handler;

    public MatchSitePhotosEndpoint(SignedInUserResolver users, IQueryHandler<MatchSitePhotos, SitePhotoMatches> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(MatchSitePhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-photos/match")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var query = await request.ReadFromJsonAsync<MatchSitePhotos>(cancellationToken);
        if (query is null || query.ContentHashes is null) return new BadRequestObjectResult("contentHashes is required.");

        return new OkObjectResult(await handler.HandleAsync(query, cancellationToken));
    }
}
