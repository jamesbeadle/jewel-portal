using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class CashForecast
{
    // ---- The statement figures (unchanged from the retired Cash Summary) --------------------
    // One line per project, computed exactly as the project's Cashflow tab computes its
    // statement — same sources, same helpers — so the two can never disagree.

    private sealed record CashRow(
        decimal ProjectClaim,
        decimal CashReceived,
        decimal RetentionOutstanding,
        decimal InvoicedAwaitingPayment,
        decimal RetentionStillToWithhold,
        decimal LeftToClaim,
        decimal Drawdown,
        decimal WoCommitted,
        decimal WoInvoiced,
        decimal BillsUnpaid,
        decimal Release1,
        decimal Release2)
    {
        public decimal CashAllocated => CashReceived + RetentionOutstanding;

        public decimal WoLeftToInvoice => WoCommitted - WoInvoiced;

        public decimal PracticalCompletionCashflow =>
            LeftToClaim - Drawdown - WoLeftToInvoice - BillsUnpaid + Release1;

        public decimal ProjectCompletionCashflow => PracticalCompletionCashflow + Release2;
    }

    private ValuationClaim? LatestClaimFor(string projectId) =>
        Claims.Current(projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .FirstOrDefault();

    private RetentionSchedule? ScheduleFor(string projectId, ValuationSummaryFigures figures) =>
        retentionByProject.TryGetValue(projectId, out var terms) && terms is not null
            ? RetentionSchedule.For(terms, figures.TotalWorksComplete, figures.RevisedContractSum)
            : null;

    private ValuationSummaryFigures FiguresFor(string projectId)
    {
        var invoiceSummary = invoiceSummaryByProject.TryGetValue(projectId, out var summary) ? summary : null;
        var latest = LatestClaimFor(projectId);
        var entries = latest is { Status: ValuationClaimStatus.Draft }
            ? ClaimEntries.Current(latest.ValuationClaimId)
            : Array.Empty<ClaimLine>();
        return ValuationSummaryFigures.For(
            Lines.Current(projectId), entries, latest,
            invoiceSummary?.TotalCertified ?? 0m, invoiceSummary?.TotalDepositCredited ?? 0m);
    }

    private CashRow RowFor(string projectId)
    {
        var invoiceSummary = invoiceSummaryByProject.TryGetValue(projectId, out var summary) ? summary : null;
        var figures = FiguresFor(projectId);

        // Retention terms + schedule — no record means the retention figures show nothing and
        // add nothing back, exactly as the Cashflow tab treats a project without terms.
        var schedule = ScheduleFor(projectId, figures);
        var retentionOutstanding = schedule?.Outstanding ?? 0m;
        var terms = retentionByProject.TryGetValue(projectId, out var retention) ? retention : null;

        var summaryRows = Summary.Current(projectId);
        var orders = WorkOrders.Current(projectId);

        var woCommitted = orders.Where(detail => !detail.Order.IsRejected).Sum(detail => detail.Order.Value);
        var woInvoiced = (woSummariesByProject.TryGetValue(projectId, out var woSummaries)
                ? woSummaries
                : Array.Empty<WorkOrderInvoiceSummary>())
            .Sum(orderSummary => orderSummary.InvoicedToDate);

        var billsUnpaid = (costLinesByProject.TryGetValue(projectId, out var costLines)
                ? costLines
                : Array.Empty<ProjectCostOfSalesLine>())
            .Sum(line => line.OutstandingNet);

        var packages = packagesByProject.TryGetValue(projectId, out var packageRows)
            ? packageRows
            : Array.Empty<PackageReconciliationRow>();

        // The DRAWDOWN side only, matching the project Cashflow tab (finance director,
        // 2026-08-17): the netted figure read as recouping every overspend, flattering the
        // forecast. Spending the full drawdown is the conservative position — an overspend
        // comes back only where it is actually bought back, which this forecast never assumes.
        var drawdown = ProjectDrawdown.SplitForProject(
            summaryRows, ProjectDrawdown.CommittedByCostCode(orders), packages).Drawdown;

        var projectClaim = summaryRows.Sum(row => row.BudgetedSales);
        var cashReceived = invoiceSummary?.TotalPaid ?? 0m;

        // Left to claim is net of the retention future valuations will withhold — the same
        // CashflowMaths the project Cashflow tab uses, so the two agree to the penny.
        var retentionStillToWithhold = CashflowMaths.RetentionStillToWithhold(
            projectClaim, figures.TotalWorksComplete, terms?.RetentionPercent ?? 0m);

        return new CashRow(
            ProjectClaim: projectClaim,
            CashReceived: cashReceived,
            RetentionOutstanding: retentionOutstanding,
            InvoicedAwaitingPayment: invoiceSummary?.Outstanding ?? 0m,
            RetentionStillToWithhold: retentionStillToWithhold,
            LeftToClaim: CashflowMaths.LeftToClaim(
                projectClaim, cashReceived, retentionOutstanding, retentionStillToWithhold),
            Drawdown: drawdown,
            WoCommitted: woCommitted,
            WoInvoiced: woInvoiced,
            BillsUnpaid: billsUnpaid,
            Release1: schedule?.CompletionRelease is { IsConfirmed: false } completion ? completion.Amount : 0m,
            Release2: schedule?.FinalRelease is { IsConfirmed: false } final ? final.Amount : 0m);
    }

    private CashRow Totals()
    {
        var rows = SelectedProjects.Select(project => RowFor(project.ProjectId)).ToList();
        return new CashRow(
            rows.Sum(row => row.ProjectClaim),
            rows.Sum(row => row.CashReceived),
            rows.Sum(row => row.RetentionOutstanding),
            rows.Sum(row => row.InvoicedAwaitingPayment),
            rows.Sum(row => row.RetentionStillToWithhold),
            rows.Sum(row => row.LeftToClaim),
            rows.Sum(row => row.Drawdown),
            rows.Sum(row => row.WoCommitted),
            rows.Sum(row => row.WoInvoiced),
            rows.Sum(row => row.BillsUnpaid),
            rows.Sum(row => row.Release1),
            rows.Sum(row => row.Release2));
    }

}
