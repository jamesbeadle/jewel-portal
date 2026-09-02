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


    // Absence modal state.
    private bool absenceOpen;
    private bool absenceSaving;
    private string absenceWorkerId = "";
    private string absenceWorkerName = "";
    private DateTime absenceDate = DateTime.Today;
    private DateTime absenceEndDate = DateTime.Today;
    private AbsenceKind absenceKind = AbsenceKind.Holiday;
    private string absenceNote = "";

    private bool Loading => !Labour.OverviewLoadedFor(year, month) && !dataFailed;
    private string MonthLabel => new DateTime(year, month, 1).ToString("MMMM yyyy");
    // Keys the month's panes: moving month recreates them, so an opened row or "show all" resets.
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

    private void OpenAbsence(string workerId, string workerName, DateTimeOffset date)
    {
        absenceWorkerId = workerId; absenceWorkerName = workerName;
        absenceDate = date.UtcDateTime.Date; absenceEndDate = absenceDate;
        absenceKind = AbsenceKind.Holiday; absenceNote = "";
        absenceOpen = true;
    }

    private async Task SaveAbsenceAsync()
    {
        absenceSaving = true; actionError = null;
        try
        {
            // One day or a range — the store records each weekday and refreshes once. Partial
            // failure keeps the modal open with the dates that did not land, so a retry only
            // re-attempts what is actually missing.
            var failedDates = await Labour.RecordAbsenceRangeAsync(year, month, absenceWorkerId,
                absenceDate, absenceEndDate, absenceKind, absenceNote);
            if (failedDates.Count == 0) { absenceOpen = false; }
            else
            {
                actionError = "Could not record "
                    + string.Join(", ", failedDates.Select(date => date.ToString("ddd dd MMM")))
                    + " — those days may already have an absence. The rest were recorded.";
            }
        }
        catch (Exception) { actionError = "Could not record the absence — try again."; }
        finally { absenceSaving = false; }
    }

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

    private IEnumerable<DateTime> MonthWeeks() =>
        MonthWeekdays().Select(ForecastRules.WeekStartOf).Distinct().OrderBy(date => date);
}
