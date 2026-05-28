using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListWorkOrdersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListWorkOrders, IReadOnlyList<WorkOrder>> handler;

    public ListWorkOrdersEndpoint(SignedInUserResolver users, IQueryHandler<ListWorkOrders, IReadOnlyList<WorkOrder>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListWorkOrders))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-orders")] HttpRequest request)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListWorkOrders(), request.HttpContext.RequestAborted));
    }
}
