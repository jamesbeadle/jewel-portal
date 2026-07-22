using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListDayworksForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>> handler;

    public ListDayworksForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial input reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListDayworksForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/dayworks")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var dayworks = await handler.HandleAsync(new ListDayworksForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(dayworks);
    }
}
