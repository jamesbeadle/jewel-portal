using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class RemoveRequestAgentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveRequestAgentAuthorisation authorisation;
    private readonly RemoveRequestAgentValidation validation;
    private readonly ICommandHandler<RemoveRequestAgent, Acknowledgement> handler;
    public RemoveRequestAgentEndpoint(SignedInUserResolver users, RemoveRequestAgentAuthorisation authorisation, RemoveRequestAgentValidation validation, ICommandHandler<RemoveRequestAgent, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RemoveRequestAgent))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "requests/{requestId}/agents/{requestAgentId}")] HttpRequest request, string requestId, string requestAgentId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveRequestAgent(requestId, requestAgentId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
