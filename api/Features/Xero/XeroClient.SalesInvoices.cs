namespace Jewel.JPMS.Api.Features.Xero;

// Reading the sales side BACK from Xero (2026-09-11): every ACCREC invoice on one contact in any
// status — PAID included, which the aged receivables read can never show — and one invoice by id
// or number. Read fresh every time: a payment is what the caller is looking for, and it may have
// landed a minute ago. Deliberately NOT summaryOnly (Xero's lightweight mode rejects where/order
// with an HTTP 400 — see XeroClient.Reads); the line items on the full shape are simply ignored.
public sealed partial class XeroClient
{
    public async Task<XeroSalesInvoiceListSnapshot> GetSalesInvoicesForContactAsync(string contactId, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroSalesInvoiceListSnapshot.NotConfigured();
        if (string.IsNullOrWhiteSpace(contactId)) return XeroSalesInvoiceListSnapshot.Failed("No Xero contact id to read invoices for.");

        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroSalesInvoiceListSnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            var invoices = new List<XeroSalesInvoiceSummary>();
            // By ContactID, never by name — the same rule as the raise. Status is NOT filtered in
            // the where clause on purpose: PAID and VOIDED belong in the answer (a voided invoice
            // is how the portal learns its link is dead); DELETED alone is dropped portal-side.
            var where = $"Type==\"ACCREC\" AND Contact.ContactID==Guid(\"{EscapedForWhere(contactId.Trim())}\")";

            for (var page = 1; page <= _options.MaxPages; page++)
            {
                var url = $"{InvoicesUrl}?page={page}"
                          + $"&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString("Date DESC")}";
                using var doc = await GetJsonAsync(token, url, "sales invoices", ct);

                if (!doc.RootElement.TryGetProperty("Invoices", out var items) || items.ValueKind != JsonValueKind.Array)
                    break;

                foreach (var item in items.EnumerateArray())
                {
                    var summary = ReadSalesInvoiceSummary(item);
                    if (summary.Status.Equals("DELETED", StringComparison.OrdinalIgnoreCase)) continue;
                    invoices.Add(summary);
                }

                if (items.GetArrayLength() < PageSize) break; // Short page — no more to fetch.
            }

            return new XeroSalesInvoiceListSnapshot(true, null, DateTimeOffset.UtcNow, invoices);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroSalesInvoiceListSnapshot.Failed(callFailure.Message);
        }
    }

    /// <summary>
    /// One sales invoice by Xero's InvoiceID or its InvoiceNumber (Xero's GET Invoices/{id} takes
    /// either). Null when Xero has nothing by that handle (404), or holds something that is not a
    /// sales invoice under it — a supplier's bill can carry any number. Throws
    /// <see cref="XeroCallFailedException"/> when Xero cannot be asked, so "not there" and "Xero
    /// is down" never read the same.
    /// </summary>
    public async Task<XeroSalesInvoiceSummary?> GetSalesInvoiceAsync(string invoiceIdOrNumber, CancellationToken ct)
    {
        if (!_options.IsConfigured) throw new XeroCallFailedException(NotConnected);
        if (string.IsNullOrWhiteSpace(invoiceIdOrNumber)) return null;
        var token = await GetAccessTokenAsync(ct);
        (JsonDocument Document, JsonElement? Invoice) fresh;
        try
        {
            fresh = await ReadFreshAsync(token, InvoicesUrl, "Invoices", Uri.EscapeDataString(invoiceIdOrNumber.Trim()), ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 404"))
        {
            return null;
        }
        using (fresh.Document)
        {
            if (fresh.Invoice is not { } invoice) return null;
            if (!string.Equals(StringOf(invoice, "Type"), "ACCREC", StringComparison.OrdinalIgnoreCase)) return null;
            var summary = ReadSalesInvoiceSummary(invoice);
            return summary.Status.Equals("DELETED", StringComparison.OrdinalIgnoreCase) ? null : summary;
        }
    }

    private static XeroSalesInvoiceSummary ReadSalesInvoiceSummary(JsonElement invoice) => new(
        InvoiceId: StringOf(invoice, "InvoiceID") ?? "",
        Number: StringOf(invoice, "InvoiceNumber"),
        Reference: StringOf(invoice, "Reference"),
        Status: StringOf(invoice, "Status") ?? "UNKNOWN",
        Date: DateOf(invoice, "DateString", "Date"),
        DueDate: DateOf(invoice, "DueDateString", "DueDate"),
        SubTotal: DecimalOf(invoice, "SubTotal"),
        TotalTax: DecimalOf(invoice, "TotalTax"),
        Total: DecimalOf(invoice, "Total"),
        AmountPaid: DecimalOf(invoice, "AmountPaid"),
        AmountDue: DecimalOf(invoice, "AmountDue"),
        // Xero sends FullyPaidOnDate in the legacy "/Date(ms+0000)/" form only (no "…String"
        // twin) — DateOf falls through to it when the ISO field is absent.
        FullyPaidOnDate: DateOf(invoice, "FullyPaidOnDateString", "FullyPaidOnDate"),
        CurrencyCode: StringOf(invoice, "CurrencyCode"));
}
