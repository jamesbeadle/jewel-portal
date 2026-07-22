using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ResendRequestDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ResendRequestDocumentAuthorisation authorisation;
    private readonly ResendRequestDocumentValidation validation;
    private readonly ICommandHandler<ResendRequestDocument, Acknowledgement> handler;

    public ResendRequestDocumentEndpoint(SignedInUserResolver users, ResendRequestDocumentAuthorisation authorisation, ResendRequestDocumentValidation validation, ICommandHandler<ResendRequestDocument, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ResendRequestDocument))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/document/send")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        // Body is optional: { "recipientOverride": "someone@example.com" } for an ad-hoc resend.
        var posted = request.ContentLength is > 0
            ? await request.ReadFromJsonAsync<ResendRequestDocument>()
            : null;
        var command = new ResendRequestDocument(requestId, posted?.RecipientOverride);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
