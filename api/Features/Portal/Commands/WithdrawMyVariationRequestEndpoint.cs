using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

/// <summary>
/// POST /api/portal/my/variation-requests/{variationRequestId}/withdraw — the subcontractor pulls
/// back their own request while it is still open (Submitted/UnderReview).
/// </summary>
public sealed class WithdrawMyVariationRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public WithdrawMyVariationRequestEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users; this.context = context;
    }

    [Function("WithdrawMyVariationRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portal/my/variation-requests/{variationRequestId}/withdraw")] HttpRequest request,
        string variationRequestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new ForbidResult();

        var entity = await context.SubcontractorVariationRequests
            .FirstOrDefaultAsync(row => row.VariationRequestId == variationRequestId, cancellationToken);
        if (entity is null || !string.Equals(entity.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new NotFoundObjectResult("Variation request not found."); // Don't reveal other companies' ids.

        if (entity.Status is not ((int)VariationRequestStatus.Submitted or (int)VariationRequestStatus.UnderReview))
            return new BadRequestObjectResult("Only an open request can be withdrawn.");

        entity.Status = (int)VariationRequestStatus.Withdrawn;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(entity.ToModel());
    }
}
