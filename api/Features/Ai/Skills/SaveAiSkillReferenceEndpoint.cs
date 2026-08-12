using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>POST /api/ai/skills/references — create or update one reference document.</summary>
public sealed class SaveAiSkillReferenceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SaveAiSkillReferenceAuthorisation authorisation;
    private readonly SaveAiSkillReferenceValidation validation;
    private readonly ICommandHandler<SaveAiSkillReference, Acknowledgement> handler;

    public SaveAiSkillReferenceEndpoint(
        SignedInUserResolver users,
        SaveAiSkillReferenceAuthorisation authorisation,
        SaveAiSkillReferenceValidation validation,
        ICommandHandler<SaveAiSkillReference, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SaveAiSkillReference))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/skills/references")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SaveAiSkillReference>();
        if (body is null) return new BadRequestResult();

        var command = body with { SavedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new[] { ex.Message });
        }
    }
}
