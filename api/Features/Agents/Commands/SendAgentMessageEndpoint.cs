using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class SendAgentMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendAgentMessageAuthorisation authorisation;
    private readonly SendAgentMessageValidation validation;
    private readonly ICommandHandler<SendAgentMessage, AgentChatMessage> handler;
    public SendAgentMessageEndpoint(SignedInUserResolver users, SendAgentMessageAuthorisation authorisation, SendAgentMessageValidation validation, ICommandHandler<SendAgentMessage, AgentChatMessage> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(SendAgentMessage))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/agents/{agentKey}/messages")] HttpRequest request, string requestId, string agentKey)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SendAgentMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.RequestId != requestId || posted.AgentKey != agentKey)
            return new BadRequestObjectResult("Route does not match body.");

        // The author is always the signed-in user — never trusted from the client body.
        var command = posted with { AuthorEmail = signedInUser.Email, AuthorName = signedInUser.DisplayName };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
