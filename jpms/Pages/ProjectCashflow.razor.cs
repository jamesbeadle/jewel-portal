using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectCashflow
{
    [Parameter] public string ProjectId { get; set; } = "";

    // Session checked and the user is signed in. This is NOT "the data is here" — keeping the two
    // apart is the whole point: this page reveals its chrome immediately and holds the statement
    // until every figure in it can be shown at once.

    // A failed fetch must open the gate, or the jewel pulses forever. The panel then says so
    // instead of showing figures; the toast at the top carries the reference and the detail.
    private bool dataFailed;

    // Nullable on purpose. An empty list is a real answer here and sums to a real-looking zero,
    // so "not yet fetched" needs to be a distinct state.
    private ProjectValuationInvoiceSummary? invoiceSummary;
    private IReadOnlyList<WorkOrderInvoiceSummary>? woSummaries;
    private IReadOnlyList<ProjectCostOfSalesLine>? costLines;
    private IReadOnlyList<PackageReconciliationRow>? packageRows;
    private IReadOnlyList<UnallocatedSiteBill>? unallocatedBills;
    private IReadOnlyList<VariationOrder>? variationOrders;

    private IReadOnlyList<WorkOrderInvoiceSummary> WoSummaries => woSummaries ?? Array.Empty<WorkOrderInvoiceSummary>();
    private IReadOnlyList<ProjectCostOfSalesLine> CostLines => costLines ?? Array.Empty<ProjectCostOfSalesLine>();
    private IReadOnlyList<PackageReconciliationRow> PackageRows => packageRows ?? Array.Empty<PackageReconciliationRow>();

    // A refresh that failed has "arrived" as far as the gate is concerned — the read model records
    // the failure rather than the rows, so waiting for rows that will never come is a hang.
    private bool SummaryArrived => Summary.LoadedFor(ProjectId) || Summary.LastRefreshFailed(ProjectId);

    // ── One gate for the whole statement: every subtotal is arithmetic over every source,
    // so a half-arrived statement would show running totals that are simply wrong. ──
    private bool StatementReady =>
        SummaryArrived
        && invoiceSummary is not null
        && ValuationReport.ReportLoadedFor(ProjectId)
        && Retention.RetentionLoadedFor(ProjectId)
        && WorkOrders.LoadedFor(ProjectId)
        && woSummaries is not null
        && costLines is not null
        && packageRows is not null
        && unallocatedBills is not null
        && variationOrders is not null;

    // ── The claim — the valuation report's counting lines ──
    private decimal ContractSales => Summary.Current(ProjectId).Sum(row => row.BudgetedSales);

    // ── Retention — from the latest claim on the Valuation Report (live for drafts),
    // computed by the same helper as the valuation summary footer so the tabs agree.
    private ValuationClaim? LatestClaim =>
        ValuationReport.ClaimsFor(ProjectId).OrderByDescending(claim => claim.ClaimNumber).FirstOrDefault();

    private ValuationSummaryFigures ValuationFigures => ValuationSummaryFigures.For(
        ValuationReport.LinesFor(ProjectId),
        LatestClaim is { Status: ValuationClaimStatus.Draft } draft
            ? ValuationReport.EntriesFor(draft.ValuationClaimId)
            : Array.Empty<ClaimLine>(),
        LatestClaim,
        invoiceSummary?.TotalCertified ?? 0m, invoiceSummary?.TotalDepositCredited ?? 0m);

    // ── Retention terms + schedule — the project's retention record (Setup tab). No record
    // means the retention rows show a dash and add nothing back: a claim's retention/release
    // percentages are a valuation-report concern, not evidence that money has actually been
    // withheld or freed.
    private ProjectRetention? RetentionTerms => Retention.RetentionFor(ProjectId);

    private RetentionSchedule? Schedule => RetentionTerms is { } terms
        ? RetentionSchedule.For(terms, ValuationFigures.TotalWorksComplete, ValuationFigures.RevisedContractSum)
        : null;

    private decimal RetentionOutstanding => Schedule?.Outstanding ?? 0m;

    // ── Cash allocated and left to claim ──
    private decimal CashReceived => invoiceSummary?.TotalPaid ?? 0m;
    private decimal InvoicedAwaitingPayment => invoiceSummary?.Outstanding ?? 0m;
    private decimal CashAllocated => CashReceived + RetentionOutstanding;

    // Left to claim is NET of the retention that future valuations will withhold: valuation
    // invoices are raised net of retention, so that slice of the remainder is never
    // invoiceable on a valuation — it comes back through the release rows instead. Without
    // this deduction the statement counts it twice (here and in the forecast releases).
    // Shared with CashSummary.razor via CashflowMaths so the two pages agree to the penny.
    private decimal RetentionPercent => RetentionTerms?.RetentionPercent ?? 0m;

    private decimal RetentionStillToWithhold => CashflowMaths.RetentionStillToWithhold(
        ContractSales, ValuationFigures.TotalWorksComplete, RetentionPercent);

    private decimal LeftToClaim => CashflowMaths.LeftToClaim(
        ContractSales, CashReceived, RetentionOutstanding, RetentionStillToWithhold);

    // ── The cash still to go out — work orders committed and Xero purchase spend ──
    // Drafts count (an intended commitment being written up); rejected drafts don't —
    // matching ProjectDrawdown.CommittedByCostCode below.
    private decimal WoCommitted =>
        WorkOrders.Current(ProjectId).Where(detail => !detail.Order.IsRejected).Sum(detail => detail.Order.Value);
    private decimal WoInvoiced => WoSummaries.Sum(summary => summary.InvoicedToDate);
    private decimal WoLeftToInvoice => WoCommitted - WoInvoiced;

    // The statement's figure and the modal's rows come from the same filter, so the
    // modal's total can never disagree with the row it explains. OutstandingNet is
    // part-payment aware (XeroPaymentMaths): a settled bill contributes 0, a part-paid
    // one only its remainder — so the figure tracks Xero's aged payables, not just the
    // bill's binary status.
    private IReadOnlyList<ProjectCostOfSalesLine> UnpaidCostLines =>
        CostLines.Where(line => line.OutstandingNet != 0m).ToList();

    private decimal BillsUnpaid => CostLines.Sum(line => line.OutstandingNet);

    // What has already been settled on bills that are only part-paid — the face-of-the-
    // statement explanation for why this figure is lower than the sum of open bills.
    private decimal BillsSettledOnPartPaid =>
        CostLines.Where(line => line.SettledFraction is > 0m and < 1m).Sum(line => line.PaidNet);

    // The unallocated guard: unpaid site-tracked lines nobody has allocated. Not part of
    // BillsUnpaid (allocation is what distributes cost to centres) — surfaced as a warning
    // here and a section in the modal so the gap is never silent.
    private decimal UnallocatedOutstandingNet =>
        (unallocatedBills ?? Array.Empty<UnallocatedSiteBill>()).Sum(bill => bill.OutstandingNet);

    // Whether the unpaid-invoices breakdown modal is open (the magnifier on its row).
    private bool showUnpaidInvoices;

    // The Financials tab's drawdown split, to the penny — computed by the shared
    // calculator from the same inputs (finalised centres realised to profit / loss and
    // reconciliation packages accounted for there, not a flat target − orders − spend).
    // The statement spends the DRAWDOWN side only (finance director, 2026-08-17): the old
    // netted figure read as recouping every overspend, flattering the cashflow. The
    // overspends surface below the completion total as the buy back still available.
    private IReadOnlyDictionary<string, decimal> WoCommittedByCode =>
        ProjectDrawdown.CommittedByCostCode(WorkOrders.Current(ProjectId));

    private DrawdownSplit DrawdownSplit =>
        ProjectDrawdown.SplitForProject(Summary.Current(ProjectId), WoCommittedByCode, PackageRows);

    private decimal Drawdown => DrawdownSplit.Drawdown;

    // The overspend side, flipped positive: committed cost already past target. Cash that
    // comes back only where an overspend is actually bought back — never assumed above.
    private decimal AvailableToBuyBack => -DrawdownSplit.Overspend;

    // ── Retention releases — only a release still forecast is added back. A confirmed release
    // has left the retention pot (Schedule.Outstanding excludes it), so it already sits inside
    // left to claim until invoiced and paid; adding it back again would count it twice. ──
    private decimal Release1AddBack =>
        Schedule?.CompletionRelease is { IsConfirmed: false } line ? line.Amount : 0m;

    private decimal Release2AddBack =>
        Schedule?.FinalRelease is { IsConfirmed: false } line ? line.Amount : 0m;

    private string Release1Note =>
        RetentionTerms is not { } terms || Schedule is not { } schedule
            ? "No retention terms — add them on the Setup tab."
            : schedule.CompletionRelease.IsConfirmed
                ? $"{Money(schedule.CompletionRelease.Amount)} released {schedule.CompletionRelease.ConfirmedAt?.ToString("d MMM yyyy")} — now part of the claim above"
                : $"{Pct(terms.CompletionReleasePercent)} at practical completion{(schedule.CompletionRelease.DueOn is { } due ? $" · due {due:d MMM yyyy}" : "")} (forecast)";

    private string Release2Note =>
        RetentionTerms is not { } terms || Schedule is not { } schedule
            ? "No retention terms — add them on the Setup tab."
            : schedule.FinalRelease.IsConfirmed
                ? $"{Money(schedule.FinalRelease.Amount)} released {schedule.FinalRelease.ConfirmedAt?.ToString("d MMM yyyy")} — now part of the claim above"
                : $"Balance after {terms.DefectsPeriodMonths} months{(schedule.FinalRelease.DueOn is { } due ? $" · due {due:d MMM yyyy}" : "")} (forecast)";

    // ── The two running totals the whiteboard is drawn around ──
    private decimal PracticalCompletionCashflow =>
        LeftToClaim - Drawdown - WoLeftToInvoice - BillsUnpaid + Release1AddBack;

    private decimal ProjectCompletionCashflow => PracticalCompletionCashflow + Release2AddBack;

    // The old netted bottom line, reached honestly: the completion total spends the full
    // drawdown, and only THEN is the buy back added — labelled as conditional on achieving it.
    private decimal PositionWithOverspendsBoughtBack => ProjectCompletionCashflow + AvailableToBuyBack;

    // ── Potential — unapproved variations, kept apart from the statement above ──
    // Pre-approval only (quoting / issued / awaiting AI): approved variations are already
    // inside the project claim, rejected ones are gone. Ordered as the Variations tab lists
    // them so the same document reads in the same place on both screens.
    private IReadOnlyList<VariationOrder> PendingVariations =>
        (variationOrders ?? Array.Empty<VariationOrder>())
            .Where(order => order.Status.IsPreApproval())
            .OrderBy(order => order.Number)
            .ToList();

    private decimal PotentialVariationValue =>
        CashflowMaths.PotentialVariationValue(variationOrders ?? Array.Empty<VariationOrder>());

    private decimal PotentialProjectCompletionCashflow =>
        ProjectCompletionCashflow + PotentialVariationValue;

    private static string AddBack(decimal value) => value == 0m ? "—" : $"+ {Money(value)}";


    private static string Pct(decimal value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("en-GB")) + "%";

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        Summary.OnChanged += StateHasChanged;
        WorkOrders.OnChanged += StateHasChanged;
        ValuationReport.OnChange += StateHasChanged;
        Retention.OnChange += StateHasChanged;
        // Refresh once per tab entry (stale-while-revalidate, per the front-end
        // data-loading convention) — cached figures render immediately, then update.
        _ = Summary.RefreshAsync(ProjectId, CancellationToken.None);
        _ = WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
        ValuationReport.Refresh(ProjectId);
        Retention.Refresh(ProjectId);

        // Fired together, awaited together — the statement stays behind its jewel until they land.
        try
        {
            var invoiceSummaryTask = Queries.AskAsync(new GetProjectValuationInvoiceSummary(ProjectId), CancellationToken.None);
            var woSummariesTask = Queries.AskAsync(new ListWorkOrderInvoiceSummaries(ProjectId), CancellationToken.None);
            var costLinesTask = Queries.AskAsync(new ListProjectCostOfSalesLines(ProjectId), CancellationToken.None);
            var packageRowsTask = Queries.AskAsync(new ListPackageReconciliation(ProjectId), CancellationToken.None);
            var unallocatedBillsTask = Queries.AskAsync(new ListUnallocatedSiteBills(ProjectId), CancellationToken.None);
            var variationOrdersTask = Queries.AskAsync(new ListVariationOrdersForProject(ProjectId), CancellationToken.None);
            invoiceSummary = await invoiceSummaryTask;
            woSummaries = await woSummariesTask;
            costLines = await costLinesTask;
            packageRows = await packageRowsTask;
            unallocatedBills = await unallocatedBillsTask;
            variationOrders = await variationOrdersTask;
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference; here we
            // only need to stop the panels waiting on data that is not coming.
            dataFailed = true;
        }
    }

    public void Dispose()
    {
        Summary.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
        ValuationReport.OnChange -= StateHasChanged;
        Retention.OnChange -= StateHasChanged;
    }
}
