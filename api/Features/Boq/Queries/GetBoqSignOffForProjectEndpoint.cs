using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Boq.Queries;

public sealed class GetBoqSignOffForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBoqSignOffForProject, BoqSignOff?> handler;

    public GetBoqSignOffForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetBoqSignOffForProject, BoqSignOff?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // BoQ reads are internal-only; external portal logins have no view of the priced bill.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(GetBoqSignOffForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/boq/sign-off")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var signOff = await handler.HandleAsync(new GetBoqSignOffForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(signOff);
    }
}
