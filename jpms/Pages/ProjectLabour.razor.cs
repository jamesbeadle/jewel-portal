using Jewel.JPMS.Features.CostCenters;

namespace Jewel.JPMS.Pages;

public partial class ProjectLabour
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Session checked and the user is signed in. This is NOT "the data is here" — the tab's
    // headings and its week nav show at once; each panel below holds until its own figures land.

    // A failed fetch must open the gates, or the jewels pulse forever. The panel says so instead
    // of showing rows; the toast at the top carries the reference and the detail.
    private bool dataFailed;

    // -- Panel gates. Each lists every source the panel reads, so it appears in one piece. --
    private bool TimesheetsReady => Labour.TimesheetsLoadedFor(ProjectId) && CostCenters.IsLoaded;
    private bool ManualEntryReady => Labour.AssignmentsLoadedFor(ProjectId) && CostCenters.IsLoaded;
    private bool AssignmentsReady => Labour.AssignmentsLoadedFor(ProjectId) && Labour.WorkersLoaded;
    private bool RegisterReady => Labour.AttendanceLoadedFor(ProjectId);
    private bool SettlementReady => Labour.SettlementLoadedFor(ProjectId);

    private string? actionError;
    private DateTimeOffset weekStart = StartOfWeek(DateTimeOffset.Now);

    private readonly HashSet<string> selectedIds = new();
    private IReadOnlyList<LabourApprovalFailure> approvalFailures = Array.Empty<LabourApprovalFailure>();

    // -- Over-budget override (2026-08-29, the accountant's ask) -----------------------------
    // Mirrors the API's LabourRoleSets.OverrideBudgetBlock (MD/FD/Admin) the way the audit trail
    // page mirrors its gate — the server re-checks; this only decides whether to offer the button.
    private bool CanOverrideBudget => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector);
    private bool overBudgetOpen;
    private bool isOverriding;
    private string overBudgetReason = "";
    private string? overBudgetError;
    private IReadOnlyList<LabourApprovalFailure> overBudgetFailures = Array.Empty<LabourApprovalFailure>();
    private bool isApproving;

    private string? editingId;
    private decimal editHours;
    private string editCostCode = "";

    // Cost codes as SearchSelect options — the same type-to-find picker as the allocation
    // table, so a fifty-line list is found, not scrolled. The label carries code + name so
    // typing matches either ("kit" finds CARP-KIT Carpentry kitchens; "ELE" finds every
    // electrical centre). Rebuilt only when the read model republishes (Alphabetical is
    // memoised per fetch), so renders don't churn allocations.
    private IReadOnlyList<SearchSelect.Option>? costCodeOptionsCache;
    private object? costCodeOptionsCacheKey;
    private IReadOnlyList<SearchSelect.Option> CostCodeOptions
    {
        get
        {
            var centres = CostCenters.Alphabetical;
            if (costCodeOptionsCache is null || !ReferenceEquals(costCodeOptionsCacheKey, centres))
            {
                costCodeOptionsCache = centres
                    .Where(centre => centre.IsActive)
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}"))
                    .ToList();
                costCodeOptionsCacheKey = centres;
            }
            return costCodeOptionsCache;
        }
    }

    private string? rejectingId;
    private string rejectReason = "";

    private bool addDayOpen;
    private string manualWorkerId = "";
    private DateTime manualDate = DateTime.Today;
    private string manualCostCode = "";
    private decimal manualHours;
    private bool isAddingManual;

    // Failures belonging to a particular form render next to that form. actionError stays for
    // page-level failures (approval, assignment, the ledger) and renders at the top of the tab.
    private string? manualError;
    private string? rejectError;

    // -- Failure reporting ------------------------------------------------------
    // CommandFailedException carries the endpoint's own words. Show those. `fallback` covers only
    // the case where nothing came back to show — a transport failure with no response at all.
    private static string DescribeFailure(Exception failure, string fallback) =>
        failure is CommandFailedException rejection && !string.IsNullOrWhiteSpace(rejection.Message)
            ? rejection.Message
            : fallback;

    private void ReportFailure(Exception failure, string fallback) =>
        actionError = DescribeFailure(failure, fallback);

    /// <summary>
    /// The server's rule (LabourRules.IsValidHours), checked here so a typo is answered in the form
    /// instead of after a round trip. It must stay in step with the server, which remains the
    /// authority — this is the courtesy, not the control.
    /// </summary>
    private static string? HoursProblem(decimal hours) =>
        hours < 0.5m ? "Hours must be at least 0.5."
        : hours % 0.5m != 0m ? $"Hours must be in half-hour steps — {hours} isn't. Try {Math.Round(hours * 2m, MidpointRounding.AwayFromZero) / 2m}."
        : null;

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Labour.OnChange += StateHasChanged;
        CostCenters.OnChanged += StateHasChanged;

        // Revalidate cached data in the background on every visit (stale-while-revalidate —
        // see the front-end data-loading convention in CLAUDE.md).
        try
        {
            await Task.WhenAll(
                Labour.RefreshTimesheetsAsync(ProjectId),
                Labour.RefreshAttendanceAsync(ProjectId),
                Labour.RefreshAssignmentsAsync(ProjectId),
                Labour.RefreshSettlementAsync(ProjectId),
                Labour.RefreshWorkersAsync(),
                CostCenters.RefreshAsync(CancellationToken.None));
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference; here
            // we only need to stop the panels waiting on data that is not coming.
            dataFailed = true;
        }
    }

    public void Dispose()
    {
        Labour.OnChange -= StateHasChanged;
        CostCenters.OnChanged -= StateHasChanged;
    }

    private static DateTimeOffset StartOfWeek(DateTimeOffset moment)
    {
        var daysSinceMonday = ((int)moment.DayOfWeek + 6) % 7;
        return new DateTimeOffset(moment.Date.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }

    private void MoveWeek(int days)
    {
        weekStart = weekStart.AddDays(days);
        // A new week is a new view — never let last week's ticked rows ride invisibly into
        // this week's Approve selected.
        selectedIds.Clear();
    }

}
