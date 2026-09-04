using ClosedXML.Excel;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.WeeklyCashflow.Export;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Weekly Cashflow export is the screen on paper: one line per supplier (a supplier group is
// one line, its members folded in by the same rule the grid uses), a column per week, and the
// band totals, net movement and closing balance the page shows. Pinned here: the folding rule,
// the per-cell arithmetic behind every line, and that the shared writer produces a workbook
// Excel opens with the three tabs laid out as promised.
public sealed class WeeklyCashflowExportTests
{
    // A Thursday. Its week starts Monday 24 Aug 2026; cell 0 = w/c 24 Aug, cell 1 = w/c 31 Aug…
    private static readonly DateTimeOffset Today = new(2026, 8, 27, 0, 0, 0, TimeSpan.Zero);
    private const string Accountant = "fd@jewelbb.co.uk";
    private const string TravisPerkins = "Travis Perkins PLC";
    private const string Hss = "HSS Pro Service Limited";
    private const string GrantAndStone = "GRANT & STONE LIMITED";
    private const int PlanFirstWeekColumn = 2;   // A is the label column
    private const int PlanTotalColumn = 16;      // A + 13 weeks + Later + Total
    private const int DetailReferenceColumn = 2;
    private const int DetailTotalColumn = 19;    // A–D + 13 weeks + Later + Total

    private static DateTimeOffset Day(int month, int day) => new(2026, month, day, 0, 0, 0, TimeSpan.Zero);

    private static WeeklyCashflowSeed Bill(string id, string supplier, decimal amount, DateTimeOffset due) =>
        new(WeeklyCashflowMaths.BillKeyFor(id), WeeklyCashflowBand.SupplierBills, supplier, id, amount, due);

    private static WeeklyCashflowExportInput Input(bool isDirector = true, decimal? openingBalance = 1_000m)
    {
        var seeds = new[]
        {
            Bill("tp1", TravisPerkins, 100m, Day(8, 20)),   // overdue → cell 0
            Bill("tp2", TravisPerkins, 250m, Day(9, 10)),   // due cell 2, moved to cell 3 below
            Bill("hss1", Hss, 40m, Day(8, 25)),             // cell 0, grouped
            Bill("gs1", GrantAndStone, 60m, Day(9, 1)),     // cell 1, grouped (case-insensitively)
            new WeeklyCashflowSeed(WeeklyCashflowMaths.ReceiptKeyFor("inv1"), WeeklyCashflowBand.ClientReceipts, "Mr Smith", "INV-1", 500m, Day(9, 3)),
        };
        var wages = new WeeklyCashflowItem("wages", "Wages", WeeklyCashflowCategory.Subcontractor, 1_000m,
            WeeklyCashflowRecurrence.Weekly, Day(8, 28), null, null, Accountant, Today, null);
        var placements = new[] { new WeeklyCashflowPlacement(WeeklyCashflowMaths.BillKeyFor("tp2"), Day(9, 14), Accountant, Today) };
        var view = WeeklyCashflowMaths.Build(Today, seeds, new[] { wages }, placements, openingBalance);

        var groups = new[] { new WeeklyCashflowSupplierGroup("g1", "Materials", new[] { Hss, "grant & stone limited" }, Accountant, Today) };
        var parked = new[] { Bill("old1", "Old Vendor", 77m, Day(8, 1)) };
        var exclusions = new[] { new WeeklyCashflowExclusion(WeeklyCashflowMaths.BillKeyFor("old1"), Accountant, Today) };
        return new WeeklyCashflowExportInput(view, groups, parked, exclusions, isDirector, isDirector ? 1_000m : null, Today, Today);
    }

    [Fact]
    public void Bands_readOneLinePerSupplier_groupsFirstThenAToZ()
    {
        var input = Input();
        var bands = WeeklyCashflowExportBands.For(input.View, input.SupplierGroups);

        Assert.Equal(new[] { "Client invoices outstanding", "Supplier bills", "Subcontractors" }, bands.Select(band => band.Label));
        var suppliers = bands[1];
        Assert.Equal(new[] { "Materials", TravisPerkins }, suppliers.Lines.Select(line => line.Label));
        Assert.Equal(140m, suppliers.AmountIn(0));
        Assert.Equal(450m, suppliers.Total);

        var travisPerkins = suppliers.Lines[1];
        Assert.Equal(100m, travisPerkins.AmountIn(0));
        Assert.Equal(250m, travisPerkins.AmountIn(3));
        Assert.True(travisPerkins.HasMovedEntryIn(3));
        Assert.False(travisPerkins.HasMovedEntryIn(0));

        // A recurring item's occurrences fold into one line along the weeks.
        var wages = Assert.Single(bands[2].Lines);
        Assert.Equal(13, wages.Entries.Count);
        Assert.Equal(1_000m, wages.AmountIn(12));
        Assert.Equal(0m, wages.AmountIn(input.View.LaterIndex));
    }

    [Fact]
    public void GroupSlices_firstGroupWinsASupplierNamedTwice()
    {
        var input = Input();
        var groups = new[]
        {
            new WeeklyCashflowSupplierGroup("first", "First", new[] { Hss }, Accountant, Today),
            new WeeklyCashflowSupplierGroup("second", "Second", new[] { Hss, GrantAndStone }, Accountant, Today),
        };
        var slices = GroupSlice.For(input.View, groups);
        Assert.Equal(new[] { "First", "Second" }, slices.Select(slice => slice.Group.Name));
        Assert.Equal(new[] { Hss }, slices[0].Entries.Select(entry => entry.Label));
        Assert.Equal(new[] { GrantAndStone }, slices[1].Entries.Select(entry => entry.Label));
    }

    [Fact]
    public void Workbook_opensWithThreeTabs_andThePlanTabAddsUp()
    {
        var bytes = ExcelWorkbookWriter.Write(WeeklyCashflowExport.Build(Input()));

        using var opened = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(new[] { "Weekly plan", "Detail", "Data" }, opened.Worksheets.Select(sheet => sheet.Name));

        var plan = opened.Worksheet("Weekly plan");
        var suppliers = RowLabelled(plan, "Supplier bills");
        Assert.Equal(140d, suppliers.Cell(PlanFirstWeekColumn).GetDouble());
        Assert.Equal(450d, suppliers.Cell(PlanTotalColumn).GetDouble());
        var travisPerkins = RowLabelled(plan, TravisPerkins);
        Assert.Equal(250d, travisPerkins.Cell(PlanFirstWeekColumn + 3).GetDouble());
        Assert.True(travisPerkins.Cell(PlanFirstWeekColumn + 2).IsEmpty());
        // Week 0: nothing in (the invoice lands w/c 31 Aug) against 140 + 1,000 out → −1,140;
        // closing = 1,000 opening − 1,140.
        Assert.Equal(-1_140d, RowLabelled(plan, "Net movement").Cell(PlanFirstWeekColumn).GetDouble());
        Assert.Equal(-140d, RowLabelled(plan, "Closing bank balance").Cell(PlanFirstWeekColumn).GetDouble());
        Assert.Contains(plan.CellsUsed().Select(cell => cell.GetString()), text => text.StartsWith("1 entry excluded"));
        // The label column and the heading rows stay put while the weeks scroll.
        Assert.Equal(1, plan.SheetView.SplitColumn);
        Assert.True(plan.SheetView.SplitRow > 0);
    }

    [Fact]
    public void DetailTab_listsEveryBillUnderItsLine_andParksExcludedOnes()
    {
        var bytes = ExcelWorkbookWriter.Write(WeeklyCashflowExport.Build(Input()));

        using var opened = new XLWorkbook(new MemoryStream(bytes));
        var detail = opened.Worksheet("Detail");
        var labels = detail.CellsUsed().Where(cell => cell.Address.ColumnNumber == 1).Select(cell => cell.GetString()).ToList();
        Assert.Equal(2, labels.Count(label => label.Trim() == TravisPerkins && label.StartsWith(" ")));
        // Parked money travels in the text — never in a week or the Total column, so the column still adds up.
        var parked = RowLabelled(detail, "    Old Vendor");
        Assert.Contains("excluded — not counted (£77.00) · " + Accountant, parked.Cell(DetailReferenceColumn).GetString());
        Assert.True(parked.Cell(DetailTotalColumn).IsEmpty());

        var data = opened.Worksheet("Data");
        Assert.Equal(1 + 18, data.LastRowUsed()!.RowNumber()); // header + 1 invoice + 4 bills + 13 wage runs
    }

    [Fact]
    public void Accounts_getsNoBankLine()
    {
        var workbook = WeeklyCashflowExport.Build(Input(isDirector: false, openingBalance: null));
        var labels = workbook.Sheets[0].Rows
            .Select(row => row.Length > 0 && row[0] is ExcelStyledCell styled ? styled.Value?.ToString() : null)
            .ToList();
        Assert.DoesNotContain("Closing bank balance", labels);
        Assert.DoesNotContain("Cash in bank", labels);
        Assert.Contains("Cash out, 13 weeks", labels);
    }

    private static IXLRow RowLabelled(IXLWorksheet sheet, string label) =>
        sheet.RowsUsed().First(row => row.Cell(1).GetString() == label);
}
