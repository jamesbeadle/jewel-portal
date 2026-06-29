using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AssignAgentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AssignAgentAuthorisation authorisation;
    private readonly AssignAgentValidation validation;
    private readonly ICommandHandler<AssignAgent, RequestAgent> handler;
    public AssignAgentEndpoint(SignedInUserResolver users, AssignAgentAuthorisation authorisation, AssignAgentValidation validation, ICommandHandler<AssignAgent, RequestAgent> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AssignAgent))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/agents")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<AssignAgent>();
        if (posted is null) return new BadRequestResult();
        if (posted.RequestId != requestId) return new BadRequestObjectResult("Route requestId does not match body.");

        // The applier is always the signed-in user — never trusted from the client body.
        var command = posted with { AssignedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
