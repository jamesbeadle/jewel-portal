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

// The Labour tab's week grid source. All internal roles may read hours; rates and £ are
// stripped from the response unless the caller is on the commercial team (scope §6).

public sealed class ListTimesheetDetailsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ListTimesheetDetailsForProjectHandler handler;
    public ListTimesheetDetailsForProjectEndpoint(SignedInUserResolver users, ListTimesheetDetailsForProjectHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListTimesheetDetailsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/labour/timesheets")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var includeMoney = JpmsRoleSets.CommercialTeam.IncludesAny(signedInUser.Roles);
        return new OkObjectResult(await handler.HandleAsync(new ListTimesheetDetailsForProject(projectId), includeMoney, request.HttpContext.RequestAborted));
    }
}

public sealed class ListTimesheetDetailsForProjectHandler : IQueryHandler<ListTimesheetDetailsForProject, IReadOnlyList<TimesheetDetail>>
{
    private readonly JpmsContext context;
    public ListTimesheetDetailsForProjectHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<TimesheetDetail>> HandleAsync(ListTimesheetDetailsForProject query, CancellationToken cancellationToken) =>
        HandleAsync(query, includeMoney: true, cancellationToken);

    public async Task<IReadOnlyList<TimesheetDetail>> HandleAsync(ListTimesheetDetailsForProject query, bool includeMoney, CancellationToken cancellationToken)
    {
        var timesheets = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == query.ProjectId)
            .OrderByDescending(timesheet => timesheet.WorkedOn)
            .ToListAsync(cancellationToken);

        var workerIds = timesheets.Select(timesheet => timesheet.WorkerId).Where(id => id != "").Distinct().ToList();
        var namesByWorker = await context.Workers
            .Where(worker => workerIds.Contains(worker.WorkerId))
            .ToDictionaryAsync(worker => worker.WorkerId, worker => worker.Name, cancellationToken);

        return timesheets.Select(timesheet =>
        {
            var name = namesByWorker.TryGetValue(timesheet.WorkerId, out var found) ? found : timesheet.PersonEmail;
            return includeMoney ? timesheet.ToDetail(name) : timesheet.ToModelWithoutMoney(name);
        }).ToList();
    }
}
