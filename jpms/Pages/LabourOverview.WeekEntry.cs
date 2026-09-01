using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Enter a week (the accountant's weekly entry + the chat's worker_week task) ------------

    private bool weekOpen;
    private bool weekSaving;
    private string? weekError;
    private IReadOnlyList<string>? weekSummaryLines;
    private string weekWorkerId = "";
    private DateTime entryWeekStart = ForecastRules.WeekStartOf(DateTime.Today);

    private sealed class WeekDayRow
    {
        public DateTime Date;
        public string ProjectId = "";
        public decimal Hours = 8m;
        public string CostCode = "";
        public bool Locked;
        public string LockedDetail = "";
        /// <summary>A site name the assistant sent that matched no project — shown so the user
        /// picks it themselves rather than the mismatch vanishing silently.</summary>
        public string UnmatchedSite = "";
    }

    private List<WeekDayRow> weekRows = new();

    /// <summary>The server's rule (LabourRules.IsValidHours), answered in the form instead of
    /// after a round trip. The server remains the authority.</summary>
    private static string? HoursProblem(decimal hours) =>
        hours < 0.5m ? "Hours must be at least 0.5."
        : hours % 0.5m != 0m ? $"Hours must be in half-hour steps — {hours} isn't." : null;

    private async Task OpenWeekEntryAsync()
    {
        weekError = null; weekSummaryLines = null;
        weekWorkerId = "";
        entryWeekStart = ForecastRules.WeekStartOf(DateTime.Today);
        await RebuildWeekRowsAsync();
        weekOpen = true;
    }

    private void CloseWeekEntry()
    {
        weekOpen = false;
    }

    private async Task WeekWorkerChangedAsync()
    {
        await RebuildWeekRowsAsync();
    }

    private async Task MoveEntryWeekAsync(int days)
    {
        entryWeekStart = entryWeekStart.AddDays(days);
        await RebuildWeekRowsAsync();
    }

    /// <summary>Fresh rows for the current worker + week. A day the month snapshot already shows
    /// as recorded (a timesheet in any status, or an absence) renders locked — the weekly entry
    /// never overwrites; corrections live on the project's Labour tab.</summary>
    private async Task RebuildWeekRowsAsync()
    {
        var days = Enumerable.Range(0, 7).Select(offset => entryWeekStart.AddDays(offset)).ToList();
        foreach (var (weekYear, weekMonth) in days.Select(date => (date.Year, date.Month)).Distinct())
        {
            if (!Labour.OverviewLoadedFor(weekYear, weekMonth))
            {
                try { await Labour.RefreshOverviewAsync(weekYear, weekMonth); }
                catch { /* Locks just won't show; the server still skips recorded days on save. */ }
            }
        }

        weekRows = days.Select(date =>
        {
            var row = new WeekDayRow { Date = date };
            if (weekWorkerId != "" && RecordedDayFor(weekWorkerId, date) is { } recorded)
            {
                row.Locked = true;
                row.LockedDetail = recorded.Absence is not null
                    ? AbsenceLabel(recorded.Absence.Value)
                    : $"{recorded.ProjectName}, {recorded.Hours:0.#} h ({StatusLabel(recorded.Status!.Value)})";
            }
            return row;
        }).ToList();
    }

    private LabourOverviewDay? RecordedDayFor(string workerId, DateTime date) =>
        Labour.Overview(date.Year, date.Month)?.Workers
            .FirstOrDefault(worker => worker.WorkerId == workerId)?
            .Days.FirstOrDefault(day =>
                day.Date.UtcDateTime.Date == date && (day.Status is not null || day.Absence is not null));

    private void SetRowProject(WeekDayRow row, string projectId)
    {
        row.ProjectId = projectId;
        row.UnmatchedSite = "";
    }

    /// <summary>Copies the first filled day's site, hours and cost code over the other unlocked
    /// weekdays — the common case is the same site all week. Weekends stay manual.</summary>
    private void CopyFirstDayDown()
    {
        var source = weekRows.FirstOrDefault(row => !row.Locked && row.ProjectId != "");
        if (source is null) return;
        foreach (var row in weekRows.Where(row => !row.Locked && row != source
                     && row.Date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)))
        {
            row.ProjectId = source.ProjectId;
            row.Hours = source.Hours;
            row.CostCode = source.CostCode;
            row.UnmatchedSite = "";
        }
    }

    private async Task SaveWeekAsync()
    {
        var filled = weekRows.Where(row => !row.Locked && row.ProjectId != "").ToList();
        weekError =
            weekWorkerId == "" ? "Pick whose week this is."
            : filled.Count == 0 ? "Give at least one day a site."
            : filled.Select(row => HoursProblem(row.Hours)).FirstOrDefault(problem => problem is not null);
        if (weekError is not null) return;

        weekSaving = true;
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
                weekWorkerId,
                new DateTimeOffset(DateTime.SpecifyKind(entryWeekStart.Date, DateTimeKind.Unspecified), TimeSpan.Zero),
                entries);

            var lines = new List<string>
            {
                $"{result.WorkerName}, w/c {entryWeekStart:dd MMM} — "
                + $"{result.Outcomes.Count(outcome => outcome.Created)} day(s) submitted for approval"
                + (result.Outcomes.Any(outcome => !outcome.Created)
                    ? $", {result.Outcomes.Count(outcome => !outcome.Created)} skipped" : "")
            };
            lines.AddRange(result.Outcomes.Select(outcome =>
                $"· {outcome.Date.UtcDateTime:ddd dd MMM} — {outcome.Detail}"));
            weekSummaryLines = lines;

            weekOpen = false;
        }
        catch (Exception failure)
        {
            weekError = failure is CommandFailedException rejection && !string.IsNullOrWhiteSpace(rejection.Message)
                ? rejection.Message
                : "Could not save the week — check your connection and try again.";
        }
        finally { weekSaving = false; }
    }

    private string SiteNameOf(string projectId) =>
        (Projects.Current ?? Array.Empty<Project>())
            .FirstOrDefault(project => project.ProjectId == projectId)?.Name ?? "";

    /// <summary>The assistant's site names are what the crews type ("Guildford", "by france") —
    /// matched against the live project list: exact name, then a unique contains-match either
    /// way round, then the reference. No match = the row says so and the user picks.</summary>
    private string MatchProjectId(string siteName)
    {
        var list = Projects.Current ?? Array.Empty<Project>();
        var trimmed = siteName.Trim();
        if (trimmed.Length == 0) return "";

        var exact = list.FirstOrDefault(project =>
            string.Equals(project.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.ProjectId;

        var contains = list.Where(project =>
            project.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains(project.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (contains.Count == 1) return contains[0].ProjectId;

        var byReference = list.FirstOrDefault(project =>
            string.Equals(project.Reference, trimmed, StringComparison.OrdinalIgnoreCase));
        return byReference?.ProjectId ?? "";
    }

    private void CloseAbsence()
    {
        absenceOpen = false;
    }

    private void AbsenceWorkerChanged()
    {
        absenceWorkerName = Labour.Workers()
            .FirstOrDefault(worker => worker.WorkerId == absenceWorkerId)?.Name ?? "";
    }

}
