using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The matching rules (2026-09-08) as the unallocated read applies them: reference beats
/// supplier, numbers are per project, the labour registry wins, the value gate holds, and the
/// proposed coding is pro rata to the order's own lines to the penny.
/// </summary>
public sealed class WorkOrderBillRecognitionTests
{
    [Fact]
    public async Task ASupplierWithTwoOpenOrdersAndNoReferenceReachesTheCardWithNothingProposed()
    {
        // The bottom of the ladder: £10,000 names no order and is not what is left on WO-0026
        // (£97,810) or WO-0001 (£14,940), alone or together — so the card lists both at nothing.
        var fixture = await WorkOrderBillFixture.CreateAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroLedgerLineId == "inv-1724:0");

        var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
        Assert.Equal(WorkOrderMatchRule.BySupplierOrders, match.Rule);
        Assert.Contains("2 open orders", match.Detail);
        Assert.Contains("not what is left on any of them", match.Detail);
        Assert.All(match.ProposedSlices, slice => Assert.Equal(0m, slice.Net));
        Assert.Equal(2, match.ProposedSlices.Count);
    }

    [Fact]
    public async Task TheAccountantsLadder_ABillThatIsExactlyWhatIsLeftOnTwoOrdersIsProposedAcrossThem()
    {
        // Sussex Tiling, Lees Green-001 (2026-09-11): £3,092 net, no order named, three open
        // orders — £1,748 + £1,344 is the only way the open orders make the total, so the card
        // lands with those two figures and needs only Approve. WO-0049's £14,921 is untouched.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 1748m, ("INT-PLS", 1748m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-49", WorkOrderBillFixture.Woodhouse, 49, "sub-dry", 14921m, ("INT-PLS", 14921m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 1344m, ("INT-PLB", 1344m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-lg", "Lees Green-001", "Drywall Co Ltd", ("321", 720m), ("322", 2372m));
        await fixture.Context.SaveChangesAsync();

        var lines = (await fixture.ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == "inv-lg").ToList();

        Assert.All(lines, line =>
        {
            var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
            Assert.Equal(WorkOrderMatchRule.ByRemainingValue, match.Rule);
            Assert.Contains("£3,092.00 is exactly what is left to invoice on WO-0055 (£1,748.00) + WO-0056 (£1,344.00)", match.Detail);
            Assert.Equal(
                new[] { ("wo-lg-55", 1748m), ("wo-lg-56", 1344m) },
                match.ProposedSlices.Select(slice => (slice.WorkOrderId, slice.Net)));
            Assert.Equal(4, match.SupplierOrders.Count);
        });
    }

    [Fact]
    public async Task TheAccountantsLadder_ABillThatIsExactlyWhatIsLeftOnOneOrderTakesThatOrder()
    {
        // Rung 3: several open orders, the total is what is left on WO-0055 alone — a part-invoiced
        // order counts by what is LEFT, not its value.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 5000m, ("INT-PLS", 5000m));
        fixture.Context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity { XeroLineWorkOrderLinkId = "L55", XeroLedgerLineId = "old-55", WorkOrderId = "wo-lg-55", ProjectId = WorkOrderBillFixture.Woodhouse, Amount = 3252m });
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-final", "Lees Green-002", "Drywall Co Ltd", ("321", 1748m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-final");

        var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
        Assert.Equal((WorkOrderMatchRule.ByRemainingValue, "wo-lg-55"), (match.Rule, match.WorkOrderId));
        Assert.Contains("exactly what is left to invoice on WO-0055", match.Detail);
        var slice = Assert.Single(match.ProposedSlices);
        Assert.Equal(("wo-lg-55", 1748m), (slice.WorkOrderId, slice.Net));
    }

    [Fact]
    public async Task TheAccountantsLadder_ATotalTheAmountsMakeTwoWaysProposesNothing()
    {
        // Two orders with £1,748 left each and a £1,748 bill: either fits, so neither is guessed.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 1748m, ("INT-PLS", 1748m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 1748m, ("INT-PLB", 1748m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-twin", "Lees Green-003", "Drywall Co Ltd", ("321", 1748m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-twin");

        var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
        Assert.Equal(WorkOrderMatchRule.BySupplierOrders, match.Rule);
        Assert.Contains("more than one combination", match.Detail);
        Assert.All(match.ProposedSlices, slice => Assert.Equal(0m, slice.Net));
    }

    [Fact]
    public async Task TheAccountantsLadder_AReferenceOnTheBillStillBeatsTheAmounts()
    {
        // Rung 1 over rung 3: the total is what is left on WO-0056, but the bill names WO-0055.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 5000m, ("INT-PLS", 5000m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 1344m, ("INT-PLB", 1344m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-ref", "Lees Green-004", "Drywall Co Ltd", ("321", 1344m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-ref", "WO-0055");

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-ref");

        Assert.Equal((WorkOrderMatchRule.ByReference, "wo-lg-55"), (line.WorkOrderMatch?.Rule, line.WorkOrderMatch?.WorkOrderId));
        // ... and the conflict is badged, not acted on: the amount fits WO-0056, the reference wins.
        Assert.Contains("Amount fits WO-0056 (£1,344.00 left)", line.WorkOrderMatch!.AmountNote);
        Assert.Contains("names WO-0055, so it lands on WO-0055", line.WorkOrderMatch.AmountNote);
    }

    [Fact]
    public async Task TheAmountsAgreeingWithTheReferenceEarnNoBadge()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 1344m, ("INT-PLS", 1344m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 5000m, ("INT-PLB", 5000m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-agree", "Lees Green-005", "Drywall Co Ltd", ("321", 1344m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-agree", "WO-0055");

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-agree");

        Assert.Equal(WorkOrderMatchRule.ByReference, line.WorkOrderMatch?.Rule);
        Assert.Null(line.WorkOrderMatch!.AmountNote);
    }

    [Fact]
    public async Task AReferenceOnTheBillPicksTheOrderAndEveryLineCarriesTheMatch()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await SetReferenceAsync(fixture, "inv-1724", "WO-0026");

        var lines = (await fixture.ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == "inv-1724").ToList();

        Assert.All(lines, line =>
        {
            var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
            Assert.Equal(("wo-bf-26", "WO-0026", WorkOrderMatchRule.ByReference, WorkOrderBillFixture.ByFrance), (match.WorkOrderId, match.WorkOrderReference, match.Rule, match.ProjectId));
            var order = match.SupplierOrders.Single(candidate => candidate.WorkOrderId == "wo-bf-26");
            Assert.Equal((97810m, 0m), (order.OrderValue, order.InvoicedToDate));
            Assert.Equal(new[] { "wo-bf-26", "wo-ra-01" }, match.SupplierOrders.Select(candidate => candidate.WorkOrderId).OrderBy(id => id));
            var slice = Assert.Single(match.ProposedSlices);
            Assert.Equal(("wo-bf-26", 10000m), (slice.WorkOrderId, slice.Net));
        });
    }

    [Theory]
    [InlineData("WO-0026")]
    [InlineData("wo 26")]
    [InlineData("PO-26 electrics")]
    [InlineData("Ref WO#26")]
    public async Task TheReferenceIsReadHoweverTheSupplierWroteIt(string reference)
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        await SetReferenceAsync(fixture, "inv-1724", reference);

        var line = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");

        Assert.Equal("WO-0026", line.WorkOrderMatch?.WorkOrderReference);
    }

    [Fact]
    public async Task ANumberOpenOnTwoProjectsIsBrokenByTheBillsOwnSiteOrStays()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-wh-26", WorkOrderBillFixture.Woodhouse, 26, WorkOrderBillFixture.SubcontractorId, 5000m, ("ELE-STD", 5000m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-1724", "WO-0026");

        var unresolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");
        Assert.Null(unresolved.WorkOrderMatch);
        Assert.Contains("more than one project", unresolved.WorkOrderExceptionReason);

        await SetSiteAsync(fixture, "inv-1724", "By France");
        var resolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1724");
        Assert.Equal("wo-bf-26", resolved.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task TheAccountantsFirstCase_ANumberOpenOnTwoProjectsIsBrokenByTheProjectSetOnTheBill()
    {
        // Anything Electrical 1725 (2026-09-09): WO-0001 on Ravenswood AND Woodhouse, no Sites tracking,
        // the accountant set the project on the bill in the portal — that must be the site.
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-wh-01-ae", WorkOrderBillFixture.Woodhouse, 1, WorkOrderBillFixture.SubcontractorId, 5000m, ("ELE-STD", 5000m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-1725", "WO-0001");

        var unresolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1725");
        Assert.Null(unresolved.WorkOrderMatch);
        Assert.Contains("set the project on the bill", unresolved.WorkOrderExceptionReason);

        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == "inv-1725").ToListAsync())
            line.ProjectId = WorkOrderBillFixture.Ravenswood;
        await fixture.Context.SaveChangesAsync();
        var resolved = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1725");
        Assert.Equal("wo-ra-01", resolved.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task TheAccountantsSecondCase_LinesNamingTheirOwnOrdersAreProposedLineByLine()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 2000m, ("INT-PLS", 2000m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 2000m, ("INT-PLB", 2000m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-lg", "Lees Green-001", "Drywall Co Ltd", ("321", 1748m), ("321", 1344m), ("321", 10m));
        await fixture.Context.SaveChangesAsync();
        await DescribeAsync(fixture, "inv-lg:0", "Tiling to WO-0055");
        await DescribeAsync(fixture, "inv-lg:1", "Adhesive per WO-0056");

        var lines = (await fixture.ReadUnallocatedAsync()).Where(line => line.XeroInvoiceId == "inv-lg").OrderBy(line => line.XeroLedgerLineId).ToList();

        Assert.All(lines, line => Assert.Equal(WorkOrderMatchRule.ByLineReference, line.WorkOrderMatch!.Rule));
        Assert.Contains("WO-0055 (2 lines), WO-0056 (1 line)", lines[0].WorkOrderMatch!.Detail);
        Assert.Contains("1 line names no order and is put on WO-0055", lines[0].WorkOrderMatch!.Detail);
        // The proposal is a figure per order off the bill, the same on every line — never a coding of the lines.
        Assert.All(lines, line => Assert.Equal(
            new[] { ("wo-lg-55", 1758m), ("wo-lg-56", 1344m) },
            line.WorkOrderMatch!.ProposedSlices.Select(slice => (slice.WorkOrderId, slice.Net))));
    }

    [Fact]
    public async Task ABillNamingTwoOfTheSuppliersOrdersReachesTheCardOnTheFirstForAHandSplit()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-55", WorkOrderBillFixture.Woodhouse, 55, "sub-dry", 1748m, ("INT-PLS", 1748m));
        WorkOrderBillFixture.AddOrder(fixture.Context, "wo-lg-56", WorkOrderBillFixture.Woodhouse, 56, "sub-dry", 2000m, ("INT-PLB", 2000m));
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-lg", "Lees Green-001", "Drywall Co Ltd", ("321", 3092m));
        await fixture.Context.SaveChangesAsync();
        await SetReferenceAsync(fixture, "inv-lg", "WO-0055 / WO-0056");

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-lg");

        // WO-0055 alone cannot hold £3,092 — the over-value gate is per order, so the bill still
        // reaches the card, where the split puts £1,344 on WO-0056.
        Assert.Null(line.WorkOrderExceptionReason);
        Assert.Equal(("wo-lg-55", WorkOrderMatchRule.ByReference), (line.WorkOrderMatch?.WorkOrderId, line.WorkOrderMatch?.Rule));
        Assert.Contains("names WO-0055 and WO-0056 — split", line.WorkOrderMatch!.Detail);
    }

    [Fact]
    public async Task ASupplierWithOneOpenOrderMatchesOnTheSupplierAlone()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-dry");

        var match = Assert.IsType<WorkOrderBillMatch>(line.WorkOrderMatch);
        Assert.Equal((WorkOrderMatchRule.BySupplier, "wo-wh-01"), (match.Rule, match.WorkOrderId));
        Assert.Contains("only open order", match.Detail);
    }

    [Fact]
    public void AMultiCodeOrderCodesAnAmountProRataToItsLinesToThePenny()
    {
        var weights = new[] { new KeyValuePair<string, decimal>("INT-PLS", 6000m), new KeyValuePair<string, decimal>("INT-PLB", 4000m) };

        var splits = WorkOrderBillRecognition.ProposedSplitsFor(weights, WorkOrderBillFixture.Woodhouse, 1000.01m);

        Assert.Equal(new[] { ("INT-PLS", 600.01m), ("INT-PLB", 400.00m) }, splits.Select(split => (split.CostCenterCode, split.Net)));
        Assert.Equal(1000.01m, splits.Sum(split => split.Net));
    }

    [Fact]
    public async Task ABillThatTakesTheOrderOverItsValueStaysWithTheShortfallOnTheRow()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-big", "78", "Drywall Co Ltd", ("321", 12000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-big");

        Assert.Null(line.WorkOrderMatch);
        Assert.Contains("over its value by £2,000.00", line.WorkOrderExceptionReason);
    }

    [Fact]
    public async Task AnOrderAlreadyInvoicedToItsValueIsNotOpen()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        fixture.Context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity { XeroLineWorkOrderLinkId = "L1", XeroLedgerLineId = "old", WorkOrderId = "wo-ra-01", ProjectId = WorkOrderBillFixture.Ravenswood, Amount = 14940m });
        await fixture.Context.SaveChangesAsync();

        // Ravenswood is fully invoiced, so By France is the supplier's only open order.
        var line = (await fixture.ReadUnallocatedAsync()).First(candidate => candidate.XeroInvoiceId == "inv-1725");

        Assert.Equal("wo-bf-26", line.WorkOrderMatch?.WorkOrderId);
    }

    [Fact]
    public async Task ASupplierOnTheLabourRegistryStaysOnTheCoverRoute()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        fixture.Context.Workers.Add(new WorkerEntity { WorkerId = "w1", Name = "Dave Drywall", SubcontractorId = "sub-dry", IsActive = true });
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-dry", "77", "Drywall Co Ltd", ("321", 1000m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-dry");

        Assert.Null(line.WorkOrderMatch);
        Assert.Contains("labour registry", line.WorkOrderExceptionReason);
        Assert.Equal("w1", line.MatchedWorkerId);
    }

    [Fact]
    public async Task ASupplierWithNoOpenOrderCarriesNeitherAMatchNorAReason()
    {
        var fixture = await WorkOrderBillFixture.CreateAsync();
        WorkOrderBillFixture.AddBill(fixture.Context, "inv-gs", "0056", "Grant & Stone Limited", ("310", 353.33m));
        await fixture.Context.SaveChangesAsync();

        var line = (await fixture.ReadUnallocatedAsync()).Single(candidate => candidate.XeroInvoiceId == "inv-gs");

        Assert.Null(line.WorkOrderMatch);
        Assert.Null(line.WorkOrderExceptionReason);
    }

    private static async Task SetReferenceAsync(WorkOrderBillFixture fixture, string invoiceId, string reference)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.Reference = reference;
        await fixture.Context.SaveChangesAsync();
    }

    private static async Task DescribeAsync(WorkOrderBillFixture fixture, string lineId, string description)
    {
        var line = await fixture.Context.XeroLedgerLines.FirstAsync(candidate => candidate.XeroLedgerLineId == lineId);
        line.Description = description;
        await fixture.Context.SaveChangesAsync();
    }

    private static async Task SetSiteAsync(WorkOrderBillFixture fixture, string invoiceId, string site)
    {
        foreach (var line in await fixture.Context.XeroLedgerLines.Where(line => line.XeroInvoiceId == invoiceId).ToListAsync())
            line.XeroSite = site;
        await fixture.Context.SaveChangesAsync();
    }
}
