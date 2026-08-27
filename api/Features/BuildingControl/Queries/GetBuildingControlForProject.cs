using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.BuildingControl.Queries;

/// <summary>Everything the Building Control tab renders, in one answer — cases newest-first
/// (the active one leads), inspections in running order, and every file. The inspection detail
/// page slices the same answer client-side, so the tab needs exactly one fetch.</summary>
public sealed class GetBuildingControlForProjectHandler
    : IQueryHandler<GetBuildingControlForProject, BuildingControlProjectView>
{
    private readonly JpmsContext context;
    public GetBuildingControlForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlProjectView> HandleAsync(
        GetBuildingControlForProject query, CancellationToken cancellationToken)
    {
        var cases = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.Number)
            .ToListAsync(cancellationToken);
        var inspections = await context.BuildingControlInspections.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderBy(row => row.DisplayOrder)
            .ThenBy(row => row.Number)
            .ToListAsync(cancellationToken);
        var attachments = await context.BuildingControlAttachments.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);

        return new BuildingControlProjectView(
            cases.Select(row => row.ToModel()).ToList(),
            inspections.Select(row => row.ToModel()).ToList(),
            attachments.Select(row => row.ToModel()).ToList());
    }
}

public sealed class GetBuildingControlForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBuildingControlForProject, BuildingControlProjectView> handler;

    public GetBuildingControlForProjectEndpoint(
        SignedInUserResolver users, IQueryHandler<GetBuildingControlForProject, BuildingControlProjectView> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetBuildingControlForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/building-control")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!BuildingControlRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new GetBuildingControlForProject(projectId), cancellationToken));
    }
}
