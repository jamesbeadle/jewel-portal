using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class CreateSiteInstructionFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateSiteInstructionFromMessageAuthorisation authorisation;
    private readonly CreateSiteInstructionFromMessageValidation validation;
    private readonly ICommandHandler<CreateSiteInstructionFromMessage, SiteInstruction> handler;

    public CreateSiteInstructionFromMessageEndpoint(
        SignedInUserResolver users,
        CreateSiteInstructionFromMessageAuthorisation authorisation,
        CreateSiteInstructionFromMessageValidation validation,
        ICommandHandler<CreateSiteInstructionFromMessage, SiteInstruction> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateSiteInstructionFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-site-instruction")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateSiteInstructionFromMessage>();
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to raise site instructions.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (an email that can't be read back for tagging) read back to
            // the user rather than a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
