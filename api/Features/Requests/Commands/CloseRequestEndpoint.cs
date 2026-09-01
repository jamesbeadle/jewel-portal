using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class CloseRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CloseRequestAuthorisation authorisation;
    private readonly CloseRequestValidation validation;
    private readonly ICommandHandler<CloseRequest, RequestCloseOutcome> handler;
    public CloseRequestEndpoint(SignedInUserResolver users, CloseRequestAuthorisation authorisation, CloseRequestValidation validation, ICommandHandler<CloseRequest, RequestCloseOutcome> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(CloseRequest))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/close")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        // The body carries the client's command (notably the user-chosen close date); the route and
        // the signed-in user stay authoritative for the id and the closer. Tolerate an absent body
        // so callers that post nothing still close as at now.
        CloseRequest? body = null;
        try { body = await request.ReadFromJsonAsync<CloseRequest>(); } catch { /* no or malformed body */ }
        var command = new CloseRequest(requestId, signedInUser.Email, body?.ClosedAt);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
