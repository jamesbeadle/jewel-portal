using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-requests/{variationRequestId}/accept — accepts a subcontractor's variation
/// request, creating a Selected VOQ carrying their price. Gated like other variation management.
/// </summary>
public sealed class AcceptVariationRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<AcceptVariationRequest, VariationOrder> handler;

    public AcceptVariationRequestEndpoint(
        SignedInUserResolver users, ICommandHandler<AcceptVariationRequest, VariationOrder> handler)
    {
        this.users = users; this.handler = handler;
    }

    [Function("AcceptVariationRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-requests/{variationRequestId}/accept")] HttpRequest request,
        string variationRequestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!VariationRoles.AllowedToManageVariations.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        try
        {
            var voq = await handler.HandleAsync(
                new AcceptVariationRequest(variationRequestId, signedInUser.Email), cancellationToken);
            return new OkObjectResult(voq);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
