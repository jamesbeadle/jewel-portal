namespace Jewel.JPMS.Commercial;

// ============================================================================
// The Cash Forecast's phasing engine — pure maths, no EF/HTTP, unit-tested
// directly (CashForecastPhasingTests). It takes the figures the statements
// already compute (CashflowMaths / ProjectDrawdown / RetentionSchedule) and
// answers only one new question: WHICH MONTH does each pound move in?
//
// The rules are the interim set from docs/Financial-Reports-and-Nav-Refactor-
// Plan.md §4, awaiting FD/consultant sign-off — change them HERE and the page
// follows; the page owns no phasing arithmetic of its own.
//
// Three honesty rules are structural, not stylistic:
//   * Overdue lands in the current month — never a past month, never dropped.
//   * A flow with no honest date goes to Undated (kept out of the balance),
//     except invoiced money, which is real and assumed due now.
//   * Every category's phased cells sum EXACTLY to the figure that went in
//     (penny remainders fold into the final slice), so the forecast can never
//     disagree with the statements it is spread from.
// ============================================================================

/// <summary>The forecast's row categories, in statement order. The first three are cash in,
/// the last three cash out. Company overheads are deliberately absent — they are a company-level
/// figure the page adds, not a per-project flow.</summary>
public enum ForecastCategory
{
    InvoicesOutstanding,
    FutureValuations,
    RetentionReleases,
    BillsUnpaid,
    WorkOrdersToInvoice,
    DrawdownsToSpend
}

/// <summary>An amount expected on a date — or on no date at all (null), which the bucketing
/// treats per category: quarantined to Undated where honesty allows, assumed current where the
/// money is already real (an issued invoice).</summary>
public sealed record DatedAmount(decimal Amount, DateTimeOffset? ExpectedOn);

/// <summary>One project's inputs, all taken from figures the statements already compute.</summary>
public sealed record ProjectForecastInputs(
    string ProjectId,
    // Cash in: each outstanding valuation invoice at its expected receipt date (the caller
    // applies the contract's payment mechanism); the left-to-claim remainder still to be valued;
    // and the two retention releases at their RetentionSchedule due dates.
    IReadOnlyList<DatedAmount> InvoiceReceipts,
    decimal FutureValuations,
    DateTimeOffset? FirstValuationMonth,      // NextExpectedValuationDate; null → next month
    DateTimeOffset? PracticalCompletionAt,    // anchors every spread; null → spreads go Undated
    int ReceiptLagDays,                       // the contract's payment mechanism, in days
    DatedAmount Release1,
    DatedAmount Release2,
    // Cash out: unpaid bills (no per-bill due dates client-side yet — assumed current month,
    // stated on the page); work-order value still to be invoiced; drawdowns still to spend.
    decimal BillsUnpaid,
    decimal WoLeftToInvoice,
    decimal Drawdown);

/// <summary>One category's phased answer: a cell per month of the visible horizon, a Later
/// bucket for dated flows beyond it, and an Undated bucket for flows with no honest date.
/// <see cref="Total"/> always equals the input figure — the invariant the tests pin.</summary>
public sealed record PhasedCategory(IReadOnlyList<decimal> Months, decimal Later, decimal Undated)
{
    public decimal Total => Months.Sum() + Later + Undated;

    public static PhasedCategory Empty(int monthCount) => new(new decimal[monthCount], 0m, 0m);
}

/// <summary>One project phased across the horizon. Cash in and cash out per month, and the
/// project's completion cashflow (every bucket, dated or not) — the figure that must equal the
/// Project Cashflow tab's Project Completion Cashflow to the penny.</summary>
public sealed record ProjectForecast(
    string ProjectId,
    IReadOnlyDictionary<ForecastCategory, PhasedCategory> Categories)
{
    private static readonly ForecastCategory[] InCategories =
    {
        ForecastCategory.InvoicesOutstanding,
        ForecastCategory.FutureValuations,
        ForecastCategory.RetentionReleases
    };

    private static readonly ForecastCategory[] OutCategories =
    {
        ForecastCategory.BillsUnpaid,
        ForecastCategory.WorkOrdersToInvoice,
        ForecastCategory.DrawdownsToSpend
    };

    public static bool IsCashIn(ForecastCategory category) => InCategories.Contains(category);

    public decimal TotalIn => InCategories.Sum(category => Categories[category].Total);

    public decimal TotalOut => OutCategories.Sum(category => Categories[category].Total);

    /// <summary>Every bucket netted — ties to the statement's Project Completion Cashflow.</summary>
    public decimal CompletionCashflow => TotalIn - TotalOut;
}

public static class CashForecastPhasing
{
    /// <summary>Wide enough to hold any real project's tail (50 years); used to probe the
    /// horizon with the same arithmetic the real phasing uses, so the two cannot drift.</summary>
    private const int ProbeMonths = 600;

    public static DateTime MonthOf(DateTimeOffset date) => new(date.Year, date.Month, 1);

    public static int MonthsBetween(DateTime fromMonth, DateTime toMonth) =>
        (toMonth.Year - fromMonth.Year) * 12 + toMonth.Month - fromMonth.Month;

