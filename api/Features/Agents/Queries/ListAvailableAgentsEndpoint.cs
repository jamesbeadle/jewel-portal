using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListAvailableAgentsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAvailableAgents, IReadOnlyList<AgentDescriptor>> handler;
    public ListAvailableAgentsEndpoint(SignedInUserResolver users, IQueryHandler<ListAvailableAgents, IReadOnlyList<AgentDescriptor>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListAvailableAgents))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agents")] HttpRequest request)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListAvailableAgents(), request.HttpContext.RequestAborted));
    }
}
