using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateManualWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateManualWorkOrderAuthorisation authorisation;
    private readonly UpdateManualWorkOrderValidation validation;
    private readonly ICommandHandler<UpdateManualWorkOrder, WorkOrder> handler;

    public UpdateManualWorkOrderEndpoint(
        SignedInUserResolver users,
        UpdateManualWorkOrderAuthorisation authorisation,
        UpdateManualWorkOrderValidation validation,
        ICommandHandler<UpdateManualWorkOrder, WorkOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateManualWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/work-orders/{workOrderId}")] HttpRequest request,
        string projectId,
        string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateManualWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to edit work orders.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (not a manual order, paid lines, unknown centres)
            // read back to the user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
