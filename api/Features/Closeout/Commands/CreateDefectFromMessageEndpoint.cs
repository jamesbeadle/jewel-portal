using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class CreateDefectFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateDefectFromMessageAuthorisation authorisation;
    private readonly CreateDefectFromMessageValidation validation;
    private readonly ICommandHandler<CreateDefectFromMessage, Defect> handler;

    public CreateDefectFromMessageEndpoint(
        SignedInUserResolver users,
        CreateDefectFromMessageAuthorisation authorisation,
        CreateDefectFromMessageValidation validation,
        ICommandHandler<CreateDefectFromMessage, Defect> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateDefectFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-defect")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateDefectFromMessage>();
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to raise defects.")
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
