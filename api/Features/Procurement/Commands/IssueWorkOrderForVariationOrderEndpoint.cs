using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// POST /api/variation-orders/{variationOrderId}/work-order — issues the new work order that
/// instructs an approved variation order. Same roles as awarding a bid package (it creates the
/// same kind of commitment).
/// </summary>
public sealed class IssueWorkOrderForVariationOrderEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<IssueWorkOrderForVariationOrder, WorkOrder> handler;

    public IssueWorkOrderForVariationOrderEndpoint(
        SignedInUserResolver users, ICommandHandler<IssueWorkOrderForVariationOrder, WorkOrder> handler)
    {
        this.users = users; this.handler = handler;
    }

    // Mirrors AwardBidPackageAuthorisation — issuing a PO is a Director/PM act.
    private static readonly RoleSet RolesThatMayIssue =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    [Function("IssueWorkOrderForVariationOrder")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{variationOrderId}/work-order")] HttpRequest request,
        string variationOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayIssue.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        try
        {
            var workOrder = await handler.HandleAsync(
                new IssueWorkOrderForVariationOrder(variationOrderId, signedInUser.Email), cancellationToken);
            return new OkObjectResult(workOrder);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
