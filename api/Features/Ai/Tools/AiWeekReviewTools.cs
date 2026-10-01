using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The week review and the week planner as reads (2026-10-01): the weeks operatives have submitted
/// from My day with their days as filed — what a director reads before sign_off_submitted_week —
/// and the planner for any week, who is in, the forecast and the actual. Each wraps the query
/// handler its endpoint composes and mirrors that endpoint's role gate exactly.
/// </summary>
internal static class AiWeekReviewTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new List<AiTool> { ListSubmittedWeeks(), ViewWeekPlan() };

    private static AiTool ListSubmittedWeeks() => new(
        "list_submitted_weeks",
        "The weeks operatives have submitted from My day for the office to sign off in one step — "
        + "the Labour overview's Weeks for review: the ones waiting first (oldest week first), then "
        + "the ones answered in the last six weeks. Each carries the worker, the week, its total hours, "
        + "where it has got to, who answered it, and every day as the worker filed it (site, hours, "
        + "trade, status, their words, late, off, planned not in). Read this before "
        + "sign_off_submitted_week or send_back_submitted_week.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        LabourRoleSets.ApproveTimesheets,
        ListSubmittedWeeksAsync);

    private static async Task<string> ListSubmittedWeeksAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var weeks = await context.Services
            .GetRequiredService<IQueryHandler<ListWorkerWeekSubmissions, IReadOnlyList<WorkerWeekSubmission>>>()
            .HandleAsync(new ListWorkerWeekSubmissions(), ct);
        return Serialise(new
        {
            ok = true,
            waiting = weeks.Count(week => week.IsAwaitingReview),
            weeks = weeks.Select(WeekShape),
            note = "Signing off approves every waiting day at the worker's rate under the budget rules "
                + "and writes the week's sign-off marker for the month-end; a day the approval door "
                + "refuses refuses the whole sign-off by name. A rejected day in a submitted week is "
                + "answered by sending the week back, never signed over."
        });
    }

    private static object WeekShape(WorkerWeekSubmission week) => new
    {
        week.WorkerName,
        weekStart = week.WeekStart,
        status = week.Status.ToString(),
        week.SubmittedAt,
        reviewedBy = NullWhenEmpty(week.ReviewedByEmail),
        week.ReviewedAt,
        reviewNote = NullWhenEmpty(week.ReviewNote),
        totalHours = week.TotalHours,
        days = week.Days.Select(DayShape)
    };

    private const string ViewWeekPlanDescription =
        "The week planner for one week: every active operative Monday to Friday, in unless the office "
        + "has recorded an absence, the planned days and forecast cost at each day rate, and the "
        + "actual cost of the days logged so far beside it. Rates ride on it, so the managing roles only.";

    private static AiTool ViewWeekPlan() => new(
        "view_week_plan",
        ViewWeekPlanDescription,
        AiToolSchema.Object(("weekStart", "string", "Any date in the week wanted, YYYY-MM-DD. Left out, this week.", false)),
        AiToolKind.Read,
        LabourRoleSets.ManageWorkers,
        ViewWeekPlanAsync);

    private static async Task<string> ViewWeekPlanAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var given = AiToolSchema.Text(input, "weekStart");
        var inWeek = SiteClock.Today();
        if (!string.IsNullOrWhiteSpace(given) && !DateTimeOffset.TryParse(given, out inWeek))
            return Fail("weekStart must be a date, YYYY-MM-DD.");
        var plan = await context.Services
            .GetRequiredService<IQueryHandler<GetLabourWeekPlan, LabourWeekPlan>>()
            .HandleAsync(new GetLabourWeekPlan(inWeek), ct);
        return Serialise(new
        {
            ok = true,
            weekStart = plan.WeekStart,
            plannedCost = plan.PlannedCost,
            actualCost = plan.ActualCost,
            workers = plan.Workers.Select(WorkerShape),
            note = "plan_worker_days marks days not in (recorded as the office's absence, which the "
                + "overview, the chase list and the sign-off gate already read) or back in. A day "
                + "already logged cannot be planned off."
        });
    }

    private static object WorkerShape(LabourPlanWorker worker) => new
    {
        worker.Name,
        worker.DayRate,
        plannedDays = worker.PlannedDays,
        plannedCost = worker.PlannedCost,
        loggedDays = worker.LoggedDays,
        actualCost = worker.ActualCost,
        days = worker.Days.Select(day => new
        {
            date = day.Date,
            isIn = day.IsIn,
            absence = day.Absence?.ToString(),
            loggedHours = day.LoggedHours,
            status = day.Status?.ToString(),
            actualCost = day.ActualCost
        })
    };

    private static object DayShape(MyWeekDay day) => new
    {
        date = day.Date,
        day.ProjectName,
        kind = day.Kind.ToString(),
        day.Hours,
        costCode = NullWhenEmpty(day.CostCode),
        status = day.Status?.ToString(),
        words = NullWhenEmpty(day.Words),
        isFiledLate = day.IsFiledLate,
        plannedAbsence = day.PlannedAbsence?.ToString()
    };

    private static string? NullWhenEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
