using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareWorkOrderEmailDraftEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PrepareWorkOrderEmailDraftAuthorisation authorisation;
    private readonly PrepareWorkOrderEmailDraftValidation validation;
    private readonly ICommandHandler<PrepareWorkOrderEmailDraft, WorkOrderEmailDraft> handler;

    public PrepareWorkOrderEmailDraftEndpoint(SignedInUserResolver users, PrepareWorkOrderEmailDraftAuthorisation authorisation, PrepareWorkOrderEmailDraftValidation validation, ICommandHandler<PrepareWorkOrderEmailDraft, WorkOrderEmailDraft> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(PrepareWorkOrderEmailDraft))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-orders/{workOrderId}/draft-email")] HttpRequest request,
        string workOrderId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<PrepareWorkOrderEmailDraft>();
        if (command is null) return new BadRequestResult();
        if (command.WorkOrderId != workOrderId) return new BadRequestObjectResult("Route workOrderId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
