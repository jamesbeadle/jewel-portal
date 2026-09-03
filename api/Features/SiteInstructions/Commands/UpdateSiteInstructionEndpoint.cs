using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class UpdateSiteInstructionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateSiteInstructionAuthorisation authorisation;
    private readonly UpdateSiteInstructionValidation validation;
    private readonly ICommandHandler<UpdateSiteInstruction, SiteInstruction> handler;
    public UpdateSiteInstructionEndpoint(SignedInUserResolver users, UpdateSiteInstructionAuthorisation authorisation, UpdateSiteInstructionValidation validation, ICommandHandler<UpdateSiteInstruction, SiteInstruction> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateSiteInstruction))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "site-instructions/{siteInstructionId}")] HttpRequest request, string siteInstructionId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateSiteInstruction>();
        if (command is null) return new BadRequestResult();
        if (command.SiteInstructionId != siteInstructionId) return new BadRequestObjectResult("Route siteInstructionId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
