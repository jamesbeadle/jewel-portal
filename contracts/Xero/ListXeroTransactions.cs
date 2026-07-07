using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Asks the API for the current transactions held in Xero (live passthrough — nothing is stored in
/// JPMS yet). The first cut lists purchase invoices (Xero type ACCPAY, i.e. supplier bills) because
/// cost-of-sales reconciliation is the goal, but the snapshot shape is deliberately generic so other
/// Xero record types (sales invoices, bank transactions, credit notes) can join it later.
/// </summary>
public sealed record ListXeroTransactions : IQuery<XeroTransactionsSnapshot>;

/// <summary>
/// What the API saw when it asked Xero. <see cref="IsConfigured"/> is false when the Xero client id
/// and secret app settings are missing (the UI explains rather than erroring); <see cref="Error"/>
/// carries a human-readable failure when Xero itself rejected or failed the call.
/// </summary>
public sealed record XeroTransactionsSnapshot(
    bool IsConfigured,
    string? Error,
    IReadOnlyList<XeroTransaction> Transactions)
{
    public static XeroTransactionsSnapshot NotConfigured() =>
        new(false, null, Array.Empty<XeroTransaction>());

    public static XeroTransactionsSnapshot Failed(string error) =>
        new(true, error, Array.Empty<XeroTransaction>());
}

/// <summary>
/// One transaction as Xero holds it. Amounts are in the invoice currency (<see cref="CurrencyCode"/>,
/// normally GBP). <see cref="Type"/> is Xero's own code: ACCPAY = purchase invoice (supplier bill),
/// ACCREC = sales invoice.
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
    string? CurrencyCode);
