using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

// The estimate's edit and its status move — the writes after it exists.
public sealed partial class SalesEstimateEndpoints
{
    [Function(nameof(UpdateEstimateDetails))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/estimates/{estimateId}")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateEstimateDetails>();
        if (command is null) return new BadRequestResult();
        if (command.EstimateId != estimateId) return new BadRequestObjectResult("Route estimateId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!updateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = updateValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => update.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(SetEstimateBreakdown))]
    public async Task<IActionResult> Breakdown(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/estimates/{estimateId}/breakdown")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SetEstimateBreakdown>();
        if (posted is null) return new BadRequestResult();
        if (posted.EstimateId != estimateId) return new BadRequestObjectResult("Route estimateId does not match body.");
        var command = posted with { ChangedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!breakdownAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = breakdownValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => breakdown.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(MoveEstimateStatus))]
    public async Task<IActionResult> Move(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/estimates/{estimateId}/status")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<MoveEstimateStatus>();
        if (posted is null) return new BadRequestResult();
        if (posted.EstimateId != estimateId) return new BadRequestObjectResult("Route estimateId does not match body.");
        var command = posted with { ChangedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!moveAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = moveValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => move.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
