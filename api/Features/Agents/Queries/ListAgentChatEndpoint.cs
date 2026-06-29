using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListAgentChatEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAgentChat, IReadOnlyList<AgentChatMessage>> handler;
    public ListAgentChatEndpoint(SignedInUserResolver users, IQueryHandler<ListAgentChat, IReadOnlyList<AgentChatMessage>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListAgentChat))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/agents/{agentKey}/chat")] HttpRequest request, string requestId, string agentKey)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListAgentChat(requestId, agentKey), request.HttpContext.RequestAborted));
    }
}
