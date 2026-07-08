using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListProjectWorkOrdersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>> handler;

    public ListProjectWorkOrdersEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListProjectWorkOrders))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/work-orders")] HttpRequest request,
        string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListProjectWorkOrders(projectId), request.HttpContext.RequestAborted));
    }
}
