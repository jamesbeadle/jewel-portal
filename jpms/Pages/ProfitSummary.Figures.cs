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
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Figures ------------------------------------------------------------

    private sealed record ProfitRow(
        decimal InitialContractSum,
        decimal InitialContractCosts,
        decimal NetVariations,
        decimal CertifiedToDate,
        decimal ActualCostOfSales,
        decimal ContractValue,
        decimal ForecastCostOfSales,
        decimal WorksComplete,
        decimal RetentionOutstanding)
    {
        public decimal BudgetedProfit => InitialContractSum - InitialContractCosts;
        public decimal CurrentProfit => CertifiedToDate - ActualCostOfSales;
        public decimal ForecastedProfit => ContractValue - ForecastCostOfSales;

        // The "To finish" band: what is left in the job from today. Revenue still to certify,
        // cost still to come, and the profit between them — defined so that
        // CurrentProfit + ToFinishProfit = ForecastedProfit exactly.
        public decimal LeftToCertify => ContractValue - CertifiedToDate;
        public decimal CostToComplete => ForecastCostOfSales - ActualCostOfSales;
        public decimal ToFinishProfit => LeftToCertify - CostToComplete;
        public decimal? ToFinishMargin => MarginOn(ToFinishProfit, LeftToCertify);

        // The bridge's third bar, defined so Budgeted + Variations + CostMovement = Forecast
        // exactly (ContractValue = InitialContractSum + NetVariations makes the algebra close).
        // Negative means the job is forecast to cost more than the deal's target.
        public decimal CostMovement => InitialContractCosts - ForecastCostOfSales;

        // How far the landing has moved from the deal — the "biggest swing" tile.
        public decimal ForecastSwing => ForecastedProfit - BudgetedProfit;

        // Value of work done against the revised sum — the project cell's progress bar. Null
        // when there is no contract value yet: no base, no honest percentage.
        public decimal? PercentComplete => ContractValue == 0m ? null : WorksComplete / ContractValue;

        // Margins on value: each profit against its own revenue column — NOT markup on cost —
        // so the budgeted margin reads ≈9.1% for the assumed 10% markup, by design (see
        // FinancialSummaryAssumptions.CostFactor's remark on the distinction). Null when the
        // revenue base is zero: no base, no honest percentage — the cell's % line simply
        // doesn't render, like any figure that isn't known.
        public decimal? BudgetedMargin => MarginOn(BudgetedProfit, InitialContractSum);
        public decimal? CurrentMargin => MarginOn(CurrentProfit, CertifiedToDate);
        public decimal? ForecastedMargin => MarginOn(ForecastedProfit, ContractValue);

        private static decimal? MarginOn(decimal profit, decimal revenue) =>
            revenue == 0m ? null : profit / revenue;
    }

    private ValuationClaim? LatestClaimFor(string projectId) =>
        Claims.Current(projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .FirstOrDefault();

    private ProfitRow RowFor(string projectId)
    {
        var latest = LatestClaimFor(projectId);
        var entries = latest is { Status: ValuationClaimStatus.Draft }
            ? ClaimEntries.Current(latest.ValuationClaimId)
            : Array.Empty<ClaimLine>();
        var certification = invoicedByProject.TryGetValue(projectId, out var totals) ? totals : (0m, 0m);
        var figures = ValuationSummaryFigures.For(
            Lines.Current(projectId), entries, latest,
            certification.Item1, certification.Item2);

        var summaryRows = Summary.Current(projectId);
        var packages = packagesByProject.TryGetValue(projectId, out var packageRows)
            ? packageRows
            : Array.Empty<PackageReconciliationRow>();

        // Actual cost of sales is the gross allocated spend — the Financials tab's total, which
        // adds packaged invoiced cost back in via the package rows (RowActual + InvoicedToDate).
        var actualCost = summaryRows.Sum(row => row.ActualCost - row.PackagedActualCost)
                         + packages.Sum(package => package.InvoicedToDate);

        return new ProfitRow(
            InitialContractSum: figures.ContractSum,
            // The same target-cost rule as the Financials tab, applied to the initial sum: what
            // the contract should cost us with the assumed markup backed out.
            InitialContractCosts: Math.Round(figures.ContractSum * FinancialSummaryAssumptions.CostFactor, 2),
            NetVariations: figures.NetVariations,
            CertifiedToDate: figures.CertifiedToDate,
            ActualCostOfSales: actualCost,
            ContractValue: figures.RevisedContractSum,
            ForecastCostOfSales: ProjectDrawdown.ForecastCostOfSales(
                summaryRows, ProjectDrawdown.CommittedByCostCode(WorkOrders.Current(projectId)), packages),
            WorksComplete: figures.TotalWorksComplete,
            RetentionOutstanding: figures.RetentionOutstanding);
    }

    // Every region (the strip; the bridge + table + totals together; the export) builds this
    // list once and reads rows from it, rather than each cell calling RowFor for itself.
    // Header sorting, the same behaviour as the Financials table: the Project column starts
    // ascending, every value column starts descending (biggest first — what the numbers are
    // usually sorted for), and clicking the active column flips the direction. A clicked column
    // ranks the WHOLE table — completed jobs mix in among live ones, because the point of
    // clicking is the comparison. Only the default order (no column clicked) keeps the
    // work-order bands.
    private string? sortColumn;
    private bool sortDescending;

    private void SortBy(string column)
    {
        if (sortColumn == column)
        {
            sortDescending = !sortDescending;
            return;
        }
        sortColumn = column;
        sortDescending = column != "project";
    }


    private static decimal SortValue(ProfitRow row, string column) => column switch
    {
        "contract" => row.ContractValue,
        "budgeted" => row.BudgetedProfit,
        "certified" => row.CertifiedToDate,
        "cost" => row.ActualCostOfSales,
        "current" => row.CurrentProfit,
        "left" => row.LeftToCertify,
        "ctc" => row.CostToComplete,
        "tofinish" => row.ToFinishProfit,
        "finalcost" => row.ForecastCostOfSales,
        "forecast" => row.ForecastedProfit,
        _ => 0m
    };

    // The rows the table renders. The default order (no column clicked) is the one departure
    // from the A–Z convention: a profit table reads as a league table, so within each
    // work-order band (live work first, then Defects Period, then Completed — the bands stay,
    // per the project-ordering convention) jobs rank by current profit, best first. Sorted here
    // rather than in SelectedProjects because the rank needs the loaded figures — TableReady
    // gates every caller, so the order never shifts mid-load. Name and reference tie-breaks
    // keep equal-value rows stable between renders.
    private List<(Project Project, ProfitRow Row)> LoadedRows()
    {
        var entries = SelectedProjects.Select(project => (Project: project, Row: RowFor(project.ProjectId)));
        var ordered = sortColumn switch
        {
            null => entries
                .OrderBy(entry => entry.Project.Stage.WorkRank())
                .ThenByDescending(entry => entry.Row.CurrentProfit),
            "project" => sortDescending
                ? entries.OrderByDescending(entry => entry.Project.Name, StringComparer.OrdinalIgnoreCase)
                : entries.OrderBy(entry => entry.Project.Name, StringComparer.OrdinalIgnoreCase),
            _ => sortDescending
                ? entries.OrderByDescending(entry => SortValue(entry.Row, sortColumn))
                : entries.OrderBy(entry => SortValue(entry.Row, sortColumn))
        };
        return ordered
            .ThenBy(entry => entry.Project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Project.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ProfitRow TotalsOf(IReadOnlyList<(Project Project, ProfitRow Row)> rows) =>
        new(
            rows.Sum(entry => entry.Row.InitialContractSum),
            rows.Sum(entry => entry.Row.InitialContractCosts),
            rows.Sum(entry => entry.Row.NetVariations),
            rows.Sum(entry => entry.Row.CertifiedToDate),
            rows.Sum(entry => entry.Row.ActualCostOfSales),
            rows.Sum(entry => entry.Row.ContractValue),
            rows.Sum(entry => entry.Row.ForecastCostOfSales),
            rows.Sum(entry => entry.Row.WorksComplete),
            rows.Sum(entry => entry.Row.RetentionOutstanding));

    // ---- Labour accrual overlay (the basis switch) --------------------------
    // OFF (default): every Xero panel reads the stored Xero months only — the auditable
    // invoiced basis the accountant reconciles against, untouched. ON: the snapshot's
    // LabourAccruals (approved timesheet cost that no Xero-approved bill settles yet — the
    // API computes it against the timesheet-cover marking, so it can never double-count)
    // joins the same months as cost-only pseudo-rows, and the running grid, trajectory and
    // cumulative charts all read the portal's live labour position. The overlay is applied
    // at read time and never written anywhere: switching off is always exactly Xero again,
    // and a fully-settled month carries no accrual, so history converges by construction.

    private bool includeLabourAccrual;

    private IReadOnlyList<XeroSiteMonthlyLabourAccrual> ActiveAccruals =>
        includeLabourAccrual
            ? SitePnl.Current?.LabourAccruals ?? Array.Empty<XeroSiteMonthlyLabourAccrual>()
            : Array.Empty<XeroSiteMonthlyLabourAccrual>();

    // The rows every Xero panel actually reads: the stored Xero months, plus (switch on) the
    // accrual as cost-only pseudo-months. Null while the store hasn't answered — the same
    // "not fetched yet" contract as the snapshot's own Rows.
    private IReadOnlyList<XeroSiteMonthlyPnl>? EffectivePnlRows()
    {
        var stored = SitePnl.Current?.Rows;
        if (stored is null) return null;
        var accruals = ActiveAccruals;
        if (accruals.Count == 0) return stored;
        return stored
            .Concat(accruals.Select(accrual =>
                new XeroSiteMonthlyPnl(accrual.ProjectId, accrual.Month, 0m, accrual.Amount, 0m)))
            .ToList();
    }

    /// <summary>The accrual sitting in ONE month of one project — the grid cell's • marker and hover. Zero when the switch is off.</summary>
    private decimal AccrualOwnFor(string projectId, DateTime month) =>
        ActiveAccruals
            .Where(accrual => string.Equals(accrual.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)
                              && accrual.Month.Year == month.Year && accrual.Month.Month == month.Month)
            .Sum(accrual => accrual.Amount);

    /// <summary>The project's whole accrued position — the "Position now" memo line. Zero when the switch is off.</summary>
    private decimal AccrualToDateFor(string projectId) =>
        ActiveAccruals
            .Where(accrual => string.Equals(accrual.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .Sum(accrual => accrual.Amount);

    /// <summary>The hover suffix a cell gains when its month carries accrual — empty otherwise, so hovers stay clean on the pure-Xero basis.</summary>
    private string AccrualHover(string projectId, DateTime month)
    {
        var amount = AccrualOwnFor(projectId, month);
        return amount > 0m ? $" · incl. {MoneyCompact(amount)} approved labour not yet billed" : "";
    }

    /// <summary>The basis tag every Xero panel's subtitle carries, so the page always says which rung it is reading.</summary>
    private string BasisLabel => includeLabourAccrual ? "invoiced + labour accrual" : "invoiced basis";

}
