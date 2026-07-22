using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PostRequestMessageAuthorisation authorisation;
    private readonly PostRequestMessageValidation validation;
    private readonly ICommandHandler<PostRequestMessage, RequestMessage> handler;
    public PostRequestMessageEndpoint(SignedInUserResolver users, PostRequestMessageAuthorisation authorisation, PostRequestMessageValidation validation, ICommandHandler<PostRequestMessage, RequestMessage> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(PostRequestMessage))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/messages")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<PostRequestMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.RequestId != requestId) return new BadRequestObjectResult("Route requestId does not match body.");

        // The author is always the signed-in user — never trusted from the client body.
        var command = posted with { AuthorEmail = signedInUser.Email, AuthorName = signedInUser.DisplayName };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
