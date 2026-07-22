using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

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

    // The project list is an internal read; external portal sessions use their own scoped endpoints.
    private static readonly RoleSet RolesThatMayListProjects = JpmsRoleSets.AllInternal;

    [Function(nameof(ListProjectsVisibleToUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayListProjects.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var projects = await handler.HandleAsync(new ListProjectsVisibleToUser(), request.HttpContext.RequestAborted);
        return new OkObjectResult(projects);
    }
}
