using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.Procurement.Acceptance;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

/// <summary>
/// POST /api/portal/my/work-orders/{workOrderId}/accept — one-click electronic acceptance of an
/// issued work order. The subcontractor id comes from the session (SubcontractorScope) and the
/// acceptance is stamped with the signed-in contact's name and email — nothing is read from the
/// body, so the client can never accept another company's order or forge who accepted. The
/// stamp itself is WorkOrderAcceptance, shared with the acceptance link in the PO email;
/// idempotent: accepting an already-accepted order returns it unchanged.
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

        var isStamped = WorkOrderAcceptance.TryStamp(order, signedInUser.DisplayName, signedInUser.Email, DateTimeOffset.UtcNow);
        if (!isStamped) return new BadRequestObjectResult(WorkOrderAcceptance.OnlyIssuedOrdersRefusal);
        await context.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(order.ToModel());
    }
}
