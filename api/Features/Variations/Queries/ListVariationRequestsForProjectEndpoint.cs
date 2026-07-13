using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

/// <summary>
/// GET /api/projects/{projectId}/variation-requests — the subcontractor-raised variation requests
/// for a project, newest first, for the internal review queue on the Variations tab.
/// </summary>
public sealed class ListVariationRequestsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;

    public ListVariationRequestsForProjectEndpoint(SignedInUserResolver users, JpmsContext context)
    {
        this.users = users; this.context = context;
    }

    // Same read gate as the other variation queries: internal roles plus the architect.
    private static readonly RoleSet RolesThatMayReadVariations = JpmsRoleSets.InternalAndArchitect;

    [Function("ListVariationRequestsForProject")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/variation-requests")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadVariations.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var entities = await context.SubcontractorVariationRequests
            .Where(row => row.ProjectId == projectId)
            .OrderByDescending(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);

        var subcontractorIds = entities.Select(row => row.SubcontractorId).Distinct().ToList();
        var subcontractorNames = (await context.Subcontractors
                .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
                .ToListAsync(cancellationToken))
            .ToDictionary(sub => sub.SubcontractorId, sub => sub.CompanyName, StringComparer.OrdinalIgnoreCase);

        var workOrderIds = entities.Select(row => row.WorkOrderId).Distinct().ToList();
        var workOrderNumbers = (await context.WorkOrders
                .Where(order => workOrderIds.Contains(order.WorkOrderId))
                .ToListAsync(cancellationToken))
            .ToDictionary(order => order.WorkOrderId, order => order.Number, StringComparer.OrdinalIgnoreCase);

        return new OkObjectResult(entities.Select(entity => entity.ToModel(
                projectName: "",
                workOrderNumbers.TryGetValue(entity.WorkOrderId, out var number) ? number : 0,
                subcontractorNames.TryGetValue(entity.SubcontractorId, out var name) ? name : "(unknown)"))
            .ToList());
    }
}
