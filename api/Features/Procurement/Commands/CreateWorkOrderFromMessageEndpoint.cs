using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateWorkOrderFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateWorkOrderFromMessageAuthorisation authorisation;
    private readonly CreateWorkOrderFromMessageValidation validation;
    private readonly ICommandHandler<CreateWorkOrderFromMessage, WorkOrder> handler;

    public CreateWorkOrderFromMessageEndpoint(
        SignedInUserResolver users,
        CreateWorkOrderFromMessageAuthorisation authorisation,
        CreateWorkOrderFromMessageValidation validation,
        ICommandHandler<CreateWorkOrderFromMessage, WorkOrder> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(CreateWorkOrderFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-work-order")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateWorkOrderFromMessage>();
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId) || string.IsNullOrWhiteSpace(posted.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // The raiser is always the signed-in user — never trusted from the client body.
        var command = posted with { RaisedByEmail = signedInUser.Email };

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to raise work orders.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (unknown project / subcontractor / cost centre, or an email
            // that can't be read back for tagging) read back to the user rather than a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
