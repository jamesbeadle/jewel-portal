using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AttemptCloseRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AttemptCloseRequestAuthorisation authorisation;
    private readonly AttemptCloseRequestValidation validation;
    private readonly ICommandHandler<AttemptCloseRequest, RequestCloseOutcome> handler;
    public AttemptCloseRequestEndpoint(SignedInUserResolver users, AttemptCloseRequestAuthorisation authorisation, AttemptCloseRequestValidation validation, ICommandHandler<AttemptCloseRequest, RequestCloseOutcome> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AttemptCloseRequest))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/agents/close")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new AttemptCloseRequest(requestId, signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
