using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/variation-orders — variations on the signed-in client's
/// projects that have reached them (Issued and later; Quoting is internal).</summary>
public sealed class ListMyClientVariationOrdersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyClientVariationOrders, IReadOnlyList<ClientPortalVariationOrder>> handler;

    public ListMyClientVariationOrdersEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListMyClientVariationOrders, IReadOnlyList<ClientPortalVariationOrder>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("ListMyClientVariationOrders")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/variation-orders")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new ListMyClientVariationOrders(clientId), cancellationToken));
    }
}
