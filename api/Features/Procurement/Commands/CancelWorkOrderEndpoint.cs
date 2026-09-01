using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CancelWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CancelWorkOrderAuthorisation authorisation;
    private readonly CancelWorkOrderValidation validation;
    private readonly ICommandHandler<CancelWorkOrder, WorkOrder> handler;

    public CancelWorkOrderEndpoint(
        SignedInUserResolver users,
        CancelWorkOrderAuthorisation authorisation,
        CancelWorkOrderValidation validation,
        ICommandHandler<CancelWorkOrder, WorkOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CancelWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/work-orders/{workOrderId}/cancel")] HttpRequest request,
        string projectId, string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CancelWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Only a director or the finance director can cancel an issued work order.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (already closed, bills linked, money paid) read back to
            // the user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
