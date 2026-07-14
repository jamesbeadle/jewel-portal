using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RecodeWorkOrderLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecodeWorkOrderLineAuthorisation authorisation;
    private readonly RecodeWorkOrderLineValidation validation;
    private readonly ICommandHandler<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>> handler;

    public RecodeWorkOrderLineEndpoint(
        SignedInUserResolver users,
        RecodeWorkOrderLineAuthorisation authorisation,
        RecodeWorkOrderLineValidation validation,
        ICommandHandler<RecodeWorkOrderLine, IReadOnlyList<WorkOrderLine>> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecodeWorkOrderLine))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/work-order-lines/{lineId}/recode")] HttpRequest request,
        string projectId, string lineId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecodeWorkOrderLine>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (command.WorkOrderLineId != lineId) return new BadRequestObjectResult("Route lineId does not match body.");

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to re-code work order lines.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule refusals (parts don't total the line, unknown centre, wrong
            // project) read back to the user rather than surfacing as a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
