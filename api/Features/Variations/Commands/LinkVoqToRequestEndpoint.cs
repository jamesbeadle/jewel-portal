using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/voqs/{voqId}/link-request — attach the VOQ to the request (RFI) it was raised from.
/// Body: { requestId }. Exists to repair pre-link (seeded) variation records.
/// </summary>
public sealed class LinkVoqToRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkVoqToRequestAuthorisation authorisation;
    private readonly LinkVoqToRequestValidation validation;
    private readonly ICommandHandler<LinkVoqToRequest, VariationOrderQuote> handler;

    public LinkVoqToRequestEndpoint(
        SignedInUserResolver users,
        LinkVoqToRequestAuthorisation authorisation,
        LinkVoqToRequestValidation validation,
        ICommandHandler<LinkVoqToRequest, VariationOrderQuote> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LinkVoqToRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "voqs/{voqId}/link-request")] HttpRequest request,
        string voqId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<LinkVoqToRequest>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderQuoteId = voqId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
