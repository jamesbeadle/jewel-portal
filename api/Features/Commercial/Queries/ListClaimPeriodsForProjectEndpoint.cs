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

    [Function(nameof(ListClaimPeriodsForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/claim-periods")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var claimPeriods = await handler.HandleAsync(new ListClaimPeriodsForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(claimPeriods);
    }
}
