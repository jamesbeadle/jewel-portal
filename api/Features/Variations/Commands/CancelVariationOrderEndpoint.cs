using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/cancel — cancel a variation order.</summary>
public sealed class CancelVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CancelVariationOrderAuthorisation authorisation;
    private readonly CancelVariationOrderValidation validation;
    private readonly ICommandHandler<CancelVariationOrder, VariationOrder> handler;

    public CancelVariationOrderEndpoint(
        SignedInUserResolver users,
        CancelVariationOrderAuthorisation authorisation,
        CancelVariationOrderValidation validation,
        ICommandHandler<CancelVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CancelVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/cancel")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new CancelVariationOrder(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
