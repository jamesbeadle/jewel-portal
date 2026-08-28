using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// ============================================================================
// Aged payables — what the company owes suppliers, aged as Xero's own report
// ages it, but INCLUDING draft bills. The accounting procedure leaves purchase
// invoices in DRAFT until they are coded through the portal, and Xero's aged
// payables report only counts approved bills — so the report the finance team
// actually needs (everything owed, coded or not) can only live here. Live
// passthrough: nothing is stored in JPMS, and the read deliberately has NO
// date floor (unlike the ledger's reporting window) so an ancient unpaid bill
// still ages into "Older" instead of vanishing.
// ============================================================================

/// <summary>
/// Asks the API for every outstanding supplier bill and credit note as Xero holds it right now —
/// ACCPAY bills (DRAFT, SUBMITTED and AUTHORISED) with money still due, plus ACCPAYCREDIT credit
/// notes with credit still unapplied. The API caches the Xero read briefly to respect Xero's rate
/// limits; <paramref name="Force"/> bypasses that cache for an explicit user refresh.
/// </summary>
public sealed record GetXeroAgedPayables(bool Force = false) : IQuery<XeroAgedPayablesSnapshot>;

/// <summary>
/// What the API saw when it asked Xero. <see cref="IsConfigured"/> is false when the Xero client
/// id and secret app settings are missing (the UI explains rather than erroring); <see cref="Error"/>
/// carries a human-readable failure when Xero itself rejected or failed the call.
/// <see cref="FetchedAtUtc"/> is when Xero was actually read (older than 'now' when the API served
/// its cache); <see cref="Truncated"/> is true when the fetch hit its page cap before exhausting
/// Xero's data — totals are then incomplete and the cap (Xero__MaxPages) needs raising.
/// </summary>
public sealed record XeroAgedPayablesSnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    bool Truncated,
    IReadOnlyList<XeroPayableBill> Bills)
{
    public static XeroAgedPayablesSnapshot NotConfigured() =>
        new(false, null, null, false, Array.Empty<XeroPayableBill>());

    public static XeroAgedPayablesSnapshot Failed(string error) =>
        new(true, error, null, false, Array.Empty<XeroPayableBill>());
}

