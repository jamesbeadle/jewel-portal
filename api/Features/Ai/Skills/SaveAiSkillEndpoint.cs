using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>POST /api/ai/skills — create or update a skill (an update is a new version).</summary>
public sealed class SaveAiSkillEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveAiSkillAuthorisation authorisation;
    private readonly SaveAiSkillValidation validation;
    private readonly ICommandHandler<SaveAiSkill, Acknowledgement> handler;

    public SaveAiSkillEndpoint(
        SignedInUserResolver users,
        SaveAiSkillAuthorisation authorisation,
        SaveAiSkillValidation validation,
        ICommandHandler<SaveAiSkill, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SaveAiSkill))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/skills")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SaveAiSkill>();
        if (body is null) return new BadRequestResult();

        var command = body with { SavedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
