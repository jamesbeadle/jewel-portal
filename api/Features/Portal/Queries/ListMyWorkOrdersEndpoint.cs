using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Portal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

/// <summary>
/// GET /api/portal/my/work-orders — the signed-in subcontractor's issued work orders. The
/// subcontractor id comes from the session via SubcontractorScope, never from the client.
/// </summary>
public sealed class ListMyWorkOrdersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyWorkOrders, IReadOnlyList<PortalWorkOrder>> handler;

    public ListMyWorkOrdersEndpoint(
        SignedInUserResolver users, IQueryHandler<ListMyWorkOrders, IReadOnlyList<PortalWorkOrder>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("ListMyWorkOrders")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portal/my/work-orders")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new ForbidResult();

        return new OkObjectResult(await handler.HandleAsync(new ListMyWorkOrders(subcontractorId), cancellationToken));
    }
}
