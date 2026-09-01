using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendWorkOrderPoEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendWorkOrderPoEmailAuthorisation authorisation;
    private readonly SendWorkOrderPoEmailValidation validation;
    private readonly ICommandHandler<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome> handler;

    public SendWorkOrderPoEmailEndpoint(SignedInUserResolver users, SendWorkOrderPoEmailAuthorisation authorisation, SendWorkOrderPoEmailValidation validation, ICommandHandler<SendWorkOrderPoEmail, WorkOrderPoEmailOutcome> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SendWorkOrderPoEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/{workOrderId}/send-po-email")] HttpRequest request,
        string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SendWorkOrderPoEmail>();
        if (command is null) return new BadRequestResult();
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (draft/rejected order, supplier without a directory email,
            // mailbox staging failure) read back to the user rather than a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
