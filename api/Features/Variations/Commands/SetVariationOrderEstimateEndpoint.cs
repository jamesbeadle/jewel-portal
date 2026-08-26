using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/estimate — re-state a pre-approval variation's estimate.
/// Body: { estimatedValue } (null or 0 = currently unpriced). See SetVariationOrderEstimate.
/// </summary>
public sealed class SetVariationOrderEstimateEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetVariationOrderEstimateAuthorisation authorisation;
    private readonly SetVariationOrderEstimateValidation validation;
    private readonly ICommandHandler<SetVariationOrderEstimate, VariationOrder> handler;

    public SetVariationOrderEstimateEndpoint(
        SignedInUserResolver users,
        SetVariationOrderEstimateAuthorisation authorisation,
        SetVariationOrderEstimateValidation validation,
        ICommandHandler<SetVariationOrderEstimate, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetVariationOrderEstimate))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/estimate")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetVariationOrderEstimate>();
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
