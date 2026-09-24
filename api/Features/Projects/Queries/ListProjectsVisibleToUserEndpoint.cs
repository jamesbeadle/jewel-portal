using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Queries;

public sealed class ListProjectsVisibleToUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectsVisibleToUser, IReadOnlyList<Project>> handler;

    public ListProjectsVisibleToUserEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectsVisibleToUser, IReadOnlyList<Project>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Every internal role reads the whole list; an architect reads the projects that name their
    // practice (ArchitectProjects). Clients and subcontractors use their own scoped portal reads.
    private static readonly RoleSet RolesThatMayListProjects = JpmsRoleSets.InternalAndArchitect;

    [Function(nameof(ListProjectsVisibleToUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayListProjects.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var query = QueryFor(signedInUser);
        if (query is null) return new OkObjectResult(Array.Empty<Project>());

        var projects = await handler.HandleAsync(query, request.HttpContext.RequestAborted);
        return new OkObjectResult(projects);
    }

    // An internal role sees everything; a login whose only reach is Role.Architect sees its
    // practice's projects, and an architect login never linked to a practice sees nothing.
    private static ListProjectsVisibleToUser? QueryFor(SignedInUser signedInUser)
    {
        var isInternal = JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles);
        if (isInternal) return new ListProjectsVisibleToUser();
        var architectId = ArchitectScope.OwnArchitectId(signedInUser);
        return architectId is null ? null : new ListProjectsVisibleToUser(architectId);
    }
}
