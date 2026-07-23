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

    private static WorkOrder WorkOrderWith() =>
        new(WorkOrderId: "WO", ProjectId: "PRJ", BidPackageId: null, SubcontractorId: "S",
            Value: 0m, Scope: "", AwardedAt: default, AwardedByEmail: "", Number: 1, Title: "",
            Status: WorkOrderStatus.Released, CreatedAt: default, ScheduledCompletion: null);

    private static WorkOrderLine Line(string costCode, decimal lineTotal) =>
        new(WorkOrderLineId: "L", WorkOrderId: "WO", Title: "", Description: "", CostType: "",
            CostCode: costCode, Quantity: 0m, Unit: "", UnitCost: 0m, LineTotal: lineTotal,
            PaidToDate: 0m, SortOrder: 0);
}
