using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The project drawdown on the Cashflow tab must equal the Financials tab's Total Drawdown to
// the penny. ProjectDrawdown is the single calculator both tabs call; these tests pin the
// rules the old flat "target − orders − spend" shortcut missed: finalised centres realise to
// profit / loss (out of drawdown), reconciliation packages carry their own drawdown, and only
// cost-coded work-order lines commit against a centre.
public sealed class ProjectDrawdownTests
{
    private static ProjectFinancialSummaryRow Centre(
        string costCode, decimal budgetedSales, decimal nonWoCost = 0m, bool finalised = false,
        decimal packagedSales = 0m, decimal packagedWoCommitted = 0m, decimal packagedNonWoCost = 0m) =>
        new(costCode,
            BudgetedSales: budgetedSales,
            BudgetedCost: Math.Round(budgetedSales * FinancialSummaryAssumptions.CostFactor, 2),
            CompletionPercent: 0m,
            ExpectedActualCost: 0m,
            ActualCost: 0m,
            UnderOverExpected: 0m,
            NonWorkOrderActualCost: nonWoCost,
            IsFinalised: finalised,
            PackagedSales: packagedSales,
            PackagedWoCommitted: packagedWoCommitted,
            PackagedNonWoCost: packagedNonWoCost);

    private static PackageReconciliationRow Package(decimal drawdown, bool locked) =>
        new(ReconciliationPackageId: "P",
            Name: "pkg",
            IsLocked: locked,
            LockedAt: null,
            WorkOrderCount: 0,
            SalesLineCount: 0,
            SalesValue: 0m,
            ClaimedToDate: 0m,
            TargetCost: 0m,
            WoCommitted: 0m,
            InvoicedToDate: 0m,
            Drawdown: drawdown,
            Margin: 0m,
            ProfitLoss: 0m);

    [Fact]
    public void Mirrors_the_financials_total_across_finalisation_packages_and_wo_only_codes()
    {
        var rows = new[]
        {
            Centre("A", budgetedSales: 110_000m),                          // target 100,000; committed 40,000 -> 60,000
            Centre("B", budgetedSales: 55_000m, nonWoCost: 5_000m),        // target 50,000; -5,000 -10,000 -> 35,000
            Centre("C", budgetedSales: 22_000m, finalised: true),          // finalised -> excluded
            Centre("D", budgetedSales: 33_000m, nonWoCost: 2_000m,         // fully packaged -> 0
                   packagedSales: 33_000m, packagedWoCommitted: 8_000m, packagedNonWoCost: 2_000m),
        };
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 40_000m, ["B"] = 10_000m, ["C"] = 5_000m, ["D"] = 8_000m, ["E"] = 3_000m, // E is WO-only
        };
        var packages = new[] { Package(1_500m, locked: false), Package(9_999m, locked: true) };

        var drawdown = ProjectDrawdown.ForProject(rows, committed, packages);

