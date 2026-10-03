using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Asks the API for the company's cash position as Xero holds it (live passthrough — nothing is
/// stored in JPMS): every bank account's balance plus the sales invoices (Xero type ACCREC —
/// valuation invoices as raised in Xero) that are issued but not yet fully paid. The API caches
/// the Xero read briefly to respect Xero's rate limits; <paramref name="Force"/> bypasses that
/// cache for an explicit user refresh.
/// </summary>
public sealed record GetXeroCashSummary(bool Force = false) : IQuery<XeroCashSummarySnapshot>;

/// <summary>
/// What the API saw when it asked Xero. <see cref="IsConfigured"/> is false when the Xero client
/// id and secret app settings are missing (the UI explains rather than erroring); <see cref="Error"/>
/// carries a human-readable failure when Xero itself rejected or failed the call.
/// <see cref="FetchedAtUtc"/> is when Xero was actually read (older than 'now' when the API served
/// its cache). <see cref="StatementError"/> says why Xero gave no statement balances, when it
/// refused them — the rest of the read still stands.
/// </summary>
public sealed record XeroCashSummarySnapshot(
    bool IsConfigured,
    string? Error,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<XeroBankAccountBalance> BankAccounts,
    IReadOnlyList<XeroOutstandingSalesInvoice> OutstandingInvoices,
    string? StatementError = null)
{
    public static XeroCashSummarySnapshot NotConfigured() =>
        new(false, null, null, Array.Empty<XeroBankAccountBalance>(), Array.Empty<XeroOutstandingSalesInvoice>());

    public static XeroCashSummarySnapshot Failed(string error) =>
        new(true, error, null, Array.Empty<XeroBankAccountBalance>(), Array.Empty<XeroOutstandingSalesInvoice>());

    /// <summary>Every account's statement balance, or its Xero balance where none is known yet.</summary>
    public decimal TotalCash => BankAccounts.Sum(account => account.CashInBank);

    /// <summary>Each account with the newer of its own statement balance and the one keyed for it.</summary>
    public XeroCashSummarySnapshot WithKeyedStatements(IReadOnlyList<KeyedBankStatementBalance> keyed)
    {
        var keyedByAccount = keyed.ToDictionary(balance => balance.AccountId, StringComparer.Ordinal);
        var accounts = BankAccounts.Select(account => account.WithKeyedStatement(keyedByAccount)).ToList();
        return this with { BankAccounts = accounts };
    }

    public decimal TotalOutstanding => OutstandingInvoices.Sum(invoice => invoice.AmountDue);
}

/// <summary>
/// One sales invoice (Xero type ACCREC) that is authorised in Xero with money still due —
/// issued to the client but not yet fully paid. Amounts are in the invoice currency
/// (<see cref="CurrencyCode"/>, normally GBP); <see cref="AmountDue"/> is what remains
/// outstanding after any part payments and credits.
/// </summary>
public sealed record XeroOutstandingSalesInvoice(
    string InvoiceId,
    string? Number,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    DateTime? DueDate,
    decimal Total,
    decimal AmountDue,
    string? CurrencyCode);