    /// <summary>The last month any selected project has a dated flow in — the natural end of the
    /// month axis. Computed by actually phasing (against a very wide horizon), so it can never
    /// disagree with what the real phasing would produce.</summary>
    public static DateTime HorizonEndFor(IEnumerable<ProjectForecastInputs> projects, DateTimeOffset asOf)
    {
        var start = MonthOf(asOf);
        var last = 0;
        foreach (var project in projects)
        {
            var forecast = Phase(project, asOf, ProbeMonths);
            foreach (var category in forecast.Categories.Values)
            {
                for (var index = category.Months.Count - 1; index > last; index--)
                {
                    if (category.Months[index] != 0m) { last = index; break; }
                }
            }
        }
        return start.AddMonths(last);
    }

    /// <summary>Phases one project against a month axis starting at <paramref name="asOf"/>'s
    /// month and running <paramref name="monthCount"/> months. Dated flows beyond the axis go to
    /// Later; flows that cannot be dated go to Undated (never silently to a month).</summary>
    public static ProjectForecast Phase(ProjectForecastInputs inputs, DateTimeOffset asOf, int monthCount)
    {
        var start = MonthOf(asOf);
        var pcMonth = inputs.PracticalCompletionAt is { } pc ? MonthOf(pc) : (DateTime?)null;

        var categories = new Dictionary<ForecastCategory, PhasedCategory>
        {
            // Issued money is real: a receipt with no computable date is assumed due now rather
            // than quarantined — hiding an invoiced amount in Undated would understate the near
            // months for the least uncertain money on the page.
            [ForecastCategory.InvoicesOutstanding] =
                Bucket(inputs.InvoiceReceipts, start, monthCount, undatedAllowed: false),

            // The remainder still to be valued: spread one slice per valuation month from the
            // next expected valuation to practical completion, each slice received a payment-
            // mechanism lag after its valuation month.
            [ForecastCategory.FutureValuations] = SpreadWithLag(
                inputs.FutureValuations,
                inputs.FirstValuationMonth is { } first ? MonthOf(first) : start.AddMonths(1),
                pcMonth, inputs.ReceiptLagDays, monthShift: 0, start, monthCount),

            [ForecastCategory.RetentionReleases] =
                Bucket(new[] { inputs.Release1, inputs.Release2 }, start, monthCount, undatedAllowed: true),

            // Interim rule, stated on the page: no per-bill due dates reach the client yet, so
            // unpaid bills are assumed payable now — conservative for the trough, never rosy.
            [ForecastCategory.BillsUnpaid] =
                Bucket(new[] { new DatedAmount(inputs.BillsUnpaid, asOf) }, start, monthCount, undatedAllowed: false),

            // Committed spend still to bill: spread to practical completion, each month's work
            // paid the month after (supplier terms).
            [ForecastCategory.WorkOrdersToInvoice] = SpreadWithLag(
                inputs.WoLeftToInvoice, start, pcMonth, lagDays: 0, monthShift: 1, start, monthCount),

            [ForecastCategory.DrawdownsToSpend] = SpreadWithLag(
                inputs.Drawdown, start, pcMonth, lagDays: 0, monthShift: 1, start, monthCount)
        };

        return new ProjectForecast(inputs.ProjectId, categories);
    }

    /// <summary>Drops dated amounts into month cells. Anything already due lands in the current
    /// month (index 0) — overdue never vanishes and never sits in the past. Dated beyond the
    /// axis → Later. Undated → Undated where allowed, else the current month.</summary>
    private static PhasedCategory Bucket(
        IEnumerable<DatedAmount> amounts, DateTime start, int monthCount, bool undatedAllowed)
    {
        var months = new decimal[monthCount];
        decimal later = 0m, undated = 0m;
        foreach (var item in amounts)
        {
            if (item.Amount == 0m) continue;
            if (item.ExpectedOn is not { } expected)
            {
                if (undatedAllowed) undated += item.Amount; else months[0] += item.Amount;
                continue;
            }
            var index = MonthsBetween(start, MonthOf(expected));
            if (index < 0) index = 0;
            if (index >= monthCount) later += item.Amount;
            else months[index] += item.Amount;
        }
        return new PhasedCategory(months, later, undated);
    }

    /// <summary>Spreads a total evenly, one slice per month from <paramref name="fromMonth"/> to
    /// <paramref name="toMonth"/> inclusive, then places each slice <paramref name="monthShift"/>
    /// months plus <paramref name="lagDays"/> days later. A null end month means the spread has
    /// no honest anchor — the whole total goes to Undated. Penny remainders fold into the final
    /// slice so the slices always sum exactly to the total.</summary>
    private static PhasedCategory SpreadWithLag(
        decimal total, DateTime fromMonth, DateTime? toMonth,
        int lagDays, int monthShift, DateTime start, int monthCount)
    {
        if (total == 0m) return PhasedCategory.Empty(monthCount);
        if (toMonth is not { } end) return new PhasedCategory(new decimal[monthCount], 0m, total);

        var from = fromMonth < start ? start : fromMonth;
        if (end < from) end = from;   // past practical completion: everything left moves now
        var slices = MonthsBetween(from, end) + 1;
        var perSlice = Math.Round(total / slices, 2);

        var dated = new List<DatedAmount>(slices);
        var allocated = 0m;
        for (var slice = 0; slice < slices; slice++)
        {
            var amount = slice == slices - 1 ? total - allocated : perSlice;
            allocated += amount;
            var lands = new DateTimeOffset(from.AddMonths(slice + monthShift), TimeSpan.Zero)
                .AddDays(lagDays);
            dated.Add(new DatedAmount(amount, lands));
        }
        return Bucket(dated, start, monthCount, undatedAllowed: false);
    }
}
