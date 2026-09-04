using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow;

/// <summary>
/// How the aged Xero snapshots become the grid's seeds — one place, so the page and the connector
/// read the same bill the same way. A bill seeds at its due date (or its Xero Planned date), an
/// invoice at its due date (or its Xero Expected date); credit notes carry their sign. An
/// excluded seed never reaches the maths: it is parked, visible and uncounted.
/// </summary>
public static class WeeklyCashflowSeeding
{
    public const string UnnamedSupplier = "(no supplier)";
    public const string UnnamedClient = "(no client)";
    private const string NoNumber = "no number";
    private const string DraftFlag = "draft";
    private const string CreditNoteFlag = "credit note";
    private const string DetailSeparator = " · ";

    public static WeeklyCashflowSeed FromBill(XeroPayableBill bill) => new(
        WeeklyCashflowMaths.BillKeyFor(bill.InvoiceId),
        WeeklyCashflowBand.SupplierBills,
        NameOr(bill.ContactName, UnnamedSupplier),
        Detail(bill.Number ?? bill.Reference, bill.IsDraft, bill.IsCreditNote),
        AgedPayablesMaths.SignedAmountDue(bill),
        AsDate(bill.DueDate ?? bill.Date),
        AsDate(bill.PlannedPaymentDate));

    public static WeeklyCashflowSeed FromInvoice(XeroReceivableInvoice invoice) => new(
        WeeklyCashflowMaths.ReceiptKeyFor(invoice.InvoiceId),
        WeeklyCashflowBand.ClientReceipts,
        NameOr(invoice.ContactName, UnnamedClient),
        Detail(invoice.Number ?? invoice.Reference, invoice.IsDraft, invoice.IsCreditNote),
        AgedReceivablesMaths.SignedAmountDue(invoice),
        AsDate(invoice.DueDate ?? invoice.Date),
        AsDate(invoice.ExpectedPaymentDate));

    /// <summary>Every Xero-fed seed, split into the ones the maths counts and the ones an
    /// exclusion has parked.</summary>
    public static (IReadOnlyList<WeeklyCashflowSeed> Counted, IReadOnlyList<WeeklyCashflowSeed> Excluded) Split(
        IEnumerable<WeeklyCashflowSeed> seeds, IEnumerable<WeeklyCashflowExclusion> exclusions)
    {
        var excludedKeys = exclusions
            .Select(exclusion => exclusion.PlacementKey)
            .ToHashSet(StringComparer.Ordinal);
        var counted = new List<WeeklyCashflowSeed>();
        var excluded = new List<WeeklyCashflowSeed>();
        foreach (var seed in seeds)
        {
            var bucket = excludedKeys.Contains(seed.PlacementKey) ? excluded : counted;
            bucket.Add(seed);
        }
        return (counted, excluded);
    }

    private static string NameOr(string? contactName, string fallback) =>
        string.IsNullOrWhiteSpace(contactName) ? fallback : contactName.Trim();

    // "INV-0811 · draft · credit note" — the document's own number, then what kind of document it is.
    private static string Detail(string? reference, bool isDraft, bool isCreditNote)
    {
        var parts = new List<string> { reference ?? NoNumber };
        if (isDraft) parts.Add(DraftFlag);
        if (isCreditNote) parts.Add(CreditNoteFlag);
        return string.Join(DetailSeparator, parts);
    }

    // Re-kind before wrapping: a DateTime that arrives Kind=Local (offset-carrying JSON, a future
    // serializer change) would make DateTimeOffset(date, TimeSpan.Zero) throw off-UTC.
    private static DateTimeOffset? AsDate(DateTime? date) =>
        date is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value.Date, DateTimeKind.Utc), TimeSpan.Zero) : null;
}
