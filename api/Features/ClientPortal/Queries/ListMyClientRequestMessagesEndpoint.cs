using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

/// <summary>GET /api/client-portal/my/requests/{requestId}/messages — the request's shared
/// in-app thread. Internal notes and email legs never travel here.</summary>
public sealed class ListMyClientRequestMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyClientRequestMessages, IReadOnlyList<RequestMessage>> handler;

    public ListMyClientRequestMessagesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListMyClientRequestMessages, IReadOnlyList<RequestMessage>> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("ListMyClientRequestMessages")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "client-portal/my/requests/{requestId}/messages")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(
            new ListMyClientRequestMessages(requestId, clientId), cancellationToken));
    }
}
