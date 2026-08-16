using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>POST /api/variation-orders/{voId}/reject — reject a variation order (reverses the
/// approval's commercial writes when it was approved).</summary>
public sealed class RejectVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RejectVariationOrderAuthorisation authorisation;
    private readonly RejectVariationOrderValidation validation;
    private readonly ICommandHandler<RejectVariationOrder, VariationOrder> handler;

    public RejectVariationOrderEndpoint(
        SignedInUserResolver users,
        RejectVariationOrderAuthorisation authorisation,
        RejectVariationOrderValidation validation,
        ICommandHandler<RejectVariationOrder, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RejectVariationOrder))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/reject")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RejectVariationOrder(voId);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
