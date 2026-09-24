using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Features.Owner;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class OwnerOverview
{
    /// <summary>The Profit Summary's ceiling on concurrent per-project loads.</summary>
    private const int ProjectRefreshConcurrency = 4;

    private readonly SemaphoreSlim throttle = new(ProjectRefreshConcurrency);

    private readonly HashSet<string> loadedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> failedProjects = new(StringComparer.OrdinalIgnoreCase);

    private bool projectsFailed;
    private bool planFailed;
    private bool leadsFailed;

    private readonly DateTimeOffset today = new(DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc), TimeSpan.Zero);

    private XeroCashSummarySnapshot? BankSnapshot => Cash.Snapshot();
    private bool BankReady => BankSnapshot is { IsConfigured: true, Error: null };

    private XeroAgedPayablesSnapshot? PayablesSnapshot => Payables.Snapshot();
    private XeroAgedReceivablesSnapshot? ReceivablesSnapshot => Receivables.Snapshot();

    private bool XeroReady =>
        PayablesSnapshot is { IsConfigured: true, Error: null }
        && ReceivablesSnapshot is { IsConfigured: true, Error: null };

    private bool WeeklyReady => !planFailed && XeroReady && Plan.Current is not null && BankSnapshot is not null;

    /// <summary>The Profit Summary's default selection, in the canonical work order.</summary>
    private List<Project> LiveProjects =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(ProjectMultiSelect.IsLiveJob)
            .InWorkOrder()
            .ToList();

    private bool ProfitReady =>
        Projects.Current is not null
        && LiveProjects.All(project =>
            loadedProjects.Contains(project.ProjectId) || failedProjects.Contains(project.ProjectId));

    private List<Project> FailedProjects =>
        LiveProjects.Where(project => failedProjects.Contains(project.ProjectId)).ToList();

    /// <summary>The Profit Summary's league order; null until every live job has answered.</summary>
    private IReadOnlyList<(Project Project, ProfitRow Row)>? ProfitRows =>
        ProfitReady
            ? LiveProjects
                .Where(project => loadedProjects.Contains(project.ProjectId))
                .Select(project => (Project: project, Row: Profit.RowFor(project.ProjectId)))
                .OrderBy(entry => entry.Project.Stage.WorkRank())
                .ThenByDescending(entry => entry.Row.CurrentProfit)
                .ToList()
            : null;

    /// <summary>The 13-week plan exactly as the Weekly Cashflow page builds it, exclusions honoured.</summary>
    private WeeklyCashflowView? WeeklyView()
    {
        if (!WeeklyReady) return null;
        var plan = Plan.Current!;
        var billSeeds = PayablesSnapshot!.Bills.Select(WeeklyCashflowSeeding.FromBill);
        var invoiceSeeds = ReceivablesSnapshot!.Invoices.Select(WeeklyCashflowSeeding.FromInvoice);
        var (counted, _) = WeeklyCashflowSeeding.Split(billSeeds.Concat(invoiceSeeds), plan.Exclusions);
        return WeeklyCashflowMaths.Build(
            today, counted, plan.Items, plan.Placements,
            BankReady ? BankSnapshot!.TotalCash : null);
    }

    private static string WeekAxisLabel(DateTime weekStart) => weekStart.ToString("d MMM");

    private int OpenLeadCount =>
        Leads.Current?.Count(lead => lead.Stage.IsOpen()) ?? 0;

    private const string NeedsXero = "Needs Xero";

    private string BankCashText => BankReady ? Money(BankSnapshot!.TotalCash) : NeedsXero;

    private string BankCashCaption
    {
        get
        {
            if (!BankReady) return "The bank position is read from Xero.";
            var fetched = BankSnapshot!.FetchedAtUtc is { } at ? at.ToLocalTime().ToString("HH:mm") : "—";
            return $"{BankSnapshot.BankAccounts.Count} accounts · Xero, as of {fetched}";
        }
    }

    private bool PayablesRead => PayablesSnapshot is { IsConfigured: true, Error: null };
    private bool ReceivablesRead => ReceivablesSnapshot is { IsConfigured: true, Error: null };

    private decimal? OverduePayables => PayablesRead
        ? PayablesSnapshot!.Bills.Where(bill => AgedPayablesMaths.IsOverdue(bill, today.UtcDateTime)).Sum(AgedPayablesMaths.SignedAmountDue)
        : null;

    private decimal? OverdueReceivables => ReceivablesRead
        ? ReceivablesSnapshot!.Invoices.Where(invoice => AgedReceivablesMaths.IsOverdue(invoice, today.UtcDateTime)).Sum(AgedReceivablesMaths.SignedAmountDue)
        : null;

    private string PayablesText => PayablesRead
        ? Money(PayablesSnapshot!.Bills.Sum(AgedPayablesMaths.SignedAmountDue))
        : NeedsXero;

    private string PayablesCaption
    {
        get
        {
            if (!PayablesRead) return "Aged Payables is read from Xero.";
            var inDraft = PayablesSnapshot!.Bills.Where(bill => bill.IsDraft).Sum(AgedPayablesMaths.SignedAmountDue);
            return $"{Money(OverduePayables ?? 0m)} overdue · incl. {Money(inDraft)} in draft";
        }
    }

    private string ReceivablesText => ReceivablesRead
        ? Money(ReceivablesSnapshot!.Invoices.Sum(AgedReceivablesMaths.SignedAmountDue))
        : NeedsXero;

    private string ReceivablesCaption => ReceivablesRead
        ? $"{Money(OverdueReceivables ?? 0m)} overdue"
        : "Aged Receivables is read from Xero.";

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Projects.OnChanged += StateHasChanged;
        Cash.OnChange += StateHasChanged;
        Payables.OnChange += StateHasChanged;
        Receivables.OnChange += StateHasChanged;
        Plan.OnChanged += StateHasChanged;
        Leads.OnChanged += StateHasChanged;

        _ = Cash.RefreshAsync();
        _ = Payables.RefreshAsync();
        _ = Receivables.RefreshAsync();

        await Task.WhenAll(LoadPlanAsync(), LoadLeadsAsync(), LoadProfitAsync());
    }

    private async Task LoadPlanAsync()
    {
        try
        {
            await Plan.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            planFailed = true;
        }
    }

    /// <summary>A leads read that fails has been reported by the query client; the tile shows nothing.</summary>
    private async Task LoadLeadsAsync()
    {
        try
        {
            if (Leads.Current is null) await Leads.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            leadsFailed = true;
        }
    }

    private async Task LoadProfitAsync()
    {
        try
        {
            if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            projectsFailed = true;
            return;
        }
        await Task.WhenAll(LiveProjects.Select(async project =>
        {
            await throttle.WaitAsync();
            try
            {
                await Profit.LoadAsync(project.ProjectId, CancellationToken.None);
                loadedProjects.Add(project.ProjectId);
            }
            catch
            {
                failedProjects.Add(project.ProjectId);
            }
            finally
            {
                throttle.Release();
            }
        }));
        StateHasChanged();
    }

    public void Dispose()
    {
        Projects.OnChanged -= StateHasChanged;
        Cash.OnChange -= StateHasChanged;
        Payables.OnChange -= StateHasChanged;
        Receivables.OnChange -= StateHasChanged;
        Plan.OnChanged -= StateHasChanged;
        Leads.OnChanged -= StateHasChanged;
    }
}
