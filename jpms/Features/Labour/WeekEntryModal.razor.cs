using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Features.Labour;

public partial class WeekEntryModal
{
    [Inject] private ILabourStore Labour { get; set; } = default!;
    [Inject] private ProjectListReadModel Projects { get; set; } = default!;
    [Inject] private CostCentersReadModel CostCenters { get; set; } = default!;

    /// <summary>The week landed: one summary line, then a line per day — the page shows them.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> OnSaved { get; set; }

    private bool open;
    private bool saving;
    private string? error;
    private string workerId = "";
    private DateTime weekStart = ForecastRules.WeekStartOf(DateTime.Today);

    private sealed class DayRow
    {
        public DateTime Date;
        public string ProjectId = "";
        public decimal Hours = 8m;
        public string CostCode = "";
        public bool Locked;
        public string LockedDetail = "";
    }

    private List<DayRow> rows = new();

    /// <summary>The server's rule (LabourRules.IsValidHours), answered in the form instead of
    /// after a round trip. The server remains the authority.</summary>
    private static string? HoursProblem(decimal hours) =>
        hours < 0.5m ? "Hours must be at least 0.5."
        : hours % 0.5m != 0m ? $"Hours must be in half-hour steps — {hours} isn't." : null;

    public async Task OpenAsync()
    {
        error = null;
        workerId = "";
        weekStart = ForecastRules.WeekStartOf(DateTime.Today);
        await RebuildRowsAsync();
        open = true;
        StateHasChanged();
    }

    private async Task MoveWeekAsync(int days)
    {
        weekStart = weekStart.AddDays(days);
        await RebuildRowsAsync();
    }

    /// <summary>Fresh rows for the current worker + week. A day the month snapshot already shows
    /// as recorded (a timesheet in any status, or an absence) renders locked — the weekly entry
    /// never overwrites; corrections live on the project's Labour tab.</summary>
    private async Task RebuildRowsAsync()
    {
        var days = Enumerable.Range(0, 7).Select(offset => weekStart.AddDays(offset)).ToList();
        foreach (var (weekYear, weekMonth) in days.Select(date => (date.Year, date.Month)).Distinct())
        {
            if (!Labour.OverviewLoadedFor(weekYear, weekMonth))
            {
                try { await Labour.RefreshOverviewAsync(weekYear, weekMonth); }
                catch { /* Locks just won't show; the server still skips recorded days on save. */ }
            }
        }

        rows = days.Select(date =>
        {
            var row = new DayRow { Date = date };
            if (workerId != "" && RecordedDayFor(workerId, date) is { } recorded)
            {
                row.Locked = true;
                row.LockedDetail = recorded.Absence is not null
                    ? AbsenceLabel(recorded.Absence.Value)
                    : $"{recorded.ProjectName}, {recorded.Hours:0.#} h ({StatusLabel(recorded.Status!.Value)})";
            }
            return row;
        }).ToList();
    }

    private LabourOverviewDay? RecordedDayFor(string forWorkerId, DateTime date) =>
        Labour.Overview(date.Year, date.Month)?.Workers
            .FirstOrDefault(worker => worker.WorkerId == forWorkerId)?
            .Days.FirstOrDefault(day =>
                day.Date.UtcDateTime.Date == date && (day.Status is not null || day.Absence is not null));

    /// <summary>Copies the first filled day's site, hours and cost code over the other unlocked
    /// weekdays — the common case is the same site all week. Weekends stay manual.</summary>
    private void CopyFirstDayDown()
    {
        var source = rows.FirstOrDefault(row => !row.Locked && row.ProjectId != "");
        if (source is null) return;
        foreach (var row in rows.Where(row => !row.Locked && row != source
                     && row.Date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)))
        {
            row.ProjectId = source.ProjectId;
            row.Hours = source.Hours;
            row.CostCode = source.CostCode;
        }
    }

    private async Task SaveAsync()
    {
        var filled = rows.Where(row => !row.Locked && row.ProjectId != "").ToList();
        error =
            workerId == "" ? "Pick whose week this is."
            : filled.Count == 0 ? "Give at least one day a site."
            : filled.Select(row => HoursProblem(row.Hours)).FirstOrDefault(problem => problem is not null);
        if (error is not null) return;

        saving = true;
        try
        {
            // SpecifyKind before the offset: DateTime.Today is Kind.Local, and a Local date with
            // a zero offset throws outside GMT (the AddManualAsync lesson on the Labour tab).
            var entries = filled
                .Select(row => new WorkerWeekDayEntry(
                    new DateTimeOffset(DateTime.SpecifyKind(row.Date.Date, DateTimeKind.Unspecified), TimeSpan.Zero),
                    row.ProjectId, row.Hours, row.CostCode))
                .ToList();
            var result = await Labour.SubmitWorkerWeekAsync(
                workerId,
                new DateTimeOffset(DateTime.SpecifyKind(weekStart.Date, DateTimeKind.Unspecified), TimeSpan.Zero),
                entries);

            var lines = new List<string>
            {
                $"{result.WorkerName}, w/c {weekStart:dd MMM} — "
                + $"{result.Outcomes.Count(outcome => outcome.Created)} day(s) submitted for approval"
                + (result.Outcomes.Any(outcome => !outcome.Created)
                    ? $", {result.Outcomes.Count(outcome => !outcome.Created)} skipped" : "")
            };
            lines.AddRange(result.Outcomes.Select(outcome =>
                $"· {outcome.Date.UtcDateTime:ddd dd MMM} — {outcome.Detail}"));

            open = false;
            await OnSaved.InvokeAsync(lines);
        }
        catch (Exception failure)
        {
            error = failure is CommandFailedException rejection && !string.IsNullOrWhiteSpace(rejection.Message)
                ? rejection.Message
                : "Could not save the week — check your connection and try again.";
        }
        finally { saving = false; }
    }
}
