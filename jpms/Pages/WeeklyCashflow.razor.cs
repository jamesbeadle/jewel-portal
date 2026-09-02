using Jewel.JPMS.Features.WeeklyCashflow;
using static Jewel.JPMS.Features.Cashflow.CashflowDisplay;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // Session checked and the user is signed in. This is NOT "the figures are here": the header,
    // the toolbar and the Add item button show at once; the grid waits behind its one gate.
    private bool isRefreshing;
    // The plan query throws on failure (no per-row failure to record) — the flag opens the gate
    // with a message instead of pulsing forever; the toast already carries the detail.
    private bool planFailed;
    private string? moveError;

    // One "as of" per render pass keeps every week, tile and export on the same day even across
    // a midnight rollover mid-session. TodayDate re-kinds the local calendar date as UTC —
    // DateTime.Today is Kind=Local, and DateTimeOffset(local, TimeSpan.Zero) THROWS in any
    // timezone that isn't UTC (it did, on BST — JPMS-4DA261).
    private DateTimeOffset today = TodayDate();

    private static DateTimeOffset TodayDate() =>
        new(DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc), TimeSpan.Zero);

    // Which bands are collapsed to their totals row. The Xero bands start collapsed (they can
    // run to dozens of lines); manual bands start open — they are the rows the accountant owns.
    private readonly HashSet<WeeklyCashflowBand> collapsedBands = new()
    {
        WeeklyCashflowBand.ClientReceipts,
        WeeklyCashflowBand.SupplierBills
    };

    // Entries with a move in flight — their arrows disable so a double-click can't race.
    private readonly HashSet<string> movingKeys = new(StringComparer.Ordinal);

    // Supplier groups whose member bills are shown (groups start collapsed to their one line),
    // groups with a whole-cell move in flight, and entries with an exclusion change in flight.
    private readonly HashSet<string> expandedGroups = new(StringComparer.Ordinal);
    private readonly HashSet<string> movingGroupIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> excludingKeys = new(StringComparer.Ordinal);

    // The Xero seeds parked by an exclusion — filled by BuildView each pass, drawn struck-through
    // at the foot of their band instead of entering the maths.
    private readonly List<WeeklyCashflowSeed> excludedSeeds = new();

    private CashflowItemModal itemModal = default!;
    private SupplierGroupsModal groupsModal = default!;

    // ---- Sources ------------------------------------------------------------

    private XeroAgedPayablesSnapshot? PayablesSnapshot => Payables.Snapshot();

    private XeroAgedReceivablesSnapshot? ReceivablesSnapshot => Receivables.Snapshot();

    // Bank rows/tiles are directors-only, mirroring the API's gate on the Xero cash summary.
    private bool IsDirector =>
        Session.ActiveRole is { } role && DesktopNavigation.CanSee(role, DesktopNavigation.DirectorRoles);

    private XeroCashSummarySnapshot? BankSnapshot => IsDirector ? Cash.Snapshot() : null;

    private bool BankReady => BankSnapshot is { IsConfigured: true, Error: null };

    private string FetchedText =>
        BankSnapshot?.FetchedAtUtc is { } fetched ? fetched.ToLocalTime().ToString("HH:mm") : "—";

    private string XeroFetchedText =>
        PayablesSnapshot?.FetchedAtUtc is { } fetched ? fetched.ToLocalTime().ToString("HH:mm") : "—";

    // The one gate: all three sources answered (or the plan failed and says so). The bank
    // position is deliberately NOT in the gate — its tiles pulse on their own, and the grid is
    // fully useful without it.
    private bool GridReady =>
        planFailed
        || (PayablesSnapshot is not null && ReceivablesSnapshot is not null && Plan.Current is not null);

    private bool XeroReady =>
        PayablesSnapshot is { IsConfigured: true, Error: null }
        && ReceivablesSnapshot is { IsConfigured: true, Error: null }
        && Plan.Current is not null;

    // ---- Loading ------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Payables.OnChange += StateHasChanged;
        Receivables.OnChange += StateHasChanged;
        Cash.OnChange += StateHasChanged;
        Plan.OnChanged += StateHasChanged;

        // Revalidate cached data in the background on tab entry (stale-while-revalidate) — the
        // stores' fetch-once guards cover the very first load. The bank position is directors
        // only, mirroring the API's gate.
        _ = Payables.RefreshAsync();
        _ = Receivables.RefreshAsync();
        if (IsDirector) _ = Cash.RefreshAsync();

        try
        {
            await Plan.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference;
            // here we only need to stop the grid waiting on a plan that is not coming.
            planFailed = true;
        }
    }

    private async Task ForceRefreshAsync()
    {
        today = TodayDate();
        isRefreshing = true;
        try
        {
            await Task.WhenAll(
                Payables.RefreshAsync(force: true),
                Receivables.RefreshAsync(force: true),
                IsDirector ? Cash.RefreshAsync(force: true) : Task.CompletedTask,
                Plan.RefreshAsync(CancellationToken.None));
            planFailed = false;
        }
        catch
        {
            // Each store reports its own failure; whatever did land still renders.
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public void Dispose()
    {
        Payables.OnChange -= StateHasChanged;
        Receivables.OnChange -= StateHasChanged;
        Cash.OnChange -= StateHasChanged;
        Plan.OnChanged -= StateHasChanged;
    }
}
