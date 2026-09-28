using System.Linq.Expressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/my/labour/day — the signed-in worker's own day: their project cards with
/// today's sign-in and sign-out, cost codes and note, the timesheets sent back to them, and their
/// last two weeks. The caller is a normal portal user resolved to their Worker record by email;
/// an account with no linked, active record gets an unlinked day, never an error. Hours only.</summary>
public sealed class GetMyLabourDayEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GetMyLabourDayHandler handler;
    public GetMyLabourDayEndpoint(SignedInUserResolver users, GetMyLabourDayHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetMyLabourDay))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/labour/day")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(signedInUser.Email, cancellationToken));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class GetMyLabourDayHandler : IQueryHandler<GetMyLabourDay, MyLabourDay>
{
    private const int RecentDays = 13;
    private readonly JpmsContext context;
    private readonly MyDayProjects projects;
    public GetMyLabourDayHandler(JpmsContext context, MyDayProjects projects) { this.context = context; this.projects = projects; }

    public Task<MyLabourDay> HandleAsync(GetMyLabourDay query, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("GetMyLabourDay requires the signed-in email — use the endpoint.");

    public async Task<MyLabourDay> HandleAsync(string email, CancellationToken cancellationToken)
    {
        var today = SiteClock.Today();
        var worker = await context.Workers.FirstOrDefaultAsync(candidate => candidate.ContactEmail == email, cancellationToken);
        if (worker is null || !worker.IsActive)
            return new MyLabourDay("", "", today, Array.Empty<MyLabourProject>(), Array.Empty<MyRejectedTimesheet>(), Array.Empty<MyRecentTimesheet>());

        var cards = await projects.ForAsync(worker, email, today, cancellationToken);
        var rejected = await OwnTimesheets(worker, sheet => sheet.Status == (int)TimesheetStatus.Rejected).ToListAsync(cancellationToken);
        var recentSince = today.AddDays(-RecentDays);
        var recent = await OwnTimesheets(worker, sheet => sheet.WorkedOn >= recentSince).ToListAsync(cancellationToken);
        return new MyLabourDay(
            worker.WorkerId, worker.Name, today, cards,
            rejected.Select(row => new MyRejectedTimesheet(
                row.TimesheetId, row.ProjectId, row.ProjectName, row.WorkedOn, row.Hours, row.CostCode, row.RejectionReason)).ToList(),
            recent.Select(row => new MyRecentTimesheet(
                row.TimesheetId, row.ProjectId, row.ProjectName, row.WorkedOn, row.Hours, row.CostCode, (TimesheetStatus)row.Status)).ToList());
    }

    // Sorted before the record is built: SQL Server cannot sort on a record made by its constructor.
    internal IQueryable<OwnTimesheet> OwnTimesheets(WorkerEntity worker, Expression<Func<TimesheetEntity, bool>> within) =>
        context.Timesheets
            .Where(timesheet => timesheet.WorkerId == worker.WorkerId)
            .Where(within)
            .OrderByDescending(timesheet => timesheet.WorkedOn)
            .Join(context.Projects, timesheet => timesheet.ProjectId, project => project.ProjectId,
                (timesheet, project) => new OwnTimesheet(
                    timesheet.TimesheetId, timesheet.ProjectId, project.Name, timesheet.WorkedOn,
                    timesheet.Hours, timesheet.CostCode, timesheet.Status, timesheet.RejectionReason));
}
