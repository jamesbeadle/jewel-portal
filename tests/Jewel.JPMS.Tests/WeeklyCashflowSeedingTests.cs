using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// How a Xero bill or invoice becomes a grid seed — shared by the page and the connector's
// get_weekly_cashflow_grid, so one reading is pinned here: the due date seeds the entry, Xero's
// Planned/Expected date rides along as the natural week, credit notes carry their sign, the
// detail names the document and its kind, and an exclusion parks a seed without losing it.
public sealed class WeeklyCashflowSeedingTests
{
    private static readonly DateTime Due = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Planned = new(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);

    private static XeroPayableBill Bill(string id, string type = "ACCPAY", string status = "AUTHORISED", DateTime? planned = null) =>
        new(id, type, "INV-0811", "PO-12", " Jewel Property Serve Ltd ", Due.AddDays(-30), Due, status, 500m, 480m, "GBP", planned);

    [Fact]
    public void FromBill_seedsAtTheDueDate_withThePlannedDateAsExpected()
    {
        var seed = WeeklyCashflowSeeding.FromBill(Bill("b1", planned: Planned));

        Assert.Equal(WeeklyCashflowMaths.BillKeyFor("b1"), seed.PlacementKey);
        Assert.Equal(WeeklyCashflowBand.SupplierBills, seed.Band);
        Assert.Equal("Jewel Property Serve Ltd", seed.Label);
        Assert.Equal("INV-0811", seed.Detail);
        Assert.Equal(480m, seed.Amount);
        Assert.Equal(new DateTimeOffset(Due, TimeSpan.Zero), seed.DueOn);
        Assert.Equal(new DateTimeOffset(Planned, TimeSpan.Zero), seed.ExpectedOn);
    }

    [Fact]
    public void FromBill_flagsDraftsAndCreditNotes_andSignsTheCredit()
    {
        var seed = WeeklyCashflowSeeding.FromBill(Bill("cn1", type: "ACCPAYCREDIT", status: "DRAFT"));

        Assert.Equal("INV-0811 · draft · credit note", seed.Detail);
        Assert.Equal(-480m, seed.Amount);
        Assert.Null(seed.ExpectedOn);
    }

    [Fact]
    public void FromInvoice_isTheSalesSideMirror()
    {
        var invoice = new XeroReceivableInvoice("i1", "ACCREC", null, null, null, Due.AddDays(-14), Due, "AUTHORISED", 1_000m, 1_000m, "GBP", Planned);
        var seed = WeeklyCashflowSeeding.FromInvoice(invoice);

        Assert.Equal(WeeklyCashflowMaths.ReceiptKeyFor("i1"), seed.PlacementKey);
        Assert.Equal(WeeklyCashflowBand.ClientReceipts, seed.Band);
        Assert.Equal(WeeklyCashflowSeeding.UnnamedClient, seed.Label);
        Assert.Equal("no number", seed.Detail);
        Assert.Equal(new DateTimeOffset(Planned, TimeSpan.Zero), seed.ExpectedOn);
    }

    [Fact]
    public void Split_parksExcludedSeeds_andKeepsTheRest()
    {
        var seeds = new[] { WeeklyCashflowSeeding.FromBill(Bill("keep")), WeeklyCashflowSeeding.FromBill(Bill("park")) };
        var exclusions = new[] { new WeeklyCashflowExclusion(WeeklyCashflowMaths.BillKeyFor("park"), "fd@jewelbb.co.uk", DateTimeOffset.UtcNow) };

        var (counted, excluded) = WeeklyCashflowSeeding.Split(seeds, exclusions);

        Assert.Equal(new[] { WeeklyCashflowMaths.BillKeyFor("keep") }, counted.Select(seed => seed.PlacementKey));
        Assert.Equal(new[] { WeeklyCashflowMaths.BillKeyFor("park") }, excluded.Select(seed => seed.PlacementKey));
    }
}
