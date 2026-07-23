using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

/// <summary>
/// POST /api/portal/my/work-orders/{workOrderId}/accept — one-click electronic acceptance of an
/// issued work order. The subcontractor id comes from the session (SubcontractorScope) and the
/// acceptance is stamped with the signed-in contact's name and email — nothing is read from the
/// body, so the client can never accept another company's order or forge who accepted.
/// Idempotent: accepting an already-accepted order returns it unchanged.
/// </summary>
public sealed class AcceptMyWorkOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public AcceptMyWorkOrderEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users; this.context = context;
    }

    [Function("AcceptMyWorkOrder")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "portal/my/work-orders/{workOrderId}/accept")] HttpRequest request,
        string workOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new StatusCodeResult(403);

        var order = await context.WorkOrders.FindAsync(new object[] { workOrderId }, cancellationToken);
        // Another company's order reads the same as a missing one — don't leak that it exists.
        if (order is null || !string.Equals(order.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new NotFoundResult();

        // Already accepted: return as-is rather than failing, so a double-click or a stale
        // second tab can't surface an error for an outcome that already holds.
        if (order.AcceptedAt is not null) return new OkObjectResult(order.ToModel());

        if (order.Status != (int)WorkOrderStatus.Released)
            return new BadRequestObjectResult("Only issued work orders can be accepted.");

        order.AcceptedAt = DateTimeOffset.UtcNow;
        order.AcceptedByEmail = signedInUser.Email;
        order.AcceptedByName = signedInUser.DisplayName;
        await context.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(order.ToModel());
    }
}
