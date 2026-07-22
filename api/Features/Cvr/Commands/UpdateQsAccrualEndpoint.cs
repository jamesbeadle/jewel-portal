using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateQsAccrualEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateQsAccrualAuthorisation authorisation;
    private readonly UpdateQsAccrualValidation validation;
    private readonly ICommandHandler<UpdateQsAccrual, QsAccrual> handler;
    public UpdateQsAccrualEndpoint(SignedInUserResolver users, UpdateQsAccrualAuthorisation authorisation, UpdateQsAccrualValidation validation, ICommandHandler<UpdateQsAccrual, QsAccrual> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateQsAccrual))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "qs-accruals/{qsAccrualId}")] HttpRequest request, string qsAccrualId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateQsAccrual>();
        if (command is null) return new BadRequestResult();
        if (command.QsAccrualId != qsAccrualId) return new BadRequestObjectResult("Route qsAccrualId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
