using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListCostCentreActualCostsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>> handler;

    public ListCostCentreActualCostsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListCostCentreActualCosts))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/cost-centres/{costCode}/actual-costs")] HttpRequest request,
        string projectId,
        string costCode)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var lines = await handler.HandleAsync(new ListCostCentreActualCosts(projectId, costCode), request.HttpContext.RequestAborted);
        return new OkObjectResult(lines);
    }
}
