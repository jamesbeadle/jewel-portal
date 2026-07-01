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
/// signed-in user; the VOQ inherits the request's title/description. No request body is required.
/// </summary>
public sealed class CreateVoqFromRfqEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateVoqFromRfqAuthorisation authorisation;
    private readonly CreateVoqFromRfqValidation validation;
    private readonly ICommandHandler<CreateVoqFromRfq, VariationOrderQuote> handler;

    public CreateVoqFromRfqEndpoint(
        SignedInUserResolver users,
        CreateVoqFromRfqAuthorisation authorisation,
        CreateVoqFromRfqValidation validation,
        ICommandHandler<CreateVoqFromRfq, VariationOrderQuote> handler)
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

        var command = new CreateVoqFromRfq(requestId, signedInUser.Email);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
