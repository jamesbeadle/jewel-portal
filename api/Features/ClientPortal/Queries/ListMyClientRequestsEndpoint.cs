using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/requests — RFIs on the signed-in client's projects. The
/// client id comes from the session via ClientScope, never from the caller.</summary>
public sealed class ListMyClientRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyClientRequests, IReadOnlyList<ClientPortalRequest>> handler;

    public ListMyClientRequestsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListMyClientRequests, IReadOnlyList<ClientPortalRequest>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("ListMyClientRequests")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/requests")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new ListMyClientRequests(clientId), cancellationToken));
    }
}
