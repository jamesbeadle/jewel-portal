using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-requests/{variationRequestId}/reject — rejects a subcontractor's variation
/// request with a reason the sub sees in the portal.
/// </summary>
public sealed class RejectVariationRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public RejectVariationRequestEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users; this.context = context;
    }

    [Function("RejectVariationRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-requests/{variationRequestId}/reject")] HttpRequest request,
        string variationRequestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!VariationRoles.AllowedToManageVariations.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        RejectVariationRequest? command;
        try { command = await request.ReadFromJsonAsync<RejectVariationRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (command is null) return new BadRequestResult();
        var reason = command.Reason?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(reason)) return new BadRequestObjectResult("A reason is required — the subcontractor sees it.");
        if (reason.Length > 1024) reason = reason[..1024];

        var entity = await context.SubcontractorVariationRequests
            .FirstOrDefaultAsync(row => row.VariationRequestId == variationRequestId, cancellationToken);
        if (entity is null) return new NotFoundObjectResult("Variation request not found.");
        if (entity.Status is not ((int)VariationRequestStatus.Submitted or (int)VariationRequestStatus.UnderReview))
            return new BadRequestObjectResult("Only an open variation request can be rejected.");

        entity.Status = (int)VariationRequestStatus.Rejected;
        entity.RejectionReason = reason;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ReviewedByEmail = signedInUser.Email;
        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(entity.ToModel());
    }
}
