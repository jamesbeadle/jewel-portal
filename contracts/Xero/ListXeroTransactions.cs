using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Asks the API for the current transactions held in Xero (live passthrough — nothing is stored in
/// JPMS yet). Lists purchase invoices (Xero type ACCPAY, i.e. supplier bills) from the configured
/// start date (default 1 Jan 2023) with their line items, because cost-of-sales reconciliation needs
/// the per-line site and cost-code tracking. The API caches the Xero read briefly to respect Xero's
/// rate limits; <paramref name="Force"/> bypasses that cache for an explicit user refresh.
/// </summary>
public sealed record ListXeroTransactions(bool Force = false) : IQuery<XeroTransactionsSnapshot>;

/// <summary>
/// What the API saw when it asked Xero. <see cref="IsConfigured"/> is false when the Xero client id
/// and secret app settings are missing (the UI explains rather than erroring); <see cref="Error"/>
/// carries a human-readable failure when Xero itself rejected or failed the call.
/// <see cref="FromDate"/> is the start of the requested window and <see cref="FetchedAtUtc"/> is
/// when Xero was actually read (older than 'now' when the API served its cache).
/// <see cref="Truncated"/> is true when the fetch hit its page cap before exhausting Xero's data —
/// totals are then incomplete and the cap (Xero__MaxPages) needs raising.
/// </summary>
public sealed record XeroTransactionsSnapshot(
    bool IsConfigured,
    string? Error,
    DateTime? FromDate,
    DateTimeOffset? FetchedAtUtc,
    bool Truncated,
    IReadOnlyList<XeroTransaction> Transactions)
{
    public static XeroTransactionsSnapshot NotConfigured() =>
        new(false, null, null, null, false, Array.Empty<XeroTransaction>());

    public static XeroTransactionsSnapshot Failed(string error) =>
        new(true, error, null, null, false, Array.Empty<XeroTransaction>());
}

/// <summary>
/// One transaction as Xero holds it. Amounts are in the invoice currency (<see cref="CurrencyCode"/>,
/// normally GBP) and are stored positive even for credit notes — consumers apply the sign from
/// <see cref="Type"/>: ACCPAY = purchase invoice (supplier bill), ACCPAYCREDIT = supplier credit
/// note (reduces spend), ACCREC = sales invoice.
/// </summary>
public sealed record XeroTransaction(
    string TransactionId,
    string Type,
    string? Number,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    DateTime? DueDate,
    string Status,
    decimal SubTotal,
    decimal TotalTax,
    decimal Total,
    decimal AmountDue,
    decimal AmountPaid,
    string? CurrencyCode,
    IReadOnlyList<XeroTransactionLine> Lines);

/// <summary>
/// One invoice line. <see cref="Site"/> and <see cref="CostCode"/> come from Xero's tracking
/// categories (named "Site" and "Cost code" in this organisation; names configurable on the API).
/// <see cref="AccountCode"/>/<see cref="AccountName"/> are the ledger account the line posts to;
/// the name is filled from the chart of accounts when the connection's scopes allow it.
/// <see cref="LineAmount"/> is always the net (pre-VAT) amount — the API normalises VAT-inclusive
/// invoices (Xero LineAmountTypes = Inclusive) by deducting each line's tax — and is the figure the
/// site × cost-code split uses.
/// </summary>
public sealed record XeroTransactionLine(
    string? Description,
    decimal Quantity,
    decimal UnitAmount,
    decimal LineAmount,
    decimal TaxAmount,
    string? AccountCode,
    string? AccountName,
    string? Site,
    string? CostCode);
