using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateManualWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateManualWorkOrderAuthorisation authorisation;
    private readonly CreateManualWorkOrderValidation validation;
    private readonly ICommandHandler<CreateManualWorkOrder, WorkOrder> handler;

    public CreateManualWorkOrderEndpoint(
        SignedInUserResolver users,
        CreateManualWorkOrderAuthorisation authorisation,
        CreateManualWorkOrderValidation validation,
        ICommandHandler<CreateManualWorkOrder, WorkOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateManualWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/work-orders")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CreateManualWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

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
            // Business-rule refusals (unknown project / subcontractor / cost centre)
            // read back to the user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
