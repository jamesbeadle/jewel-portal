using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>GET /api/ai/skills/{skillKey} — one skill with its body and references.</summary>
public sealed class GetAiSkillEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetAiSkill, SkillDetail?> handler;

    public GetAiSkillEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetAiSkill, SkillDetail?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetAiSkill))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/skills/{skillKey}")] HttpRequest request,
        string skillKey)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SkillRoles.ManageSkills.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var skill = await handler.HandleAsync(new GetAiSkill(skillKey), cancellationToken);
        return skill is null ? new NotFoundResult() : new OkObjectResult(skill);
    }
}