        // 60,000 + 35,000 + 0 (C finalised) + 0 (D packaged) − 3,000 (E) + 1,500 (unlocked pkg)
        Assert.Equal(93_500.00m, drawdown);
    }

    [Fact]
    public void Finalised_centres_do_not_count_toward_drawdown()
    {
        var rows = new[] { Centre("X", budgetedSales: 110_000m, finalised: true) };
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["X"] = 10_000m };
        Assert.Equal(0m, ProjectDrawdown.ForProject(rows, committed, Array.Empty<PackageReconciliationRow>()));
    }

    [Fact]
    public void Work_order_only_codes_reduce_drawdown_by_their_committed_value()
    {
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["Z"] = 7_000m };
        Assert.Equal(-7_000m, ProjectDrawdown.ForProject(
            Array.Empty<ProjectFinancialSummaryRow>(), committed, Array.Empty<PackageReconciliationRow>()));
    }

    [Fact]
    public void Locked_packages_are_excluded_and_unlocked_packages_add_their_drawdown()
    {
        var rows = new[] { Centre("Y", budgetedSales: 11_000m) };   // target 10,000, nothing committed
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(10_000m, ProjectDrawdown.ForProject(rows, committed, new[] { Package(500m, locked: true) }));
        Assert.Equal(10_500m, ProjectDrawdown.ForProject(rows, committed, new[] { Package(500m, locked: false) }));
    }

    [Fact]
    public void Split_separates_underspent_and_overspent_centres_per_centre()
    {
        var rows = new[]
        {
            Centre("A", budgetedSales: 110_000m),   // target 100,000; committed 40,000 -> +60,000
            Centre("B", budgetedSales: 11_000m),    // target 10,000; committed 25,000 -> −15,000
        };
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 40_000m, ["B"] = 25_000m,
        };

        var split = ProjectDrawdown.SplitForProject(rows, committed, Array.Empty<PackageReconciliationRow>());

        // Split per centre: B's overspend must not be swallowed by A's drawdown.
        Assert.Equal(60_000m, split.Drawdown);
        Assert.Equal(-15_000m, split.Overspend);
        Assert.Equal(45_000m, split.Net);
        Assert.Equal(split.Net, ProjectDrawdown.ForProject(rows, committed, Array.Empty<PackageReconciliationRow>()));
    }

    [Fact]
    public void Split_signs_unlocked_package_drawdowns_and_skips_locked_ones()
    {
        var packages = new[]
        {
            Package(1_500m, locked: false),
            Package(-700m, locked: false),
            Package(9_999m, locked: true),
        };

        var split = ProjectDrawdown.SplitForProject(
            Array.Empty<ProjectFinancialSummaryRow>(),
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
            packages);

        Assert.Equal(1_500m, split.Drawdown);
        Assert.Equal(-700m, split.Overspend);
    }

    [Fact]
    public void Forecast_is_committed_plus_positive_drawdown_only()
    {
        var rows = new[]
        {
            Centre("A", budgetedSales: 110_000m),   // target 100,000; committed 40,000 -> forecast 100,000
            Centre("B", budgetedSales: 11_000m),    // target 10,000; committed 25,000 -> forecast 25,000
        };
        var committed = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 40_000m, ["B"] = 25_000m,
        };

        var forecast = ProjectDrawdown.ForecastCostOfSales(rows, committed, Array.Empty<PackageReconciliationRow>());

        // Committed 65,000 + drawdown 60,000: A forecasts its full target, B its committed cost.
        Assert.Equal(125_000m, forecast);
    }

    [Fact]
    public void Forecast_counts_unlocked_packages_at_their_committed_figure_plus_drawdown()
    {
        // One unlocked package: target 10,000, drawdown 1,500 -> committed 8,500, forecast 10,000.
        var package = Package(1_500m, locked: false) with { TargetCost = 10_000m };

        var forecast = ProjectDrawdown.ForecastCostOfSales(
            Array.Empty<ProjectFinancialSummaryRow>(),
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase),
            new[] { package });

        Assert.Equal(10_000m, forecast);
    }

    [Fact]
    public void CommittedByCostCode_sums_cost_coded_lines_and_ignores_blank_codes()
    {
        var detail = new ProjectWorkOrderDetail(
            Order: WorkOrderWith(),
            SubcontractorName: "Sub",
            Lines: new[]
            {
                Line("100", 4_000m),
                Line("100", 1_000m),
                Line("200", 2_500m),
                Line("", 9_999m),   // no cost code -> ignored
            });

        var byCode = ProjectDrawdown.CommittedByCostCode(new[] { detail });

        Assert.Equal(5_000m, byCode["100"]);
        Assert.Equal(2_500m, byCode["200"]);
        Assert.False(byCode.ContainsKey(""));
    }

    [Fact]
    public void CommittedByCostCode_countsDrafts_butNeverRejected()
    {
        var released = new ProjectWorkOrderDetail(
            Order: WorkOrderWith(),
            SubcontractorName: "Sub",
            Lines: new[] { Line("100", 4_000m) });
        // A draft is an intended commitment being written up: the Financials tab counts it.
        var draft = new ProjectWorkOrderDetail(
            Order: WorkOrderWith() with { WorkOrderId = "WO-D", Number = 0, Status = WorkOrderStatus.Draft },
            SubcontractorName: "Sub",
            Lines: new[] { Line("100", 6_000m) });
        // A rejected draft counts nowhere — the decision was no.
        var rejected = new ProjectWorkOrderDetail(
            Order: WorkOrderWith() with { WorkOrderId = "WO-R", Number = 0, Status = WorkOrderStatus.Rejected },
            SubcontractorName: "Sub",
            Lines: new[] { Line("100", 9_999m), Line("300", 2_000m) });

        var byCode = ProjectDrawdown.CommittedByCostCode(new[] { released, draft, rejected });

        Assert.Equal(10_000m, byCode["100"]);         // 4,000 released + 6,000 draft
        Assert.False(byCode.ContainsKey("300"));      // a rejected-only centre commits nothing
    }

    private static WorkOrder WorkOrderWith() =>
        new(WorkOrderId: "WO", ProjectId: "PRJ", BidPackageId: null, SubcontractorId: "S",
            Value: 0m, Scope: "", AwardedAt: default, AwardedByEmail: "", Number: 1, Title: "",
            Status: WorkOrderStatus.Released, CreatedAt: default, ScheduledCompletion: null);

    private static WorkOrderLine Line(string costCode, decimal lineTotal) =>
        new(WorkOrderLineId: "L", WorkOrderId: "WO", Title: "", Description: "", CostType: "",
            CostCode: costCode, Quantity: 0m, Unit: "", UnitCost: 0m, LineTotal: lineTotal,
            PaidToDate: 0m, SortOrder: 0);
}
