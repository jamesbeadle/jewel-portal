using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class RemoveBoqLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveBoqLineAuthorisation authorisation;
    private readonly RemoveBoqLineValidation validation;
    private readonly ICommandHandler<RemoveBoqLine, Acknowledgement> handler;

    public RemoveBoqLineEndpoint(
        SignedInUserResolver users,
        RemoveBoqLineAuthorisation authorisation,
        RemoveBoqLineValidation validation,
        ICommandHandler<RemoveBoqLine, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RemoveBoqLine))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "boq-lines/{boqLineItemId}")] HttpRequest request,
        string boqLineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveBoqLine(boqLineItemId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(acknowledgement);
    }
}
