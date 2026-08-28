using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// ============================================================================
// Aged receivables — what clients owe the company, aged as Xero's own report
// ages it, but INCLUDING draft sales invoices still being prepared. The mirror
// of the aged payables read (finance meeting 2026-08-03): one row per client,
// the same Current / 1–3 months / Older calendar-month buckets, credit notes
// netting off. Live passthrough: nothing is stored in JPMS, and the read
// deliberately has NO date floor (unlike the ledger's reporting window) so an
// ancient unpaid invoice still ages into "Older" instead of vanishing.
// ============================================================================

/// <summary>
/// Asks the API for every outstanding sales invoice and credit note as Xero holds it right now —
/// ACCREC invoices (DRAFT, SUBMITTED and AUTHORISED) with money still due, plus ACCRECCREDIT
/// credit notes with credit still unapplied. The API caches the Xero read briefly to respect
/// Xero's rate limits; <paramref name="Force"/> bypasses that cache for an explicit user refresh.
/// </summary>
public sealed record GetXeroAgedReceivables(bool Force = false) : IQuery<XeroAgedReceivablesSnapshot>;

/// <summary>
/// What the API saw when it asked Xero. <see cref="IsConfigured"/> is false when the Xero client
/// id and secret app settings are missing (the UI explains rather than erroring); <see cref="Error"/>
/// carries a human-readable failure when Xero itself rejected or failed the call.
/// <see cref="FetchedAtUtc"/> is when Xero was actually read (older than 'now' when the API served
/// its cache); <see cref="Truncated"/> is true when the fetch hit its page cap before exhausting
/// Xero's data — totals are then incomplete and the cap (Xero__MaxPages) needs raising.
/// </summary>
public sealed record XeroAgedReceivablesSnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    bool Truncated,
    IReadOnlyList<XeroReceivableInvoice> Invoices)
{
    public static XeroAgedReceivablesSnapshot NotConfigured() =>
        new(false, null, null, false, Array.Empty<XeroReceivableInvoice>());

    public static XeroAgedReceivablesSnapshot Failed(string error) =>
        new(true, error, null, false, Array.Empty<XeroReceivableInvoice>());
}

