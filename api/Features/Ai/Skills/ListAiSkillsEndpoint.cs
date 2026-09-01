using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>GET /api/ai/skills — every skill, for the admin page's list.</summary>
public sealed class ListAiSkillsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAiSkills, IReadOnlyList<SkillSummary>> handler;

    public ListAiSkillsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListAiSkills, IReadOnlyList<SkillSummary>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListAiSkills))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/skills")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SkillRoles.ManageSkills.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new ListAiSkills(), cancellationToken));
    }
}
