using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>GET /api/ai/action-skills — the action registry with its skill attachments, for the
/// AI Actions admin page. Same gate as the skill store: the wiring is doctrine curation.</summary>
public sealed class GetAiActionCatalogueEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetAiActionCatalogue, AiActionCatalogue> handler;

    public GetAiActionCatalogueEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetAiActionCatalogue, AiActionCatalogue> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetAiActionCatalogue))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ai/action-skills")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SkillRoles.ManageSkills.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await handler.HandleAsync(new GetAiActionCatalogue(), cancellationToken));
    }
}
