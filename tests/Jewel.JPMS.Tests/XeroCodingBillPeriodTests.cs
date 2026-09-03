using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The coding run's bill-period rule (2026-09-03, item A of the accountant's spec): a bill's
/// period is what its number / reference STATES where it states one — Adam's "Aug 2026" — and
/// only otherwise a date window. A worker invoicing on the 1st for the previous month must
/// still land in the previous month.
/// </summary>
public sealed class XeroCodingBillPeriodTests
{
    [Theory]
    [InlineData("Aug 2026", 2026, 8)]
    [InlineData("August 2026", 2026, 8)]
    [InlineData("Labour - Sept 2026", 2026, 9)]
    [InlineData("JPMS labour Aug 2026 — Adam Midgley", 2026, 8)]
    [InlineData("INV 08/2026", 2026, 8)]
    [InlineData("2026-08", 2026, 8)]
    [InlineData("Jul '26", 2026, 7)]
    public void StatedMonth_readsTheMonthTheBillNames(string text, int year, int month)
    {
        var stated = RunXeroCodingHandler.StatedMonth(text);
        Assert.NotNull(stated);
        Assert.Equal((year, month), stated!.Value);
    }

    [Theory]
    [InlineData("RB21597565420")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Invoice 2157826885")]
    public void StatedMonth_isNullWhenTheBillNamesNoMonth(string? text)
    {
        Assert.Null(RunXeroCodingHandler.StatedMonth(text));
    }

    [Fact]
    public void BillSummary_authorisedWithNothingPaidIsRecodable_paidIsNot()
    {
        var authorised = Summary("AUTHORISED", paid: 0m, credited: 0m);
        Assert.True(authorised.IsRecodable);

        var partPaid = Summary("AUTHORISED", paid: 500m, credited: 0m);
        Assert.False(partPaid.IsRecodable);
        Assert.Contains("£500.00 paid", partPaid.NotRecodableReason);

        var paid = Summary("PAID", paid: 2560m, credited: 0m);
        Assert.False(paid.IsRecodable);
        Assert.Equal("it is PAID", paid.NotRecodableReason);

        var voided = Summary("VOIDED", paid: 0m, credited: 0m);
        Assert.False(voided.IsRecodable);
    }

    private static XeroBillSummary Summary(string status, decimal paid, decimal credited) =>
        new("id", status, "Aug 2026", null, "Adam Midgley", new DateTime(2026, 8, 25),
            "Inclusive", 3200m, 0m, 3200m, paid, credited, 2560m - paid, 1, "NONE");
}
