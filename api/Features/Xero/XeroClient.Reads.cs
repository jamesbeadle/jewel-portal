using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroTransactionsSnapshot.NotConfigured();

        await _snapshotLock.WaitAsync(ct);
        try
        {
            if (!force && CachedSnapshotIsFresh)
                return _cachedSnapshot!;

            var snapshot = await FetchSnapshotAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict good data,
            // but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedSnapshot = snapshot;
                _cachedSnapshotAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _snapshotLock.Release();
        }
    }

    private bool CachedSnapshotIsFresh =>
        _cachedSnapshot is not null
        && DateTimeOffset.UtcNow < _cachedSnapshotAt.AddMinutes(_options.CacheMinutes);

    // -- cash summary: bank balances + outstanding sales invoices ----------------------

    public async Task<XeroCashSummarySnapshot> GetCashSummaryAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroCashSummarySnapshot.NotConfigured();

        await _cashSummaryLock.WaitAsync(ct);
        try
        {
            if (!force && CachedCashSummaryIsFresh)
                return _cachedCashSummary!;

            var snapshot = await FetchCashSummaryAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict
            // good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedCashSummary = snapshot;
                _cachedCashSummaryAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _cashSummaryLock.Release();
        }
    }

    private bool CachedCashSummaryIsFresh =>
        _cachedCashSummary is not null
        && DateTimeOffset.UtcNow < _cachedCashSummaryAt.AddMinutes(_options.CacheMinutes);

    // -- aged payables: outstanding supplier bills, drafts included ---------------------

    public async Task<XeroAgedPayablesSnapshot> GetAgedPayablesAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroAgedPayablesSnapshot.NotConfigured();

        await _agedPayablesLock.WaitAsync(ct);
        try
        {
            if (!force && CachedAgedPayablesAreFresh)
                return _cachedAgedPayables!;

            var snapshot = await FetchAgedPayablesAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict
            // good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedAgedPayables = snapshot;
                _cachedAgedPayablesAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _agedPayablesLock.Release();
        }
    }

    private bool CachedAgedPayablesAreFresh =>
        _cachedAgedPayables is not null
        && DateTimeOffset.UtcNow < _cachedAgedPayablesAt.AddMinutes(_options.CacheMinutes);

    // -- aged receivables: outstanding sales invoices, drafts included ------------------

    public async Task<XeroAgedReceivablesSnapshot> GetAgedReceivablesAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroAgedReceivablesSnapshot.NotConfigured();

        await _agedReceivablesLock.WaitAsync(ct);
        try
        {
            if (!force && CachedAgedReceivablesAreFresh)
                return _cachedAgedReceivables!;

            var snapshot = await FetchAgedReceivablesAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict
            // good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedAgedReceivables = snapshot;
                _cachedAgedReceivablesAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _agedReceivablesLock.Release();
        }
    }

    private bool CachedAgedReceivablesAreFresh =>
        _cachedAgedReceivables is not null
        && DateTimeOffset.UtcNow < _cachedAgedReceivablesAt.AddMinutes(_options.CacheMinutes);

    // -- suppliers: the contact list behind the directory's "Import from Xero" ----------


    /// <summary>
    /// Authorised sales invoices (ACCREC) with money still due, ordered soonest-due first.
    /// Deliberately NOT summaryOnly: Xero's lightweight mode doesn't accept the where/order
    /// parameters (the combination is an HTTP 400), and the full shape is the same paged read
    /// the purchase-side sync already uses — the line items it carries are simply ignored.
    /// AmountDue is filtered portal-side because it's a calculated field Xero's where clause
    /// doesn't index (part-paid invoices stay AUTHORISED, fully paid ones become PAID, so the
    /// filter rarely removes anything).
    /// </summary>
    private async Task<IReadOnlyList<XeroOutstandingSalesInvoice>> FetchOutstandingSalesInvoicesAsync(
        string token, CancellationToken ct)
    {
        var invoices = new List<XeroOutstandingSalesInvoice>();
        var where = "Type==\"ACCREC\" AND Status==\"AUTHORISED\"";

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            var url = $"{InvoicesUrl}?page={page}"
                      + $"&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString("DueDate ASC")}";
            using var doc = await GetJsonAsync(token, url, "sales invoices", ct);

            if (!doc.RootElement.TryGetProperty("Invoices", out var items) || items.ValueKind != JsonValueKind.Array)
                break;

            foreach (var item in items.EnumerateArray())
            {
                var amountDue = DecimalOf(item, "AmountDue");
                if (amountDue == 0m) continue;
                invoices.Add(new XeroOutstandingSalesInvoice(
                    InvoiceId: StringOf(item, "InvoiceID") ?? Guid.NewGuid().ToString(),
                    Number: StringOf(item, "InvoiceNumber"),
                    Reference: StringOf(item, "Reference"),
                    ContactName: item.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
                    Date: DateOf(item, "DateString", "Date"),
                    DueDate: DateOf(item, "DueDateString", "DueDate"),
                    Total: DecimalOf(item, "Total"),
                    AmountDue: amountDue,
                    CurrencyCode: StringOf(item, "CurrencyCode")));
            }

            if (items.GetArrayLength() < PageSize) break; // Short page — no more to fetch.
        }

        return invoices;
    }

    private async Task<XeroAgedPayablesSnapshot> FetchAgedPayablesAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroAgedPayablesSnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            var bills = new List<XeroPayableBill>();
            // Bills first, then supplier credit notes so unapplied credit nets off the position —
            // the same pairing the ledger sync uses. Statuses are filtered in the where clause
            // (PAID and VOIDED/DELETED never belong on a payables report); AmountDue is filtered
            // portal-side because it's a calculated field Xero's where clause doesn't index.
            var truncated = await FetchOutstandingPayablesAsync(
                token, InvoicesUrl, "Invoices", "ACCPAY", "DueDate ASC", bills, ct);
            truncated |= await FetchOutstandingPayablesAsync(
                token, CreditNotesUrl, "CreditNotes", "ACCPAYCREDIT", "Date DESC", bills, ct);

            return new XeroAgedPayablesSnapshot(true, null, DateTimeOffset.UtcNow, truncated, bills);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroAgedPayablesSnapshot.Failed(callFailure.Message);
        }
    }

    /// <summary>
    /// Pages through one collection of outstanding purchase-side documents into
    /// <paramref name="into"/>; true = page cap hit with data left. DRAFT and SUBMITTED are
    /// requested alongside AUTHORISED — the whole point of the report is the drafts Dext has
    /// published that are still being coded, which Xero's own aged payables cannot show.
    /// Deliberately NOT summaryOnly (Xero's lightweight mode rejects where/order with an HTTP
    /// 400 — see the sales-side read); the line items on the full shape are simply ignored.
    /// </summary>
    private async Task<bool> FetchOutstandingPayablesAsync(
        string token, string baseUrl, string collectionProperty, string xeroType, string order,
        List<XeroPayableBill> into, CancellationToken ct)
    {
        var where = $"Type==\"{xeroType}\" AND "
                    + "(Status==\"DRAFT\" OR Status==\"SUBMITTED\" OR Status==\"AUTHORISED\")";

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            var url = $"{baseUrl}?page={page}"
                      + $"&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString(order)}";
            using var doc = await GetJsonAsync(token, url, collectionProperty.ToLowerInvariant(), ct);

            if (!doc.RootElement.TryGetProperty(collectionProperty, out var items)
                || items.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in items.EnumerateArray())
            {
                // AmountDue for bills, RemainingCredit for credit notes — both mean "still
                // outstanding". Zero means settled (or an empty draft): nothing to age.
                var amountDue = item.TryGetProperty("AmountDue", out _)
                    ? DecimalOf(item, "AmountDue")
                    : DecimalOf(item, "RemainingCredit");
                if (amountDue == 0m) continue;

                into.Add(new XeroPayableBill(
                    InvoiceId: StringOf(item, "InvoiceID") ?? StringOf(item, "CreditNoteID") ?? Guid.NewGuid().ToString(),
                    Type: StringOf(item, "Type") ?? xeroType,
                    Number: StringOf(item, "InvoiceNumber") ?? StringOf(item, "CreditNoteNumber"),
                    Reference: StringOf(item, "Reference"),
                    ContactName: item.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
                    Date: DateOf(item, "DateString", "Date"),
                    DueDate: DateOf(item, "DueDateString", "DueDate"),
                    Status: StringOf(item, "Status") ?? "UNKNOWN",
                    Total: DecimalOf(item, "Total"),
                    AmountDue: amountDue,
                    CurrencyCode: StringOf(item, "CurrencyCode"),
                    // Xero's "Planned date" (Awaiting Payment planning column) — bills only,
                    // absent on credit notes, so DateOf simply returns null there.
                    PlannedPaymentDate: DateOf(item, "PlannedPaymentDateString", "PlannedPaymentDate")));
            }

            if (items.GetArrayLength() < PageSize) return false; // Short page — no more to fetch.
        }
        return true;
    }

    private async Task<XeroAgedReceivablesSnapshot> FetchAgedReceivablesAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroAgedReceivablesSnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            var invoices = new List<XeroReceivableInvoice>();
            // Invoices first, then client credit notes so unapplied credit nets off the position —
            // mirroring the payables read. Statuses are filtered in the where clause (PAID and
            // VOIDED/DELETED never belong on a receivables report); AmountDue is filtered
            // portal-side because it's a calculated field Xero's where clause doesn't index.
            var truncated = await FetchOutstandingReceivablesAsync(
                token, InvoicesUrl, "Invoices", "ACCREC", "DueDate ASC", invoices, ct);
            truncated |= await FetchOutstandingReceivablesAsync(
                token, CreditNotesUrl, "CreditNotes", "ACCRECCREDIT", "Date DESC", invoices, ct);

            return new XeroAgedReceivablesSnapshot(true, null, DateTimeOffset.UtcNow, truncated, invoices);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroAgedReceivablesSnapshot.Failed(callFailure.Message);
        }
    }

    /// <summary>
    /// Pages through one collection of outstanding sales-side documents into
    /// <paramref name="into"/>; true = page cap hit with data left. DRAFT and SUBMITTED are
    /// requested alongside AUTHORISED, mirroring the payables read — an invoice still being
    /// prepared is part of the honest receivables picture even though Xero's own report
    /// cannot see it. Deliberately NOT summaryOnly (Xero's lightweight mode rejects
    /// where/order with an HTTP 400 — see the sales-side read); the line items on the full
    /// shape are simply ignored.
    /// </summary>
    private async Task<bool> FetchOutstandingReceivablesAsync(
        string token, string baseUrl, string collectionProperty, string xeroType, string order,
        List<XeroReceivableInvoice> into, CancellationToken ct)
    {
        var where = $"Type==\"{xeroType}\" AND "
                    + "(Status==\"DRAFT\" OR Status==\"SUBMITTED\" OR Status==\"AUTHORISED\")";

        for (var page = 1; page <= _options.MaxPages; page++)
        {
            var url = $"{baseUrl}?page={page}"
                      + $"&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString(order)}";
            using var doc = await GetJsonAsync(token, url, collectionProperty.ToLowerInvariant(), ct);

            if (!doc.RootElement.TryGetProperty(collectionProperty, out var items)
                || items.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in items.EnumerateArray())
            {
                // AmountDue for invoices, RemainingCredit for credit notes — both mean "still
                // outstanding". Zero means settled (or an empty draft): nothing to age.
                var amountDue = item.TryGetProperty("AmountDue", out _)
                    ? DecimalOf(item, "AmountDue")
                    : DecimalOf(item, "RemainingCredit");
                if (amountDue == 0m) continue;

                into.Add(new XeroReceivableInvoice(
                    InvoiceId: StringOf(item, "InvoiceID") ?? StringOf(item, "CreditNoteID") ?? Guid.NewGuid().ToString(),
                    Type: StringOf(item, "Type") ?? xeroType,
                    Number: StringOf(item, "InvoiceNumber") ?? StringOf(item, "CreditNoteNumber"),
                    Reference: StringOf(item, "Reference"),
                    ContactName: item.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
                    Date: DateOf(item, "DateString", "Date"),
                    DueDate: DateOf(item, "DueDateString", "DueDate"),
                    Status: StringOf(item, "Status") ?? "UNKNOWN",
                    Total: DecimalOf(item, "Total"),
                    AmountDue: amountDue,
                    CurrencyCode: StringOf(item, "CurrencyCode"),
                    // Xero's "Expected date" (set on the invoice or the Awaiting Payment list)
                    // — invoices only, absent on credit notes, so DateOf simply returns null.
                    ExpectedPaymentDate: DateOf(item, "ExpectedPaymentDateString", "ExpectedPaymentDate")));
            }

            if (items.GetArrayLength() < PageSize) return false; // Short page — no more to fetch.
        }
        return true;
    }

    // -- write-back: tracking confirmation + approval ---------------------------------

}
