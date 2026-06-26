using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler;
    public ListRequestMessagesEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListRequestMessages))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/messages")] HttpRequest request, string requestId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListRequestMessages(requestId), request.HttpContext.RequestAborted));
    }
}
