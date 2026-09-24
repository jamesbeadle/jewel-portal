using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

/// <summary>POST /api/ai/skills/restore — save an earlier version of a skill or reference as a new one.</summary>
public sealed class RestoreAiSkillVersionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RestoreAiSkillVersionAuthorisation authorisation;
    private readonly RestoreAiSkillVersionValidation validation;
    private readonly ICommandHandler<RestoreAiSkillVersion, Acknowledgement> handler;

    public RestoreAiSkillVersionEndpoint(
        SignedInUserResolver users,
        RestoreAiSkillVersionAuthorisation authorisation,
        RestoreAiSkillVersionValidation validation,
        ICommandHandler<RestoreAiSkillVersion, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RestoreAiSkillVersion))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai/skills/restore")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<RestoreAiSkillVersion>();
        if (body is null) return new BadRequestResult();

        var command = body with { RestoredByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}
