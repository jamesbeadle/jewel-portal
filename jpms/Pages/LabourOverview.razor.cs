using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Ai;
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

    private string? expandedWorkerId;
    private string? siteDetailProjectId;
    private bool chaseShowAll;

    // ---- Chase dismissal (2026-08-31): inline reason, audited server-side. ---------------------
    private string? dismissingKey;
    private string dismissReason = "";
    private bool chaseBusy;
    private string? chaseError;

    private static string ChaseKey(LabourChaseItem item) => $"{item.WorkerId}|{item.Date:yyyy-MM-dd}";

    private void StartDismiss(LabourChaseItem item)
    {
        dismissingKey = ChaseKey(item);
        dismissReason = "";
        chaseError = null;
    }

    private async Task ConfirmDismissAsync(LabourChaseItem item)
    {
        if (chaseBusy || string.IsNullOrWhiteSpace(dismissReason)) return;
        chaseBusy = true; chaseError = null;
        try
        {
            await Labour.DismissChaseDayAsync(item.WorkerId, item.Date, dismissReason.Trim());
            dismissingKey = null;
            dismissReason = "";
            await RefreshAsync();
        }
        catch (CommandFailedException failure) { chaseError = failure.Message; }
        catch (Exception) { chaseError = "Could not dismiss the day — try again."; }
        finally { chaseBusy = false; }
    }

    // Absence modal state.
    private bool absenceOpen;
    private bool absenceSaving;
    private string absenceWorkerId = "";
    private string absenceWorkerName = "";
    private DateTime absenceDate = DateTime.Today;
    private DateTime absenceEndDate = DateTime.Today;
    private AbsenceKind absenceKind = AbsenceKind.Holiday;
    private string absenceNote = "";

    // Contract editing (inside the worker detail row). The CIS select binds a STRING: the rate
    // arrives from the API as a decimal whose scale follows the stored value ("20.00"), and a
    // <select @bind> matches its options by string — bound to the decimal, "20.00" matched no
    // option, the select rendered blank, and picking "20" then compared numerically equal to the
    // current value, so Save sent nothing. loaded* keep what the panel opened with so Save knows
    // what genuinely changed.
    private decimal editContractedDays;
    private string editCisRate = "20";
    private decimal loadedContractedDays;
    private decimal loadedCisRate;
    private bool planSaving;
    private bool planSaved;
    private string? planError;

    private bool Loading => !Labour.OverviewLoadedFor(year, month) && !dataFailed;
    private string MonthLabel => new DateTime(year, month, 1).ToString("MMMM yyyy");

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
        expandedWorkerId = null; siteDetailProjectId = null; chaseShowAll = false;
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

    private void ToggleWorker(string workerId)
    {
        if (expandedWorkerId == workerId) { expandedWorkerId = null; return; }
        expandedWorkerId = workerId;
        var worker = Labour.Overview(year, month)?.Workers.FirstOrDefault(row => row.WorkerId == workerId);
        loadedContractedDays = worker?.ContractedDays ?? 0m;
        loadedCisRate = worker?.CisRatePercent ?? 20m;
        editContractedDays = loadedContractedDays;
        editCisRate = loadedCisRate.ToString("0");
        planSaved = false; planError = null;
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

    private async Task SavePlanAsync(LabourOverviewWorker worker)
    {
        planSaving = true; planSaved = false; planError = null; actionError = null;
        try
        {
            var cisRate = decimal.TryParse(editCisRate, out var parsed) ? parsed : loadedCisRate;
            var changed = false;
            if (editContractedDays != loadedContractedDays)
            {
                await Labour.SetWorkerContractAsync(year, month, worker.WorkerId, editContractedDays);
                changed = true;
            }
            if (cisRate != loadedCisRate)
            {
                await Labour.SetWorkerCisStatusAsync(year, month, worker.WorkerId, cisRate, "");
                changed = true;
            }
            // Show the saved values back from the server — without this, a successful save
            // changed nothing on screen and read as "it does nothing".
            if (changed) await Labour.RefreshOverviewAsync(year, month);
            loadedContractedDays = editContractedDays;
            loadedCisRate = cisRate;
            planSaved = true;
        }
        catch (CommandFailedException failure) { planError = failure.Message; }
        catch (Exception) { planError = "Could not save the contract settings — try again."; }
        finally { planSaving = false; }
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


    private static string CodingOutcomeLabel(XeroCodingOutcome outcome) => outcome switch
    {
        XeroCodingOutcome.BillRecoded => "bill recoded",
        XeroCodingOutcome.DraftStaged => "draft staged",
        XeroCodingOutcome.Skipped => "skipped",
        _ => "failed",
    };

}
