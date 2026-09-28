using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Queries;

/// <summary>GET /api/projects/{projectId}/site-links — the register's Site links panel, for the
/// roles that curate the register.</summary>
public sealed class ListSiteDrawingLinksForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSiteDrawingLinksForProject, IReadOnlyList<SiteDrawingLink>> handler;

    public ListSiteDrawingLinksForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListSiteDrawingLinksForProject, IReadOnlyList<SiteDrawingLink>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSiteDrawingLinksForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/site-links")] HttpRequest request,
        string projectId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SiteDrawingLinkRoles.Curators.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var links = await handler.HandleAsync(new ListSiteDrawingLinksForProject(projectId), cancellationToken);
        return new OkObjectResult(links);
    }
}
