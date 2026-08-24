using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeleteDraftWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteDraftWorkOrderAuthorisation authorisation;
    private readonly DeleteDraftWorkOrderValidation validation;
    private readonly ICommandHandler<DeleteDraftWorkOrder, Acknowledgement> handler;

    public DeleteDraftWorkOrderEndpoint(
        SignedInUserResolver users,
        DeleteDraftWorkOrderAuthorisation authorisation,
        DeleteDraftWorkOrderValidation validation,
        ICommandHandler<DeleteDraftWorkOrder, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(DeleteDraftWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}/work-orders/{workOrderId}")] HttpRequest request,
        string projectId, string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteDraftWorkOrder(projectId, workOrderId);

        // Readable 403 rather than ForbidResult — see CreateCostCentreGroupEndpoint.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to delete draft work orders.")
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
