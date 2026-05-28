using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateWorkOrderAuthorisation authorisation;
    private readonly UpdateWorkOrderValidation validation;
    private readonly ICommandHandler<UpdateWorkOrder, WorkOrder> handler;

    public UpdateWorkOrderEndpoint(SignedInUserResolver users, UpdateWorkOrderAuthorisation authorisation, UpdateWorkOrderValidation validation, ICommandHandler<UpdateWorkOrder, WorkOrder> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UpdateWorkOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "work-orders/{workOrderId}")] HttpRequest request,
        string workOrderId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateWorkOrder>();
        if (command is null) return new BadRequestResult();
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
