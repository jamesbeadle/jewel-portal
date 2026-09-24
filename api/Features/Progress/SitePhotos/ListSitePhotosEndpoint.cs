using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>GET /api/site-photos[?unfiled=1][?archived=1][?projectId=…] — the pool, newest upload
/// first, each filed photo with the project and day it went to.</summary>
public sealed class ListSitePhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSitePhotos, IReadOnlyList<SitePhoto>> handler;

    public ListSitePhotosEndpoint(SignedInUserResolver users, IQueryHandler<ListSitePhotos, IReadOnlyList<SitePhoto>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSitePhotos))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "site-photos")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!ProgressRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var photos = await handler.HandleAsync(
            new ListSitePhotos(QueryFlags.IsSet(request, "unfiled"), QueryFlags.IsSet(request, "archived"),
                ProjectIdOf(request)),
            request.HttpContext.RequestAborted);
        return new OkObjectResult(photos);
    }

    private static string? ProjectIdOf(HttpRequest request) =>
        request.Query.TryGetValue("projectId", out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString() : null;
}

internal static class QueryFlags
{
    public static bool IsSet(HttpRequest request, string name) =>
        request.Query.TryGetValue(name, out var value)
        && (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
}
