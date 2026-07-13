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

// The signed-in worker's own day: their assigned projects with today's sign-in/out state and
// allocation cost codes, plus rejected timesheets awaiting correction. The caller is a normal
// portal user; their Worker record is resolved by email. Hours only — no rates, no £.

public sealed class GetMyLabourDayEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GetMyLabourDayHandler handler;
    public GetMyLabourDayEndpoint(SignedInUserResolver users, GetMyLabourDayHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetMyLabourDay))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/labour/day")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class GetMyLabourDayHandler : IQueryHandler<GetMyLabourDay, MyLabourDay>
{
    private readonly JpmsContext context;
    public GetMyLabourDayHandler(JpmsContext context) { this.context = context; }

    public Task<MyLabourDay> HandleAsync(GetMyLabourDay query, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("GetMyLabourDay requires the signed-in email — use the endpoint.");

    public async Task<MyLabourDay> HandleAsync(string email, CancellationToken cancellationToken)
    {
        // No worker record for this email is an expected state (admins, staff browsing the
        // page, a worker not yet linked) — return an unlinked day rather than an error; the
        // page explains what to do. Write actions still require a linked, active record.
        var unlinked = await context.Workers.FirstOrDefaultAsync(
            candidate => candidate.ContactEmail == email, cancellationToken);
        if (unlinked is null || !unlinked.IsActive)
            return new MyLabourDay("", "", SiteClock.Today(),
                Array.Empty<MyLabourProject>(), Array.Empty<MyRejectedTimesheet>());

        var worker = unlinked;
        var today = SiteClock.Today();

        var assignments = await context.ProjectWorkerAssignments
            .Where(assignment => assignment.WorkerId == worker.WorkerId && assignment.IsActive)
            .Join(context.Projects, assignment => assignment.ProjectId, project => project.ProjectId,
                (assignment, project) => new { project.ProjectId, project.Name })
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);
        var projectIds = assignments.Select(project => project.ProjectId).ToList();

        var attendanceToday = await context.SiteAttendances
            .Where(attendance => attendance.WorkerId == worker.WorkerId
                                 && attendance.WorkDate == today
                                 && projectIds.Contains(attendance.ProjectId))
            .ToListAsync(cancellationToken);

        // Allocation list per project: budgeted cost codes; whole active master list if nothing
        // is budgeted yet, so the page never dead-ends.
        var budgets = await context.CostCodeBudgets
            .Where(budget => projectIds.Contains(budget.ProjectId))
            .Select(budget => new { budget.ProjectId, budget.CostCode })
            .ToListAsync(cancellationToken);
        var centres = await context.CostCenters
            .Where(centre => centre.IsActive)
            .OrderBy(centre => centre.SortOrder)
            .Select(centre => new SiteSheetCostCode(centre.Code, centre.Name))
            .ToListAsync(cancellationToken);

        var projects = assignments.Select(project =>
        {
            var attendance = attendanceToday.FirstOrDefault(row => row.ProjectId == project.ProjectId);
            var budgeted = budgets.Where(budget => budget.ProjectId == project.ProjectId)
                .Select(budget => budget.CostCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var codes = budgeted.Count > 0
                ? centres.Where(centre => budgeted.Contains(centre.Code)).ToList()
                : centres;
            return new MyLabourProject(project.ProjectId, project.Name,
                attendance is not null, attendance?.SignedOutAt is not null, codes);
        }).ToList();

        var rejected = await context.Timesheets
            .Where(timesheet => timesheet.WorkerId == worker.WorkerId
                                && timesheet.Status == (int)TimesheetStatus.Rejected)
            .Join(context.Projects, timesheet => timesheet.ProjectId, project => project.ProjectId,
                (timesheet, project) => new { timesheet, project.Name })
            .OrderByDescending(row => row.timesheet.WorkedOn)
            .ToListAsync(cancellationToken);

        return new MyLabourDay(
            worker.WorkerId, worker.Name, today, projects,
            rejected.Select(row => new MyRejectedTimesheet(
                row.timesheet.TimesheetId, row.timesheet.ProjectId, row.Name, row.timesheet.WorkedOn,
                row.timesheet.Hours, row.timesheet.CostCode, row.timesheet.RejectionReason)).ToList());
    }
}
