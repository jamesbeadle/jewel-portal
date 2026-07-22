using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

// The daily site register: who was on site, signed in/out when. Readable by all internal
// roles — the foreman checking the register is part of the sign-in honesty control (scope §8).

public sealed class ListSiteAttendanceForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>> handler;
    public ListSiteAttendanceForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListSiteAttendanceForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/labour/attendance")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListSiteAttendanceForProject(projectId), request.HttpContext.RequestAborted));
    }
}

public sealed class ListSiteAttendanceForProjectHandler : IQueryHandler<ListSiteAttendanceForProject, IReadOnlyList<SiteAttendance>>
{
    private readonly JpmsContext context;
    public ListSiteAttendanceForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SiteAttendance>> HandleAsync(ListSiteAttendanceForProject query, CancellationToken cancellationToken)
    {
        var attendance = await context.SiteAttendances
            .Where(row => row.ProjectId == query.ProjectId)
            .Join(context.Workers, row => row.WorkerId, worker => worker.WorkerId,
                (row, worker) => new { row, worker.Name })
            .OrderByDescending(joined => joined.row.WorkDate).ThenBy(joined => joined.Name)
            .ToListAsync(cancellationToken);
        return attendance.Select(joined => joined.row.ToModel(joined.Name)).ToList();
    }
}
