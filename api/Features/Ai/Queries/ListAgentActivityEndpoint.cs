using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Ai.Queries;

/// <summary>
/// GET /api/agents/activity — the agent activity log, newest first.
///
/// <para>Optional query string: <c>projectId</c>, <c>agentKey</c>, <c>autonomousOnly=1</c>,
/// <c>take</c>. Gated to administrators and directors: the log carries spend, and the people who
/// authorise the spend are the people who should see it.</para>
/// </summary>
public sealed class ListAgentActivityEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAgentActivity, IReadOnlyList<AgentActivity>> handler;

    public ListAgentActivityEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListAgentActivity, IReadOnlyList<AgentActivity>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListAgentActivity))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agents/activity")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AiRoles.AllowedToUseAssistant.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var projectId = Text(request, "projectId");
        var agentKey = Text(request, "agentKey");
        var autonomousOnly = Flag(request, "autonomousOnly");
        var take = int.TryParse(Text(request, "take"), out var parsed) ? parsed : 200;

        var query = new ListAgentActivity(projectId, agentKey, autonomousOnly, take);
        return new OkObjectResult(await handler.HandleAsync(query, cancellationToken));
    }

    private static string? Text(HttpRequest request, string name) =>
        request.Query.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;

    private static bool? Flag(HttpRequest request, string name)
    {
        var raw = Text(request, name);
        if (raw is null) return null;
        return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }
}
