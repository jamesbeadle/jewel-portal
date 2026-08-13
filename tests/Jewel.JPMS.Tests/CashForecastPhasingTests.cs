using Jewel.JPMS.Commercial;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Cash Forecast's phasing engine. The one invariant everything else leans on: every
// category's phased cells (months + Later + Undated) sum EXACTLY to the figure that went in,
// so the forecast is the statements spread in time — never a second opinion. The other rules
// pinned here: overdue lands in the current month (never the past, never dropped), no honest
// date means Undated (except invoiced money, which is real and assumed due now), and dated
// flows beyond the visible axis go to Later rather than vanishing.
public sealed class CashForecastPhasingTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 8, 11, 9, 0, 0, TimeSpan.Zero);

    private static ProjectForecastInputs Inputs(
        IReadOnlyList<DatedAmount>? invoices = null,
        decimal futureValuations = 0m,
        DateTimeOffset? firstValuation = null,
        DateTimeOffset? practicalCompletion = null,
        int lagDays = 0,
        DatedAmount? release1 = null,
        DatedAmount? release2 = null,
        decimal bills = 0m,
        decimal wo = 0m,
        decimal drawdown = 0m,
        decimal? monthlyOverride = null) =>
        new("p1",
            invoices ?? Array.Empty<DatedAmount>(),
            futureValuations, firstValuation, practicalCompletion, lagDays,
            release1 ?? new DatedAmount(0m, null),
            release2 ?? new DatedAmount(0m, null),
            bills, wo, drawdown, monthlyOverride);

    // ---- The invariant -------------------------------------------------------------------

    [Fact]
    public void EveryCategoryTotal_equalsItsInput_toThePenny()
    {
        var inputs = Inputs(
            invoices: new[] { new DatedAmount(96_400m, AsOf.AddDays(14)), new DatedAmount(41_200m, AsOf.AddDays(40)) },
            futureValuations: 100_000.01m,                       // awkward pennies on purpose
            firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            practicalCompletion: new DateTimeOffset(2027, 2, 15, 0, 0, 0, TimeSpan.Zero),
            lagDays: 35,
            release1: new DatedAmount(14_200m, new DateTimeOffset(2027, 2, 15, 0, 0, 0, TimeSpan.Zero)),
            release2: new DatedAmount(9_800m, new DateTimeOffset(2027, 8, 15, 0, 0, 0, TimeSpan.Zero)),
            bills: 132_700m, wo: 84_333.33m, drawdown: 66_666.67m);

        var forecast = CashForecastPhasing.Phase(inputs, AsOf, monthCount: 12);

        Assert.Equal(137_600m, forecast.Categories[ForecastCategory.InvoicesOutstanding].Total);
        Assert.Equal(100_000.01m, forecast.Categories[ForecastCategory.FutureValuations].Total);
        Assert.Equal(24_000m, forecast.Categories[ForecastCategory.RetentionReleases].Total);
        Assert.Equal(132_700m, forecast.Categories[ForecastCategory.BillsUnpaid].Total);
        Assert.Equal(84_333.33m, forecast.Categories[ForecastCategory.WorkOrdersToInvoice].Total);
        Assert.Equal(66_666.67m, forecast.Categories[ForecastCategory.DrawdownsToSpend].Total);

        // And therefore the netted whole ties to the statement's completion cashflow.
        Assert.Equal(
            137_600m + 100_000.01m + 24_000m - 132_700m - 84_333.33m - 66_666.67m,
            forecast.CompletionCashflow);
    }

    // ---- Overdue never vanishes ----------------------------------------------------------

    [Fact]
    public void OverdueReceipt_landsInTheCurrentMonth_neverThePast()
    {
        var forecast = CashForecastPhasing.Phase(
            Inputs(invoices: new[] { new DatedAmount(10_000m, AsOf.AddMonths(-3)) }),
            AsOf, monthCount: 6);

        Assert.Equal(10_000m, forecast.Categories[ForecastCategory.InvoicesOutstanding].Months[0]);
    }

    [Fact]
    public void SpreadStartingBeforeToday_isClampedToToday()
    {
        // Valuations "expected" from three months ago (a stale NextExpectedValuationDate) must
        // not phase into the past: the whole spread compresses into the months from now to PC.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 30_000m,
                firstValuation: AsOf.AddMonths(-3),
                practicalCompletion: AsOf.AddMonths(2)),
            AsOf, monthCount: 6);

        var months = forecast.Categories[ForecastCategory.FutureValuations].Months;
        Assert.Equal(30_000m, months.Sum());
        Assert.Equal(10_000m, months[0]);   // three slices: now, +1, +2 (no lag in this test)
        Assert.Equal(10_000m, months[1]);
        Assert.Equal(10_000m, months[2]);
    }

    // ---- No honest date → Undated (or now, where the money is already real) ---------------

    [Fact]
    public void SpreadsWithNoPracticalCompletion_goWholeToUndated()
    {
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 50_000m, wo: 20_000m, drawdown: 12_000m),
            AsOf, monthCount: 6);

        Assert.Equal(50_000m, forecast.Categories[ForecastCategory.FutureValuations].Undated);
        Assert.Equal(20_000m, forecast.Categories[ForecastCategory.WorkOrdersToInvoice].Undated);
        Assert.Equal(12_000m, forecast.Categories[ForecastCategory.DrawdownsToSpend].Undated);
        Assert.Equal(0m, forecast.Categories[ForecastCategory.FutureValuations].Months.Sum());
    }

    [Fact]
    public void UndatedRelease_goesToUndated_butUndatedInvoiceIsAssumedDueNow()
    {
        var forecast = CashForecastPhasing.Phase(
            Inputs(
                invoices: new[] { new DatedAmount(5_000m, null) },
                release2: new DatedAmount(9_800m, null)),
            AsOf, monthCount: 6);

        Assert.Equal(5_000m, forecast.Categories[ForecastCategory.InvoicesOutstanding].Months[0]);
        Assert.Equal(9_800m, forecast.Categories[ForecastCategory.RetentionReleases].Undated);
    }

    // ---- Beyond the axis → Later ----------------------------------------------------------

    [Fact]
    public void DatedBeyondTheVisibleHorizon_goesToLater_notDropped()
    {
        var forecast = CashForecastPhasing.Phase(
            Inputs(release2: new DatedAmount(24_600m, AsOf.AddMonths(20))),
            AsOf, monthCount: 12);

        Assert.Equal(24_600m, forecast.Categories[ForecastCategory.RetentionReleases].Later);
        Assert.Equal(24_600m, forecast.Categories[ForecastCategory.RetentionReleases].Total);
    }

    // ---- Timing rules ---------------------------------------------------------------------

    [Fact]
    public void FutureValuations_receiveOnePaymentLagAfterEachValuationMonth()
    {
        // Two valuation months (Sep, Oct = PC), 35-day lag: Sep 1 + 35d = Oct, Oct 1 + 35d = Nov.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 20_000m,
                firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                practicalCompletion: new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero),
                lagDays: 35),
            AsOf, monthCount: 6);

        var months = forecast.Categories[ForecastCategory.FutureValuations].Months;
        Assert.Equal(0m, months[0]);          // Aug
        Assert.Equal(0m, months[1]);          // Sep — valued, not yet paid
        Assert.Equal(10_000m, months[2]);     // Oct
        Assert.Equal(10_000m, months[3]);     // Nov
    }

    [Fact]
    public void WorkOrders_spreadToCompletion_paidTheMonthAfter()
    {
        // Spread Aug–Oct (PC), paid one month later: Sep, Oct, Nov.
        var forecast = CashForecastPhasing.Phase(
            Inputs(wo: 30_000m,
                practicalCompletion: new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.Zero)),
            AsOf, monthCount: 6);

        var months = forecast.Categories[ForecastCategory.WorkOrdersToInvoice].Months;
        Assert.Equal(0m, months[0]);
        Assert.Equal(10_000m, months[1]);
        Assert.Equal(10_000m, months[2]);
        Assert.Equal(10_000m, months[3]);
    }

    [Fact]
    public void PennyRemainders_foldIntoTheFinalSlice_exactly()
    {
        // £100 over 3 months: 33.33 + 33.33 + 33.34.
        var forecast = CashForecastPhasing.Phase(
            Inputs(drawdown: 100m,
                practicalCompletion: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)),
            AsOf, monthCount: 6);

        var months = forecast.Categories[ForecastCategory.DrawdownsToSpend].Months;
        Assert.Equal(33.33m, months[1]);
        Assert.Equal(33.33m, months[2]);
        Assert.Equal(33.34m, months[3]);
    }

    // ---- The FD's monthly-rate override (2026-08-13) ---------------------------------------
    // "Woodhouse seems to be a lot less than that … Abbott will probably land up being more."
    // With ExpectedMonthlyValuation set, future valuations are claimed at that rate from the
    // next expected valuation until left-to-claim runs out — full slices, then one partial —
    // rather than spread evenly to practical completion.

    [Fact]
    public void MonthlyOverride_claimsAtTheRate_thenOnePartialFinalSlice()
    {
        // £50k at £20k/month from Sep: 20k Sep, 20k Oct, 10k Nov — no PC date needed.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 50_000m,
                firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                monthlyOverride: 20_000m),
            AsOf, monthCount: 6);

        var phased = forecast.Categories[ForecastCategory.FutureValuations];
        Assert.Equal(0m, phased.Months[0]);          // Aug — nothing before the first valuation
        Assert.Equal(20_000m, phased.Months[1]);     // Sep
        Assert.Equal(20_000m, phased.Months[2]);     // Oct
        Assert.Equal(10_000m, phased.Months[3]);     // Nov — the partial tail
        Assert.Equal(0m, phased.Undated);
        Assert.Equal(50_000m, phased.Total);         // the invariant survives the override
    }

    [Fact]
    public void MonthlyOverride_lagsEachSlice_byThePaymentMechanism()
    {
        // Valued Sep and Oct, received 35 days later: Oct and Nov.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 20_000m,
                firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                lagDays: 35,
                monthlyOverride: 10_000m),
            AsOf, monthCount: 6);

        var months = forecast.Categories[ForecastCategory.FutureValuations].Months;
        Assert.Equal(0m, months[1]);                 // Sep — valued, not yet paid
        Assert.Equal(10_000m, months[2]);            // Oct
        Assert.Equal(10_000m, months[3]);            // Nov
    }

    [Fact]
    public void MonthlyOverride_needsNoPracticalCompletion_nothingGoesUndated()
    {
        // The even spread would quarantine this to Undated (no PC date); the rate dates it.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: 30_000m, monthlyOverride: 15_000m),
            AsOf, monthCount: 6);

        var phased = forecast.Categories[ForecastCategory.FutureValuations];
        Assert.Equal(0m, phased.Undated);
        Assert.Equal(15_000m, phased.Months[1]);     // no first-valuation date → next month
        Assert.Equal(15_000m, phased.Months[2]);
        Assert.Equal(30_000m, phased.Total);
    }

    [Fact]
    public void MonthlyOverride_negativeRemainder_landsWholeInTheFirstValuationMonth()
    {
        // Invoices already issued past left-to-claim: nothing to claim at a rate — the
        // negative correction lands whole where the even spread would put it, keeping the
        // tie-back to the statement exact.
        var forecast = CashForecastPhasing.Phase(
            Inputs(futureValuations: -5_000m,
                firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                monthlyOverride: 20_000m),
            AsOf, monthCount: 6);

        var phased = forecast.Categories[ForecastCategory.FutureValuations];
        Assert.Equal(-5_000m, phased.Months[1]);
        Assert.Equal(-5_000m, phased.Total);
    }

    [Fact]
    public void MonthlyOverride_extendsTheHorizon_theProbeSeesTheRateSpread()
    {
        // £120k at £10k/month from Sep 26 runs to Aug 27 — the axis must follow it.
        var projects = new[]
        {
            Inputs(futureValuations: 120_000m,
                firstValuation: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                monthlyOverride: 10_000m)
        };

        Assert.Equal(new DateTime(2027, 8, 1), CashForecastPhasing.HorizonEndFor(projects, AsOf));
    }

    // ---- The horizon probe ----------------------------------------------------------------

    [Fact]
    public void HorizonEnd_isTheLastMonthAnyProjectHasADatedFlowIn()
    {
        var projects = new[]
        {
            Inputs(bills: 1_000m),                                                        // this month
            Inputs(release2: new DatedAmount(9_800m, new DateTimeOffset(2027, 8, 15, 0, 0, 0, TimeSpan.Zero)))
        };

        Assert.Equal(new DateTime(2027, 8, 1), CashForecastPhasing.HorizonEndFor(projects, AsOf));
    }

    [Fact]
    public void HorizonEnd_ignoresUndatedFlows()
    {
        var projects = new[] { Inputs(futureValuations: 50_000m) };   // no PC date → Undated
        Assert.Equal(new DateTime(2026, 8, 1), CashForecastPhasing.HorizonEndFor(projects, AsOf));
    }
}
