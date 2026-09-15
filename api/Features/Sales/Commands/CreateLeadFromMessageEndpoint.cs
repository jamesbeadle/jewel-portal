using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// POST mailbox/message/create-lead — the Sales pane's "create new" (2026-09-15). The message id
// travels in the body (Graph ids contain path-unsafe characters), as every from-message route does.
public sealed class CreateLeadFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly CreateLeadFromMessageAuthorisation authorisation;
    private readonly CreateLeadFromMessageValidation validation;
    private readonly ICommandHandler<CreateLeadFromMessage, Lead> handler;

    public CreateLeadFromMessageEndpoint(
        SignedInUserResolver users,
        AuditActor auditActor,
        CreateLeadFromMessageAuthorisation authorisation,
        CreateLeadFromMessageValidation validation,
        ICommandHandler<CreateLeadFromMessage, Lead> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateLeadFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-lead")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateLeadFromMessage>();
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId))
            return new BadRequestObjectResult("messageId is required.");

        // The actor reaches the handler (the lead's default owner, the audit rows) through the
        // scoped AuditActor, never from the body.
        auditActor.Email = signedInUser.Email;
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to raise a lead from an email.")
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
