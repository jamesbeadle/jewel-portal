using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>The estimate's 3D model write (2026-09-16); /model is left for the client-facing page.</summary>
public sealed partial class SalesEstimateEndpoints
{
    [Function(nameof(SetEstimateHouseModel))]
    public async Task<IActionResult> HouseModel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/estimates/{estimateId}/house-model")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SetEstimateHouseModel>();
        if (posted is null) return new BadRequestResult();
        if (posted.EstimateId != estimateId) return new BadRequestObjectResult("Route estimateId does not match body.");
        var command = posted with { ChangedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!houseModelAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = houseModelValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => houseModel.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
