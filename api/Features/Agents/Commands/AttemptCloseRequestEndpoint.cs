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

        // The body carries the client's command (notably the user-chosen close date); the route and
        // the signed-in user stay authoritative for the id and the closer. Tolerate an absent body
        // so callers that post nothing still close as at now.
        AttemptCloseRequest? body = null;
        try { body = await request.ReadFromJsonAsync<AttemptCloseRequest>(); } catch { /* no or malformed body */ }
        var command = new AttemptCloseRequest(requestId, signedInUser.Email, body?.ClosedAt);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
