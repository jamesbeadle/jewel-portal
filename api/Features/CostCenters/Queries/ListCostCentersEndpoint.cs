using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CostCenters.Queries;

public sealed class ListCostCentersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>> handler;
    public ListCostCentersEndpoint(SignedInUserResolver users, IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListCostCenters))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cost-centers")] HttpRequest request)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListCostCenters(), request.HttpContext.RequestAborted));
    }
}
