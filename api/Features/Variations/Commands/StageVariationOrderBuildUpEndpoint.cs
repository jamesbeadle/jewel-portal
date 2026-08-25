using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{voId}/build-up — stage the agreed build-up on a pre-approval
/// variation. Body: { lines, commercialBasis?, programmeImpact?, exclusions? }. The stager is the
/// signed-in user.
/// </summary>
public sealed class StageVariationOrderBuildUpEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly StageVariationOrderBuildUpAuthorisation authorisation;
    private readonly StageVariationOrderBuildUpValidation validation;
    private readonly ICommandHandler<StageVariationOrderBuildUp, VariationOrder> handler;

    public StageVariationOrderBuildUpEndpoint(
        SignedInUserResolver users,
        StageVariationOrderBuildUpAuthorisation authorisation,
        StageVariationOrderBuildUpValidation validation,
        ICommandHandler<StageVariationOrderBuildUp, VariationOrder> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(StageVariationOrderBuildUp))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/build-up")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<StageVariationOrderBuildUp>(cancellationToken);
        if (body is null) return new BadRequestResult();

        var command = body with { VariationOrderId = voId, StagedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (already approved, rejected) are answers to what was asked,
            // not faults — 400 so the dialog shows the reason next to the lines.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
