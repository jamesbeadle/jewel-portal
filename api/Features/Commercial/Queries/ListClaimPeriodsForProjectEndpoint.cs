using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListClaimPeriodsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListClaimPeriodsForProject, IReadOnlyList<ClaimPeriod>> handler;

    public ListClaimPeriodsForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListClaimPeriodsForProject, IReadOnlyList<ClaimPeriod>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Commercial reads are internal-only; external portal logins have no view of project money.
    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    [Function(nameof(ListClaimPeriodsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/claim-periods")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var claimPeriods = await handler.HandleAsync(new ListClaimPeriodsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(claimPeriods);
    }
}
