using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RejectWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RejectWorkOrderAuthorisation authorisation;
    private readonly RejectWorkOrderValidation validation;
    private readonly ICommandHandler<RejectWorkOrder, WorkOrder> handler;

    public RejectWorkOrderEndpoint(
        SignedInUserResolver users,
        RejectWorkOrderAuthorisation authorisation,
        RejectWorkOrderValidation validation,
        ICommandHandler<RejectWorkOrder, WorkOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RejectWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/work-orders/{workOrderId}/reject")] HttpRequest request,
        string projectId, string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RejectWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to reject work orders.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (already decided, wrong project) read back to the
            // user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
