using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/variation-orders/{voId}/messages — the order's shared
/// in-app thread. Internal notes never travel here.</summary>
public sealed class ListMyClientVariationOrderMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyClientVariationOrderMessages, IReadOnlyList<VariationOrderMessage>> handler;

    public ListMyClientVariationOrderMessagesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListMyClientVariationOrderMessages, IReadOnlyList<VariationOrderMessage>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("ListMyClientVariationOrderMessages")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/variation-orders/{voId}/messages")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(
            new ListMyClientVariationOrderMessages(voId, clientId), cancellationToken));
    }
}