/// <summary>
/// One outstanding sales invoice (Xero type ACCREC) or credit note (ACCRECCREDIT).
/// <see cref="AmountDue"/> is what remains outstanding (Xero's AmountDue for invoices,
/// RemainingCredit for credit notes) and is stored POSITIVE even for credit notes —
/// consumers apply the sign via <see cref="AgedReceivablesMaths.SignedAmountDue"/>, the same
/// convention as the payables side. A DRAFT invoice has no payments yet, so its AmountDue is
/// its total — exactly the amount Xero's own aged receivables report is not showing.
/// <see cref="ExpectedPaymentDate"/> is Xero's optional "Expected date" (set on the invoice or
/// the Awaiting Payment list) — when the accountant records one, it is the honest answer to
/// "when will this actually arrive" (retentions, agreed late payment) and the Weekly Cashflow
/// seeds there instead of the due week. Ageing everywhere stays on the due date. Credit notes
/// never carry one.
/// </summary>
public sealed record XeroReceivableInvoice(
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
    DateTime? ExpectedPaymentDate = null)
{
    public bool IsCreditNote => string.Equals(Type, "ACCRECCREDIT", StringComparison.OrdinalIgnoreCase);

    public bool IsDraft => string.Equals(Status, "DRAFT", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Which date an invoice ages from — Xero's own report offers the same choice,
/// defaulting to the due date.</summary>
public enum ReceivablesAgeBasis { DueDate = 0, InvoiceDate = 1 }

/// <summary>
/// The ageing arithmetic, shared by the page and its Excel export so the two can never disagree,
/// and kept out of the UI so it can be unit-tested. Buckets follow Xero's default monthly layout —
/// Current, 1 month, 2 months, 3 months, Older — aged by CALENDAR month (an invoice due any time
/// last month sits in "1 month" for the whole of this month), which is Xero's month-period
/// behaviour and stops invoices creeping between columns mid-month. Mirrors AgedPayablesMaths.
/// </summary>
public static class AgedReceivablesMaths
{
    public const int BucketCount = 5;

    public static readonly IReadOnlyList<string> BucketLabels =
        new[] { "Current", "1 month", "2 months", "3 months", "Older" };

    /// <summary>
    /// Which bucket an invoice sits in as of <paramref name="asOf"/> (today, normally).
    /// 0 = Current (not yet due, or due this calendar month), 1..3 = that many whole calendar
    /// months behind, 4 = older. An invoice with no due date ages from its invoice date instead
    /// (Xero treats a missing due date the same way); with neither date it stays Current —
    /// there is nothing honest to age it from.
    /// </summary>
    public static int BucketFor(XeroReceivableInvoice invoice, DateTime asOf, ReceivablesAgeBasis basis = ReceivablesAgeBasis.DueDate)
    {
        var agesFrom = basis == ReceivablesAgeBasis.InvoiceDate
            ? invoice.Date
            : invoice.DueDate ?? invoice.Date;
        if (agesFrom is not { } date) return 0;

        var monthsBehind = (asOf.Year - date.Year) * 12 + (asOf.Month - date.Month);
        return Math.Clamp(monthsBehind, 0, BucketCount - 1);
    }

    /// <summary>The amount an invoice contributes to the receivables position: invoices add,
    /// credit notes subtract. Amounts arrive positive from Xero for both (RemainingCredit for
    /// credit notes).</summary>
    public static decimal SignedAmountDue(XeroReceivableInvoice invoice) =>
        invoice.IsCreditNote ? -invoice.AmountDue : invoice.AmountDue;

    /// <summary>An invoice is overdue when it sits in any bucket past Current.</summary>
    public static bool IsOverdue(XeroReceivableInvoice invoice, DateTime asOf, ReceivablesAgeBasis basis = ReceivablesAgeBasis.DueDate) =>
        BucketFor(invoice, asOf, basis) > 0;

    /// <summary>
    /// One client's row of the summary: the signed amounts due per bucket, plus the invoices
    /// behind them for the drill-down. <see cref="Total"/> is the sum across buckets.
    /// </summary>
    public sealed record ClientRow(
        string Client,
        IReadOnlyList<decimal> Buckets,
        IReadOnlyList<XeroReceivableInvoice> Invoices)
    {
        public decimal Total => Buckets.Sum();

        public decimal DraftTotal => Invoices.Where(invoice => invoice.IsDraft).Sum(SignedAmountDue);
    }

    /// <summary>
    /// Groups outstanding invoices into one row per client, A–Z like Xero's report, each with its
    /// per-bucket signed totals and its invoices soonest-due first. Invoices with no client name
    /// group under "(no client)" rather than disappearing from the total.
    /// </summary>
    public static IReadOnlyList<ClientRow> SummariseByClient(
        IEnumerable<XeroReceivableInvoice> invoices, DateTime asOf, ReceivablesAgeBasis basis = ReceivablesAgeBasis.DueDate) =>
        invoices
            .GroupBy(
                invoice => string.IsNullOrWhiteSpace(invoice.ContactName) ? "(no client)" : invoice.ContactName!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var buckets = new decimal[BucketCount];
                foreach (var invoice in group)
                    buckets[BucketFor(invoice, asOf, basis)] += SignedAmountDue(invoice);
                return new ClientRow(
                    group.Key,
                    buckets,
                    group.OrderBy(invoice => invoice.DueDate ?? invoice.Date ?? DateTime.MaxValue).ToList());
            })
            .ToList();

    /// <summary>The per-bucket totals across every client row — the summary table's footer.</summary>
    public static IReadOnlyList<decimal> BucketTotals(IReadOnlyList<ClientRow> rows)
    {
        var totals = new decimal[BucketCount];
        foreach (var row in rows)
            for (var bucket = 0; bucket < BucketCount; bucket++)
                totals[bucket] += row.Buckets[bucket];
        return totals;
    }
}
