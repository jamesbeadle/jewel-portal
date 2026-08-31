using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>POST /api/ai/action-skills — replace one action's (or area's) attached-skill set.</summary>
public sealed class SaveAiActionSkillsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveAiActionSkillsAuthorisation authorisation;
    private readonly SaveAiActionSkillsValidation validation;
    private readonly ICommandHandler<SaveAiActionSkills, Acknowledgement> handler;

    public SaveAiActionSkillsEndpoint(
        SignedInUserResolver users,
        SaveAiActionSkillsAuthorisation authorisation,
        SaveAiActionSkillsValidation validation,
        ICommandHandler<SaveAiActionSkills, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SaveAiActionSkills))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/action-skills")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SaveAiActionSkills>();
        if (body is null) return new BadRequestResult();

        var command = body with { SavedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = await validation.CheckAsync(command, cancellationToken);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
