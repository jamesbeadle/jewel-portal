using Jewel.JPMS.Features.Labour;
using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    private bool sessionReady;
    private bool dataFailed;
    private bool refreshing;
    private string? actionError;

    private int year;
    private int month;
    private string view = "workers";
    private static readonly (string Key, string Label)[] Views =
    {
        ("workers", "By worker"), ("sites", "By site"), ("costcodes", "By cost code"), ("signoff", "Sign-off"),
        ("settlement", "Settlement"),
    };


    // The dialogs own their fields; the page opens them and shows the week's summary.
    private AbsenceModal? absenceModal;
    private SettlementLineModal? settlementLineModal;
    private WeekEntryModal? weekEntryModal;
    private IReadOnlyList<string>? weekSummaryLines;

    private bool Loading => !Labour.OverviewLoadedFor(year, month) && !dataFailed;
    private string MonthLabel => new DateTime(year, month, 1).ToString("MMMM yyyy");
    // Keys the month's panes: moving month recreates them, so an opened row or "show all" resets.
    // Each pane prefixes it — the open pane and the chase list are siblings, and Blazor refuses
    // two siblings with the same key (JPMS-683BE8).
    private string MonthKey => $"{year}-{month}";

    protected override async Task OnInitializedAsync()
    {
        var today = DateTime.Today;
        year = today.Year; month = today.Month;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Labour.OnChange += HandleChange;
        Projects.OnChanged += HandleChange;
        CostCenters.OnChanged += HandleChange;
        sessionReady = true;
        // Paint the chrome before the fetch: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed (Workers.razor has the full story).
        StateHasChanged();
        if (Session.IsApproved)
        {
            try { await Labour.RefreshOverviewAsync(year, month); }
            catch { dataFailed = true; }
            // The weekly-entry pickers' sources. Best-effort: a failure leaves the selects
            // disabled at "Loading…" and the error toast already carries the reference.
            try
            {
                await Task.WhenAll(
                    Projects.RefreshAsync(CancellationToken.None),
                    CostCenters.RefreshAsync(CancellationToken.None));
            }
            catch { /* reported by the query client; the overview itself is unaffected */ }
        }

    }

    private void HandleChange() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Labour.OnChange -= HandleChange;
        Projects.OnChanged -= HandleChange;
        CostCenters.OnChanged -= HandleChange;
    }

    private async Task MoveMonth(int delta)
    {
        var moved = new DateTime(year, month, 1).AddMonths(delta);
        year = moved.Year; month = moved.Month;
        dataFailed = false;
        if (!Labour.OverviewLoadedFor(year, month))
        {
            try { await Labour.RefreshOverviewAsync(year, month); }
            catch { dataFailed = true; }
        }
    }

    private async Task RefreshAsync()
    {
        refreshing = true; dataFailed = false; actionError = null;
        try { await Labour.RefreshOverviewAsync(year, month); }
        catch { dataFailed = true; }
        finally { refreshing = false; }
    }

    private void OpenAbsence(string workerId, string workerName, DateTimeOffset date) =>
        absenceModal!.Open(workerId, workerName, date);

    private void OpenAbsenceToday(LabourOverviewWorker worker) =>
        OpenAbsence(worker.WorkerId, worker.Name, DateTimeOffset.UtcNow);

    // Sign-off is per month part of a week (2026-09-02): the store stamps the month in view on
    // the command, so a week straddling the month end signs off THIS month's days only.
    private async Task SignOffAsync(string workerId, DateTime weekStart)
    {
        actionError = null;
        try { await Labour.SignOffWeekAsync(year, month, workerId, new DateTimeOffset(weekStart, TimeSpan.Zero)); }
        catch (Exception) { actionError = "Couldn't sign the week off — it may have unexplained days. Deal with those on the project's Labour tab first."; }
    }

    private async Task RemoveSignOffAsync(string workerId, DateTimeOffset weekStart)
    {
        actionError = null;
        try { await Labour.RemoveWeekSignOffAsync(year, month, workerId, weekStart); }
        catch (Exception) { actionError = "Couldn't remove the sign-off — try again."; }
    }

    // ---- Placement grid ----------------------------------------------------------------------

    private IEnumerable<DateTime> MonthWeekdays()
    {
        var first = new DateTime(year, month, 1);
        for (var date = first; date.Month == month; date = date.AddDays(1))
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) yield return date;
    }

    // The weeks the month's sign-off table shows: every week with a weekday in the month, plus
    // any week that touches the month only at a weekend but has time recorded on it (a Saturday
    // 1st with a timesheet needs its month part signed like any other day) — so the settlement
    // gate never waits on a part the table cannot show.
    private IEnumerable<DateTime> MonthWeeks(LabourOverviewSnapshot? snapshot)
    {
        var weekdays = MonthWeekdays().Select(ForecastRules.WeekStartOf);
        var recorded = snapshot?.Workers
            .SelectMany(worker => worker.Days)
            .Select(day => ForecastRules.WeekStartOf(day.Date.UtcDateTime.Date))
            ?? Enumerable.Empty<DateTime>();
        return weekdays.Concat(recorded).Distinct().OrderBy(date => date);
    }
}
