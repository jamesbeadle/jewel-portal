using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The aged payables ageing arithmetic — Xero's default monthly layout (Current, 1 month,
/// 2 months, 3 months, Older) aged by CALENDAR month, so a bill keeps its column for the whole
/// month rather than creeping mid-month. Drafts age exactly like approved bills: the whole point
/// of the report is that Xero's own version cannot see them.
/// </summary>
public class AgedPayablesMathsTests
{
    private static readonly DateTime AsOf = new(2026, 7, 30);

    private static XeroPayableBill Bill(
        DateTime? due = null, DateTime? date = null, string type = "ACCPAY",
        string status = "AUTHORISED", decimal amountDue = 100m, string? supplier = "Acme") =>
        new("id-" + Guid.NewGuid(), type, "INV-1", null, supplier, date, due, status, amountDue, amountDue, "GBP");

    [Fact]
    public void NotYetDueIsCurrent()
    {
        Assert.Equal(0, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 8, 14)), AsOf));
    }

    [Fact]
    public void DueEarlierThisCalendarMonthIsStillCurrent()
    {
        // Xero's month-period ageing: overdue by days but not by a calendar month yet.
        Assert.Equal(0, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 7, 2)), AsOf));
    }

    [Fact]
    public void EachCalendarMonthBehindMovesOneColumn()
    {
        Assert.Equal(1, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 6, 28)), AsOf));
        Assert.Equal(2, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 5, 1)), AsOf));
        Assert.Equal(3, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 4, 30)), AsOf));
    }

    [Fact]
    public void FourOrMoreMonthsBehindIsOlder()
    {
        Assert.Equal(4, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2026, 3, 31)), AsOf));
        Assert.Equal(4, AgedPayablesMaths.BucketFor(Bill(due: new DateTime(2023, 1, 15)), AsOf));
    }

    [Fact]
    public void MissingDueDateAgesFromInvoiceDate()
    {
        // Drafts fresh from Dext often carry no due date yet; Xero ages those from the
        // invoice date rather than parking them as Current for ever.
        Assert.Equal(2, AgedPayablesMaths.BucketFor(Bill(due: null, date: new DateTime(2026, 5, 20)), AsOf));
    }

    [Fact]
    public void NoDatesAtAllStaysCurrent()
    {
        Assert.Equal(0, AgedPayablesMaths.BucketFor(Bill(due: null, date: null), AsOf));
    }

    [Fact]
    public void InvoiceDateBasisIgnoresTheDueDate()
    {
        var bill = Bill(due: new DateTime(2026, 7, 15), date: new DateTime(2026, 4, 15));
        Assert.Equal(0, AgedPayablesMaths.BucketFor(bill, AsOf, PayablesAgeBasis.DueDate));
        Assert.Equal(3, AgedPayablesMaths.BucketFor(bill, AsOf, PayablesAgeBasis.InvoiceDate));
    }

    [Fact]
    public void CreditNotesSubtract()
    {
        Assert.Equal(100m, AgedPayablesMaths.SignedAmountDue(Bill()));
        Assert.Equal(-40m, AgedPayablesMaths.SignedAmountDue(Bill(type: "ACCPAYCREDIT", amountDue: 40m)));
    }

    [Fact]
    public void DraftBillsAgeLikeApprovedOnes()
    {
        var draft = Bill(due: new DateTime(2026, 6, 10), status: "DRAFT");
        Assert.Equal(1, AgedPayablesMaths.BucketFor(draft, AsOf));
        Assert.True(draft.IsDraft);
    }

    [Fact]
    public void SummaryGroupsBySupplierAlphabetically()
    {
        var rows = AgedPayablesMaths.SummariseBySupplier(new[]
        {
            Bill(supplier: "Zenith Roofing", due: new DateTime(2026, 8, 1)),
            Bill(supplier: "Acme Scaffolding", due: new DateTime(2026, 6, 1), amountDue: 250m),
            Bill(supplier: "acme scaffolding", due: new DateTime(2026, 8, 1), amountDue: 50m)
        }, AsOf);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Acme Scaffolding", rows[0].Supplier);
        Assert.Equal(50m, rows[0].Buckets[0]);   // Current
        Assert.Equal(250m, rows[0].Buckets[1]);  // 1 month
        Assert.Equal(300m, rows[0].Total);
        Assert.Equal("Zenith Roofing", rows[1].Supplier);
    }

    [Fact]
    public void SupplierlessBillsStayInTheTotalRatherThanVanishing()
    {
        var rows = AgedPayablesMaths.SummariseBySupplier(
            new[] { Bill(supplier: null, due: new DateTime(2026, 7, 1), amountDue: 75m) }, AsOf);

        var row = Assert.Single(rows);
        Assert.Equal("(no supplier)", row.Supplier);
        Assert.Equal(75m, row.Total);
    }

    [Fact]
    public void CreditNoteNetsOffItsSuppliersBucket()
    {
        var rows = AgedPayablesMaths.SummariseBySupplier(new[]
        {
            Bill(supplier: "Acme", due: new DateTime(2026, 6, 5), amountDue: 500m),
            Bill(supplier: "Acme", due: new DateTime(2026, 6, 20), type: "ACCPAYCREDIT", amountDue: 200m)
        }, AsOf);

        var row = Assert.Single(rows);
        Assert.Equal(300m, row.Buckets[1]);
        Assert.Equal(300m, row.Total);
    }

    [Fact]
    public void BucketTotalsSumEverySupplier()
    {
        var rows = AgedPayablesMaths.SummariseBySupplier(new[]
        {
            Bill(supplier: "A", due: new DateTime(2026, 8, 1), amountDue: 10m),
            Bill(supplier: "B", due: new DateTime(2026, 8, 2), amountDue: 20m),
            Bill(supplier: "B", due: new DateTime(2026, 2, 1), amountDue: 5m)
        }, AsOf);

        var totals = AgedPayablesMaths.BucketTotals(rows);
        Assert.Equal(30m, totals[0]);
        Assert.Equal(5m, totals[4]);
    }

    [Fact]
    public void DraftTotalIsTheSliceXeroCannotSee()
    {
        var rows = AgedPayablesMaths.SummariseBySupplier(new[]
        {
            Bill(supplier: "Acme", due: new DateTime(2026, 7, 20), amountDue: 900m, status: "DRAFT"),
            Bill(supplier: "Acme", due: new DateTime(2026, 7, 25), amountDue: 100m)
        }, AsOf);

        Assert.Equal(900m, Assert.Single(rows).DraftTotal);
    }
}
