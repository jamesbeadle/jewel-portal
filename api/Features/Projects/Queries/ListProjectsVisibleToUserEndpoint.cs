using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.Parties;
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

    // Every internal role reads the whole list; the project's parties read their own projects —
    // an architect those naming their practice (ArchitectProjects), a client its own
    // (ClientProjects) — with the commercial fields stripped (Parties/PartyReads).
    private static readonly RoleSet RolesThatMayListProjects = JpmsRoleSets.DeliveryTeamAndParties;

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
        return new OkObjectResult(projects.Select(project => project.AsReadBy(signedInUser)).ToList());
    }

    // An internal role sees everything; a linked client or architect sees its own projects, and
    // a party login never linked to its client or practice sees nothing.
    private static ListProjectsVisibleToUser? QueryFor(SignedInUser signedInUser)
    {
        if (PartyReads.IsInternal(signedInUser)) return new ListProjectsVisibleToUser();
        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is not null) return new ListProjectsVisibleToUser(ClientId: clientId);
        var architectId = ArchitectScope.OwnArchitectId(signedInUser);
        return architectId is null ? null : new ListProjectsVisibleToUser(architectId);
    }
}
