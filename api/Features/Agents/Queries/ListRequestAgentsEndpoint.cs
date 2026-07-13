using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListRequestAgentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRequestAgents, IReadOnlyList<RequestAgent>> handler;
    public ListRequestAgentsEndpoint(SignedInUserResolver users, IQueryHandler<ListRequestAgents, IReadOnlyList<RequestAgent>> handler) { this.users = users; this.handler = handler; }

    // Agent reads share the gate with agent commands: the roles allowed to operate agents.
    private static readonly RoleSet RolesThatMayReadAgents = AgentRoles.AllowedToOperateAgents;

    [Function(nameof(ListRequestAgents))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "requests/{requestId}/agents")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadAgents.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListRequestAgents(requestId), request.HttpContext.RequestAborted));
    }
}
