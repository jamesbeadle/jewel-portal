using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/labour/plan/{weekStart} — the week planner: every active operative, Monday to
/// Friday, in unless the office has recorded an absence, with the forecast at each day rate and
/// the actuals of the days logged so far. Rates ride on it, so the managing roles only.</summary>
public sealed class GetLabourWeekPlanEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GetLabourWeekPlanHandler handler;
    public GetLabourWeekPlanEndpoint(SignedInUserResolver users, GetLabourWeekPlanHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetLabourWeekPlan))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/plan/{weekStart}")] HttpRequest request, string weekStart)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!DateTimeOffset.TryParse(weekStart, out var inWeek)) return new BadRequestObjectResult(new[] { "Name the week by a date in it." });
        return new OkObjectResult(await handler.HandleAsync(new GetLabourWeekPlan(inWeek), cancellationToken));
    }
}

public sealed class GetLabourWeekPlanHandler : IQueryHandler<GetLabourWeekPlan, LabourWeekPlan>
{
    private readonly JpmsContext context;
    public GetLabourWeekPlanHandler(JpmsContext context) { this.context = context; }

    public async Task<LabourWeekPlan> HandleAsync(GetLabourWeekPlan query, CancellationToken cancellationToken)
    {
        var monday = LabourWeeks.MondayOf(query.WeekStart);
        var friday = LabourWeeks.FridayOf(query.WeekStart);
        var workers = await context.Workers.AsNoTracking()
            .Where(worker => worker.IsActive).OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        var absences = await context.WorkerAbsences.AsNoTracking()
            .Where(row => row.Date >= monday && row.Date <= friday).ToListAsync(cancellationToken);
        var timesheets = await context.Timesheets.AsNoTracking()
            .Where(row => row.WorkedOn >= monday && row.WorkedOn <= friday && row.WorkerId != "").ToListAsync(cancellationToken);
        var rows = workers.Select(worker => PlanFor(worker, monday, absences, timesheets)).ToList();
        return new LabourWeekPlan(monday, rows);
    }

    private static LabourPlanWorker PlanFor(
        WorkerEntity worker, DateTimeOffset monday, List<WorkerAbsenceEntity> absences, List<TimesheetEntity> timesheets)
    {
        var dayRate = worker.HourlyRate * ForecastRules.StandardHoursPerDay;
        var days = LabourWeeks.WorkingDaysOf(monday).Select(day => DayFor(worker, day, absences, timesheets)).ToList();
        return new LabourPlanWorker(worker.WorkerId, worker.Name, dayRate, days);
    }

    private static LabourPlanDay DayFor(
        WorkerEntity worker, DateTimeOffset day, List<WorkerAbsenceEntity> absences, List<TimesheetEntity> timesheets)
    {
        var absence = absences.FirstOrDefault(row => row.WorkerId == worker.WorkerId && row.Date == day);
        var logged = timesheets.Where(row => row.WorkerId == worker.WorkerId && row.WorkedOn == day).ToList();
        var hours = logged.Sum(row => row.Hours);
        var status = logged.Count == 0 ? (TimesheetStatus?)null : StatusOf(logged);
        var actual = logged.Sum(row => LabourActuals.CostOf(row, worker.HourlyRate));
        return new LabourPlanDay(day, absence is null ? null : (AbsenceKind)absence.Kind, hours, status, actual);
    }

    private static TimesheetStatus StatusOf(List<TimesheetEntity> logged) =>
        logged.All(row => row.Status == (int)TimesheetStatus.Approved) ? TimesheetStatus.Approved
            : logged.Any(row => row.Status == (int)TimesheetStatus.Rejected) ? TimesheetStatus.Rejected
            : TimesheetStatus.Submitted;
}
