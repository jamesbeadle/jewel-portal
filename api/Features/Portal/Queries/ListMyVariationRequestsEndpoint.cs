using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

/// <summary>
/// GET /api/portal/my/variation-requests — the signed-in subcontractor's variation requests,
/// newest first, with project name and work-order number resolved server-side.
/// </summary>
public sealed class ListMyVariationRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public ListMyVariationRequestsEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users; this.context = context;
    }

    [Function("ListMyVariationRequests")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portal/my/variation-requests")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var subcontractorId = SubcontractorScope.OwnSubcontractorId(signedInUser);
        if (subcontractorId is null) return new StatusCodeResult(403);

        var entities = await context.SubcontractorVariationRequests
            .Where(row => row.SubcontractorId == subcontractorId)
            .OrderByDescending(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);

        var projectIds = entities.Select(row => row.ProjectId).Distinct().ToList();
        var projectNames = (await context.Projects
                .Where(project => projectIds.Contains(project.ProjectId))
                .ToListAsync(cancellationToken))
            .ToDictionary(project => project.ProjectId, project => project.Name, StringComparer.OrdinalIgnoreCase);

        var workOrderIds = entities.Select(row => row.WorkOrderId).Distinct().ToList();
        var workOrderNumbers = (await context.WorkOrders
                .Where(order => workOrderIds.Contains(order.WorkOrderId))
                .ToListAsync(cancellationToken))
            .ToDictionary(order => order.WorkOrderId, order => order.Number, StringComparer.OrdinalIgnoreCase);

        return new OkObjectResult(entities.Select(entity => entity.ToModel(
                projectNames.TryGetValue(entity.ProjectId, out var name) ? name : "",
                workOrderNumbers.TryGetValue(entity.WorkOrderId, out var number) ? number : 0))
            .ToList());
    }
}
