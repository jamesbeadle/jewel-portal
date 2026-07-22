using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class UpdateBoqLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateBoqLineAuthorisation authorisation;
    private readonly UpdateBoqLineValidation validation;
    private readonly ICommandHandler<UpdateBoqLine, BoqLineItem> handler;

    public UpdateBoqLineEndpoint(
        SignedInUserResolver users,
        UpdateBoqLineAuthorisation authorisation,
        UpdateBoqLineValidation validation,
        ICommandHandler<UpdateBoqLine, BoqLineItem> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateBoqLine))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "boq-lines/{boqLineItemId}")] HttpRequest request,
        string boqLineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpdateBoqLine>();
        if (command is null) return new BadRequestResult();
        if (command.BoqLineItemId != boqLineItemId) return new BadRequestObjectResult("Route boqLineItemId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var line = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(line);
    }
}
