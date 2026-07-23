using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/requests/{requestId}/voq — create the VOQ from a request's RFQ. The creator is the
/// signed-in user. The body is optional: when the UI has run the AI draft-review flow it carries
/// the reviewed title/description/estimated value; otherwise the VOQ inherits the request's own
/// title/description.
/// </summary>
public sealed class CreateVoqFromRfqEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateVoqFromRfqAuthorisation authorisation;
    private readonly CreateVoqFromRfqValidation validation;
    private readonly ICommandHandler<CreateVoqFromRfq, VariationOrder> handler;

    public CreateVoqFromRfqEndpoint(
        SignedInUserResolver users,
        CreateVoqFromRfqAuthorisation authorisation,
        CreateVoqFromRfqValidation validation,
        ICommandHandler<CreateVoqFromRfq, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateVoqFromRfq))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/voq")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // The body is optional (older callers send none); a malformed body is treated as absent so
        // the command falls back to the request's own title/description.
        CreateVoqFromRfq? body = null;
        try { body = await request.ReadFromJsonAsync<CreateVoqFromRfq>(cancellationToken); }
        catch (System.Text.Json.JsonException) { }

        var command = new CreateVoqFromRfq(
            requestId, signedInUser.Email, body?.Title, body?.Description, body?.EstimatedValue);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
