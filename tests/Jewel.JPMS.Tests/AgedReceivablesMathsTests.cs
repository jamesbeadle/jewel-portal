using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The aged receivables ageing arithmetic — the sales-side mirror of AgedPayablesMathsTests:
/// Xero's default monthly layout (Current, 1 month, 2 months, 3 months, Older) aged by CALENDAR
/// month, so an invoice keeps its column for the whole month rather than creeping mid-month.
/// Drafts age exactly like authorised invoices: the whole point of the report is that Xero's
/// own version cannot see them.
/// </summary>
public class AgedReceivablesMathsTests
{
    private static readonly DateTime AsOf = new(2026, 7, 30);

    private static XeroReceivableInvoice Invoice(
        DateTime? due = null, DateTime? date = null, string type = "ACCREC",
        string status = "AUTHORISED", decimal amountDue = 100m, string? client = "Acme") =>
        new("id-" + Guid.NewGuid(), type, "INV-1", null, client, date, due, status, amountDue, amountDue, "GBP");

    [Fact]
    public void NotYetDueIsCurrent()
    {
        Assert.Equal(0, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 8, 14)), AsOf));
    }

    [Fact]
    public void DueEarlierThisCalendarMonthIsStillCurrent()
    {
        // Xero's month-period ageing: overdue by days but not by a calendar month yet.
        Assert.Equal(0, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 7, 2)), AsOf));
    }

    [Fact]
    public void EachCalendarMonthBehindMovesOneColumn()
    {
        Assert.Equal(1, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 6, 28)), AsOf));
        Assert.Equal(2, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 5, 1)), AsOf));
        Assert.Equal(3, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 4, 30)), AsOf));
    }

    [Fact]
    public void FourOrMoreMonthsBehindIsOlder()
    {
        Assert.Equal(4, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2026, 3, 31)), AsOf));
        Assert.Equal(4, AgedReceivablesMaths.BucketFor(Invoice(due: new DateTime(2023, 1, 15)), AsOf));
    }

    [Fact]
    public void MissingDueDateAgesFromInvoiceDate()
    {
        Assert.Equal(2, AgedReceivablesMaths.BucketFor(Invoice(due: null, date: new DateTime(2026, 5, 20)), AsOf));
    }

    [Fact]
    public void NoDatesAtAllStaysCurrent()
    {
        Assert.Equal(0, AgedReceivablesMaths.BucketFor(Invoice(due: null, date: null), AsOf));
    }

    [Fact]
    public void InvoiceDateBasisIgnoresTheDueDate()
    {
        var invoice = Invoice(due: new DateTime(2026, 7, 15), date: new DateTime(2026, 4, 15));
        Assert.Equal(0, AgedReceivablesMaths.BucketFor(invoice, AsOf, ReceivablesAgeBasis.DueDate));
        Assert.Equal(3, AgedReceivablesMaths.BucketFor(invoice, AsOf, ReceivablesAgeBasis.InvoiceDate));
    }

    [Fact]
    public void CreditNotesSubtract()
    {
        Assert.Equal(100m, AgedReceivablesMaths.SignedAmountDue(Invoice()));
        Assert.Equal(-40m, AgedReceivablesMaths.SignedAmountDue(Invoice(type: "ACCRECCREDIT", amountDue: 40m)));
    }

    [Fact]
    public void DraftInvoicesAgeLikeAuthorisedOnes()
    {
        var draft = Invoice(due: new DateTime(2026, 6, 10), status: "DRAFT");
        Assert.Equal(1, AgedReceivablesMaths.BucketFor(draft, AsOf));
        Assert.True(draft.IsDraft);
    }

    [Fact]
    public void SummaryGroupsByClientAlphabetically()
    {
        var rows = AgedReceivablesMaths.SummariseByClient(new[]
        {
            Invoice(client: "Zenith Developments", due: new DateTime(2026, 8, 1)),
            Invoice(client: "Acme Estates", due: new DateTime(2026, 6, 1), amountDue: 250m),
            Invoice(client: "acme estates", due: new DateTime(2026, 8, 1), amountDue: 50m)
        }, AsOf);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Acme Estates", rows[0].Client);
        Assert.Equal(50m, rows[0].Buckets[0]);   // Current
        Assert.Equal(250m, rows[0].Buckets[1]);  // 1 month
        Assert.Equal(300m, rows[0].Total);
        Assert.Equal("Zenith Developments", rows[1].Client);
    }

    [Fact]
    public void ClientlessInvoicesStayInTheTotalRatherThanVanishing()
    {
        var rows = AgedReceivablesMaths.SummariseByClient(
            new[] { Invoice(client: null, due: new DateTime(2026, 7, 1), amountDue: 75m) }, AsOf);

        var row = Assert.Single(rows);
        Assert.Equal("(no client)", row.Client);
        Assert.Equal(75m, row.Total);
    }

    [Fact]
    public void CreditNoteNetsOffItsClientsBucket()
    {
        var rows = AgedReceivablesMaths.SummariseByClient(new[]
        {
            Invoice(client: "Acme", due: new DateTime(2026, 6, 5), amountDue: 500m),
            Invoice(client: "Acme", due: new DateTime(2026, 6, 20), type: "ACCRECCREDIT", amountDue: 200m)
        }, AsOf);

        var row = Assert.Single(rows);
        Assert.Equal(300m, row.Buckets[1]);
        Assert.Equal(300m, row.Total);
    }

    [Fact]
    public void BucketTotalsSumEveryClient()
    {
        var rows = AgedReceivablesMaths.SummariseByClient(new[]
        {
            Invoice(client: "A", due: new DateTime(2026, 8, 1), amountDue: 10m),
            Invoice(client: "B", due: new DateTime(2026, 8, 2), amountDue: 20m),
            Invoice(client: "B", due: new DateTime(2026, 2, 1), amountDue: 5m)
        }, AsOf);

        var totals = AgedReceivablesMaths.BucketTotals(rows);
        Assert.Equal(30m, totals[0]);
        Assert.Equal(5m, totals[4]);
    }

    [Fact]
    public void DraftTotalIsTheSliceXeroCannotSee()
    {
        var rows = AgedReceivablesMaths.SummariseByClient(new[]
        {
            Invoice(client: "Acme", due: new DateTime(2026, 7, 20), amountDue: 900m, status: "DRAFT"),
            Invoice(client: "Acme", due: new DateTime(2026, 7, 25), amountDue: 100m)
        }, AsOf);

        Assert.Equal(900m, Assert.Single(rows).DraftTotal);
    }
}
