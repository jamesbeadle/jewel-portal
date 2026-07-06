using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ResolveRequestRecipientsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ResolveRequestRecipients, RequestRecipientSet> handler;

    public ResolveRequestRecipientsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ResolveRequestRecipients, RequestRecipientSet> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ResolveRequestRecipients))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/recipients")] HttpRequest request,
        string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var recipients = await handler.HandleAsync(new ResolveRequestRecipients(requestId), request.HttpContext.RequestAborted);
        return new OkObjectResult(recipients);
    }
}
