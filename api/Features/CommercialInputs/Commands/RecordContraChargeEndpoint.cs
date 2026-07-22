using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordContraChargeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordContraChargeAuthorisation authorisation;
    private readonly RecordContraChargeValidation validation;
    private readonly ICommandHandler<RecordContraCharge, ContraCharge> handler;

    public RecordContraChargeEndpoint(
        SignedInUserResolver users,
        RecordContraChargeAuthorisation authorisation,
        RecordContraChargeValidation validation,
        ICommandHandler<RecordContraCharge, ContraCharge> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordContraCharge))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/contra-charges")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordContraCharge>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var contraCharge = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(contraCharge);
    }
}