/// <summary>
/// One outstanding supplier bill (Xero type ACCPAY) or credit note (ACCPAYCREDIT).
/// <see cref="AmountDue"/> is what remains outstanding (Xero's AmountDue for bills,
/// RemainingCredit for credit notes) and is stored POSITIVE even for credit notes —
/// consumers apply the sign via <see cref="AgedPayablesMaths.SignedAmountDue"/>, the same
/// convention as the stored ledger. A DRAFT bill has no payments yet, so its AmountDue is
/// its total — exactly the amount Xero's own aged payables report is not showing.
/// <see cref="PlannedPaymentDate"/> is Xero's optional "Planned date" (the Awaiting Payment
/// list's planning column) — when set, the Weekly Cashflow seeds the bill there instead of
/// its due week, mirroring the receivables side's Expected date. Ageing stays on the due
/// date. Credit notes never carry one.
/// </summary>
public sealed record XeroPayableBill(
    string InvoiceId,
    string Type,
    string? Number,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    DateTime? DueDate,
    string Status,
    decimal Total,
    decimal AmountDue,
    string? CurrencyCode,
    DateTime? PlannedPaymentDate = null)
{
    public bool IsCreditNote => string.Equals(Type, "ACCPAYCREDIT", StringComparison.OrdinalIgnoreCase);

    public bool IsDraft => string.Equals(Status, "DRAFT", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Which date a bill ages from — Xero's own report offers the same choice, defaulting
/// to the due date.</summary>
public enum PayablesAgeBasis { DueDate = 0, InvoiceDate = 1 }

/// <summary>
/// The ageing arithmetic, shared by the page and its Excel export so the two can never disagree,
/// and kept out of the UI so it can be unit-tested. Buckets follow Xero's default monthly layout —
/// Current, 1 month, 2 months, 3 months, Older — aged by CALENDAR month (a bill due any time last
/// month sits in "1 month" for the whole of this month), which is Xero's month-period behaviour
/// and stops bills creeping between columns mid-month.
/// </summary>
public static class AgedPayablesMaths
{
    public const int BucketCount = 5;

    public static readonly IReadOnlyList<string> BucketLabels =
        new[] { "Current", "1 month", "2 months", "3 months", "Older" };

    /// <summary>
    /// Which bucket a bill sits in as of <paramref name="asOf"/> (today, normally).
    /// 0 = Current (not yet due, or due this calendar month), 1..3 = that many whole calendar
    /// months behind, 4 = older. A bill with no due date ages from its invoice date instead
    /// (Xero treats a missing due date the same way); with neither date it stays Current —
    /// there is nothing honest to age it from.
    /// </summary>
    public static int BucketFor(XeroPayableBill bill, DateTime asOf, PayablesAgeBasis basis = PayablesAgeBasis.DueDate)
    {
        var agesFrom = basis == PayablesAgeBasis.InvoiceDate
            ? bill.Date
            : bill.DueDate ?? bill.Date;
        if (agesFrom is not { } date) return 0;

        var monthsBehind = (asOf.Year - date.Year) * 12 + (asOf.Month - date.Month);
        return Math.Clamp(monthsBehind, 0, BucketCount - 1);
    }

    /// <summary>The amount a bill contributes to the payables position: bills add, credit notes
    /// subtract. Amounts arrive positive from Xero for both (RemainingCredit for credit notes).</summary>
    public static decimal SignedAmountDue(XeroPayableBill bill) =>
        bill.IsCreditNote ? -bill.AmountDue : bill.AmountDue;

    /// <summary>A bill is overdue when it sits in any bucket past Current.</summary>
    public static bool IsOverdue(XeroPayableBill bill, DateTime asOf, PayablesAgeBasis basis = PayablesAgeBasis.DueDate) =>
        BucketFor(bill, asOf, basis) > 0;

    /// <summary>
    /// One supplier's row of the summary: the signed amounts due per bucket, plus the bills
    /// behind them for the drill-down. <see cref="Total"/> is the sum across buckets.
    /// </summary>
    public sealed record SupplierRow(
        string Supplier,
        IReadOnlyList<decimal> Buckets,
        IReadOnlyList<XeroPayableBill> Bills)
    {
        public decimal Total => Buckets.Sum();

        public decimal DraftTotal => Bills.Where(bill => bill.IsDraft).Sum(SignedAmountDue);
    }

    /// <summary>
    /// Groups outstanding bills into one row per supplier, A–Z like Xero's report, each with its
    /// per-bucket signed totals and its bills soonest-due first. Bills with no supplier name group
    /// under "(no supplier)" rather than disappearing from the total.
    /// </summary>
    public static IReadOnlyList<SupplierRow> SummariseBySupplier(
        IEnumerable<XeroPayableBill> bills, DateTime asOf, PayablesAgeBasis basis = PayablesAgeBasis.DueDate) =>
        bills
            .GroupBy(
                bill => string.IsNullOrWhiteSpace(bill.ContactName) ? "(no supplier)" : bill.ContactName!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var buckets = new decimal[BucketCount];
                foreach (var bill in group)
                    buckets[BucketFor(bill, asOf, basis)] += SignedAmountDue(bill);
                return new SupplierRow(
                    group.Key,
                    buckets,
                    group.OrderBy(bill => bill.DueDate ?? bill.Date ?? DateTime.MaxValue).ToList());
            })
            .ToList();

    /// <summary>The per-bucket totals across every supplier row — the summary table's footer.</summary>
    public static IReadOnlyList<decimal> BucketTotals(IReadOnlyList<SupplierRow> rows)
    {
        var totals = new decimal[BucketCount];
        foreach (var row in rows)
            for (var bucket = 0; bucket < BucketCount; bucket++)
                totals[bucket] += row.Buckets[bucket];
        return totals;
    }
}
