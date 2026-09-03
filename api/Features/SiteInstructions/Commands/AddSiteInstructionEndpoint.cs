using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class AddSiteInstructionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddSiteInstructionAuthorisation authorisation;
    private readonly AddSiteInstructionValidation validation;
    private readonly ICommandHandler<AddSiteInstruction, SiteInstruction> handler;
    public AddSiteInstructionEndpoint(SignedInUserResolver users, AddSiteInstructionAuthorisation authorisation, AddSiteInstructionValidation validation, ICommandHandler<AddSiteInstruction, SiteInstruction> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AddSiteInstruction))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/site-instructions")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AddSiteInstruction>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
