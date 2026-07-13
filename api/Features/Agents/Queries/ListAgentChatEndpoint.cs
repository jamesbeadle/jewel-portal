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

    // Agent reads share the gate with agent commands: the roles allowed to operate agents.
    private static readonly RoleSet RolesThatMayReadAgents = AgentRoles.AllowedToOperateAgents;

    [Function(nameof(ListAgentChat))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/agents/{agentKey}/chat")] HttpRequest request, string requestId, string agentKey)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadAgents.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListAgentChat(requestId, agentKey), request.HttpContext.RequestAborted));
    }
}
