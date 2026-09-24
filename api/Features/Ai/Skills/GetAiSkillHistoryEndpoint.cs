using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>GET /api/ai/skills/{skillKey}/history — every version of a skill and its references.</summary>
public sealed class GetAiSkillHistoryEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetAiSkillHistory, SkillHistory?> handler;

    public GetAiSkillHistoryEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetAiSkillHistory, SkillHistory?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetAiSkillHistory))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/skills/{skillKey}/history")] HttpRequest request,
        string skillKey)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SkillRoles.ManageSkills.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var history = await handler.HandleAsync(new GetAiSkillHistory(skillKey), cancellationToken);
        return history is null ? new NotFoundResult() : new OkObjectResult(history);
    }
}
