using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/requests/{requestId} — one RFI, null when it isn't on one
/// of the signed-in client's projects.</summary>
public sealed class GetMyClientRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetMyClientRequest, ClientPortalRequest?> handler;

    public GetMyClientRequestEndpoint(
        SignedInUserResolver users, IQueryHandler<GetMyClientRequest, ClientPortalRequest?> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("GetMyClientRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/requests/{requestId}")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new GetMyClientRequest(requestId, clientId), cancellationToken));
    }
}
