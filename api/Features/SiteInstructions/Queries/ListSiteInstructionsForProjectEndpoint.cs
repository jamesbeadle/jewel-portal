using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Queries;

public sealed class ListSiteInstructionsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSiteInstructionsForProject, IReadOnlyList<SiteInstruction>> handler;
    public ListSiteInstructionsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListSiteInstructionsForProject, IReadOnlyList<SiteInstruction>> handler) { this.users = users; this.handler = handler; }

    // Site instructions are internal-only; external portal logins have no view here.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListSiteInstructionsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/site-instructions")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListSiteInstructionsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
