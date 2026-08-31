using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/variation-orders/{voId} — one variation order, null when
/// it isn't on one of the signed-in client's projects or hasn't reached them yet.</summary>
public sealed class GetMyClientVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetMyClientVariationOrder, ClientPortalVariationOrder?> handler;

    public GetMyClientVariationOrderEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetMyClientVariationOrder, ClientPortalVariationOrder?> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("GetMyClientVariationOrder")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/variation-orders/{voId}")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(
            new GetMyClientVariationOrder(voId, clientId), cancellationToken));
    }
}
