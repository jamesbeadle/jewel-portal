using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging;

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

    public async Task<XeroSuppliersSnapshot> GetSuppliersAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroSuppliersSnapshot.NotConfigured();

        await _suppliersLock.WaitAsync(ct);
        try
        {
            if (!force && CachedSuppliersAreFresh)
                return _cachedSuppliers!;

            var snapshot = await FetchSuppliersAsync(ct);

            // Only successful reads replace the cache — a transient failure shouldn't evict
            // good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedSuppliers = snapshot;
                _cachedSuppliersAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _suppliersLock.Release();
        }
    }

    private bool CachedSuppliersAreFresh =>
        _cachedSuppliers is not null
        && DateTimeOffset.UtcNow < _cachedSuppliersAt.AddMinutes(_options.CacheMinutes);

    private async Task<XeroSuppliersSnapshot> FetchSuppliersAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroSuppliersSnapshot.Failed(tokenFailure.Message);
        }

        var suppliers = new List<XeroSupplier>();
        var truncated = false;
        try
        {
            // EVERY active contact, not where=IsSupplier==true: Xero only sets IsSupplier once a
            // contact has had a bill, so the filter would hide a supplier created moments ago and
            // never yet billed. The flags come back per row and the modal narrows client-side
            // (customer-only contacts hidden by default). includeArchived is deliberately not
            // sent. Paged like the invoices read.
            for (var page = 1; ; page++)
            {
                if (page > _options.MaxPages) { truncated = true; break; }

                var url = $"{ContactsUrl}?page={page}&order={Uri.EscapeDataString("Name")}";
                using var doc = await GetJsonAsync(token, url, "contacts", ct);

                if (!doc.RootElement.TryGetProperty("Contacts", out var contacts) || contacts.ValueKind != JsonValueKind.Array)
                    break;

                var pageOfSuppliers = contacts.EnumerateArray().Select(ReadSupplier).ToList();
                suppliers.AddRange(pageOfSuppliers);
                if (pageOfSuppliers.Count < PageSize) break; // Short page — no more to fetch.
            }
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroSuppliersSnapshot.Failed(callFailure.Message);
        }

        return new XeroSuppliersSnapshot(true, null, DateTimeOffset.UtcNow, truncated, suppliers);
    }

    // -- tracking categories: the Cost codes page's Xero sites / Xero cost codes tabs ---

    public async Task<XeroTrackingCategoriesSnapshot> GetTrackingCategoriesSnapshotAsync(bool force, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroTrackingCategoriesSnapshot.NotConfigured();

        await _trackingCategoriesLock.WaitAsync(ct);
        try
        {
            if (!force && CachedTrackingCategoriesAreFresh)
                return _cachedTrackingCategories!;

            var snapshot = await FetchTrackingCategoriesSnapshotAsync(ct);

            // Only successful reads replace the cache — a transient failure (429 above all)
            // shouldn't evict good data, but it is still returned so the user sees what went wrong.
            if (snapshot.Error is null)
            {
                _cachedTrackingCategories = snapshot;
                _cachedTrackingCategoriesAt = DateTimeOffset.UtcNow;
            }
            return snapshot;
        }
        finally
        {
            _trackingCategoriesLock.Release();
        }
    }

    private bool CachedTrackingCategoriesAreFresh =>
        _cachedTrackingCategories is not null
        && DateTimeOffset.UtcNow < _cachedTrackingCategoriesAt.AddMinutes(_options.CacheMinutes);

    private async Task<XeroTrackingCategoriesSnapshot> FetchTrackingCategoriesSnapshotAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroTrackingCategoriesSnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            // includeArchived: a retired option's exact name still explains historical tracking,
            // and hiding it here would make "why doesn't this match?" harder, not easier. The
            // UI flags archived rows instead. Unlike GetTrackingCategoriesAsync (the write-back's
            // lookup) this read is diagnostic: EVERY category comes back, and a missing Sites /
            // Cost Code category is the UI's message to render, not an exception.
            using var doc = await GetJsonAsync(
                token, $"{TrackingCategoriesUrl}?includeArchived=true", "tracking categories", ct);

            var categories = new List<XeroTrackingCategory>();
            if (doc.RootElement.TryGetProperty("TrackingCategories", out var trackingCategories)
                && trackingCategories.ValueKind == JsonValueKind.Array)
            {
                foreach (var category in trackingCategories.EnumerateArray())
                {
                    var name = StringOf(category, "Name");
                    var id = StringOf(category, "TrackingCategoryID");
                    if (name is null || id is null) continue;

                    var options = new List<XeroTrackingOption>();
                    if (category.TryGetProperty("Options", out var optionElements) && optionElements.ValueKind == JsonValueKind.Array)
                        foreach (var option in optionElements.EnumerateArray())
                            if (StringOf(option, "Name") is { } optionName)
                                options.Add(new XeroTrackingOption(
                                    StringOf(option, "TrackingOptionID") ?? "",
                                    optionName,
                                    StringOf(option, "Status") ?? "ACTIVE"));

                    categories.Add(new XeroTrackingCategory(
                        id,
                        name,
                        StringOf(category, "Status") ?? "ACTIVE",
                        options,
                        IsSiteCategory: Normalise(name) == Normalise(_options.SiteTrackingCategory),
                        IsCostCodeCategory: Normalise(name) == Normalise(_options.CostCodeTrackingCategory)));
                }
            }

            return new XeroTrackingCategoriesSnapshot(true, null, DateTimeOffset.UtcNow, categories);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroTrackingCategoriesSnapshot.Failed(
                "Couldn't read Xero's tracking categories. If Xero answered 403, the custom "
                + "connection needs the accounting.settings scope; a 429 means Xero's rate "
                + "limit — wait a minute and refresh. " + callFailure.Message);
        }
    }

    private static XeroSupplier ReadSupplier(JsonElement contact) => new(
        ContactId: StringOf(contact, "ContactID") ?? Guid.NewGuid().ToString(),
        Name: StringOf(contact, "Name") ?? "",
        EmailAddress: StringOf(contact, "EmailAddress") ?? "",
        Phone: PhoneOf(contact, "DEFAULT") ?? PhoneOf(contact, "DDI") ?? "",
        Mobile: PhoneOf(contact, "MOBILE") ?? "",
        Town: AddressPartOf(contact, "City"),
        County: AddressPartOf(contact, "Region"),
        AddressLine: StreetOf(contact),
        Postcode: AddressPartOf(contact, "PostalCode"),
        ContactPersons: ReadContactPersons(contact),
        IsSupplier: BoolOf(contact, "IsSupplier"),
        IsCustomer: BoolOf(contact, "IsCustomer"));

    /// <summary>The street line(s) from the contact's first address that carries any — Xero's
    /// AddressLine1–4 joined onto one line for the directory record's AddressLine field.</summary>
    private static string StreetOf(JsonElement contact)
    {
        if (!contact.TryGetProperty("Addresses", out var addresses) || addresses.ValueKind != JsonValueKind.Array)
            return "";
        foreach (var address in addresses.EnumerateArray())
        {
            var street = string.Join(", ",
                new[] { StringOf(address, "AddressLine1"), StringOf(address, "AddressLine2"),
                        StringOf(address, "AddressLine3"), StringOf(address, "AddressLine4") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(street)) return street;
        }
        return "";
    }

    /// <summary>One phone line by Xero PhoneType, assembled country + area + number; null when empty.</summary>
    private static string? PhoneOf(JsonElement contact, string phoneType)
    {
        if (!contact.TryGetProperty("Phones", out var phones) || phones.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var phone in phones.EnumerateArray())
        {
            if (!string.Equals(StringOf(phone, "PhoneType"), phoneType, StringComparison.OrdinalIgnoreCase))
                continue;
            var number = string.Join(" ",
                new[] { StringOf(phone, "PhoneCountryCode"), StringOf(phone, "PhoneAreaCode"), StringOf(phone, "PhoneNumber") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(number)) return number;
        }
        return null;
    }

    /// <summary>One field ("City"/"Region") from the contact's first address that carries it.</summary>
    private static string AddressPartOf(JsonElement contact, string part)
    {
        if (!contact.TryGetProperty("Addresses", out var addresses) || addresses.ValueKind != JsonValueKind.Array)
            return "";
        foreach (var address in addresses.EnumerateArray())
        {
            var value = StringOf(address, part);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return "";
    }

    private static IReadOnlyList<XeroContactPerson> ReadContactPersons(JsonElement contact)
    {
        if (!contact.TryGetProperty("ContactPersons", out var persons) || persons.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroContactPerson>();
        return persons.EnumerateArray()
            .Select(person => new XeroContactPerson(
                Name: string.Join(" ",
                    new[] { StringOf(person, "FirstName"), StringOf(person, "LastName") }
                        .Where(part => !string.IsNullOrWhiteSpace(part))),
                EmailAddress: StringOf(person, "EmailAddress") ?? ""))
            .Where(person => !string.IsNullOrWhiteSpace(person.Name) || !string.IsNullOrWhiteSpace(person.EmailAddress))
            .ToList();
    }

    private async Task<XeroCashSummarySnapshot> FetchCashSummaryAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroCashSummarySnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            var bankAccounts = await FetchBankBalancesAsync(token, ct);
            var outstanding = await FetchOutstandingSalesInvoicesAsync(token, ct);
            return new XeroCashSummarySnapshot(true, null, DateTimeOffset.UtcNow, bankAccounts, outstanding);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroCashSummarySnapshot.Failed(callFailure.Message);
        }
    }

    /// <summary>
    /// Each bank account's closing balance as of today, from Xero's bank summary report
    /// (the report is in the organisation's base currency). The report's rows carry the
    /// account name + accountID in the first cell and the closing balance in the column the
    /// header names "Closing Balance" (last column as a fallback, so a report-layout tweak
    /// degrades gracefully rather than dropping balances).
    /// </summary>
    private async Task<IReadOnlyList<XeroBankAccountBalance>> FetchBankBalancesAsync(string token, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var url = $"{BankSummaryReportUrl}?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";

        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, url, "bank summary report", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            throw new XeroCallFailedException(
                "Couldn't read Xero's bank summary report — the Xero custom connection needs the "
                + "accounting.reports.read scope ticked in the Xero developer portal. " + failure.Message);
        }

        using (doc)
        {
            var balances = new List<XeroBankAccountBalance>();
            if (!doc.RootElement.TryGetProperty("Reports", out var reports)
                || reports.ValueKind != JsonValueKind.Array || reports.GetArrayLength() == 0)
                return balances;

            var closingColumn = -1;
            foreach (var row in RowsOf(reports[0]))
            {
                var rowType = StringOf(row, "RowType");
                if (rowType == "Header")
                {
                    closingColumn = FindColumn(row, "Closing Balance");
                    continue;
                }
                if (rowType != "Section") continue;

                foreach (var accountRow in RowsOf(row))
                {
                    if (StringOf(accountRow, "RowType") != "Row") continue;
                    if (!accountRow.TryGetProperty("Cells", out var cells)
                        || cells.ValueKind != JsonValueKind.Array || cells.GetArrayLength() == 0)
                        continue;

                    var nameCell = cells[0];
                    var name = StringOf(nameCell, "Value");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var balanceIndex = closingColumn >= 0 && closingColumn < cells.GetArrayLength()
                        ? closingColumn
                        : cells.GetArrayLength() - 1;
                    balances.Add(new XeroBankAccountBalance(
                        AccountId: CellAttribute(nameCell, "accountID") ?? name,
                        Name: name,
                        Balance: CellDecimal(cells[balanceIndex])));
                }
            }
            return balances;
        }
    }

    /// <summary>Rows of a report or section — both nest them under "Rows".</summary>
    private static IEnumerable<JsonElement> RowsOf(JsonElement reportOrSection)
    {
        if (reportOrSection.TryGetProperty("Rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
            foreach (var row in rows.EnumerateArray())
                yield return row;
    }

    private static int FindColumn(JsonElement headerRow, string title)
    {
        if (!headerRow.TryGetProperty("Cells", out var cells) || cells.ValueKind != JsonValueKind.Array)
            return -1;
        var index = 0;
        foreach (var cell in cells.EnumerateArray())
        {
            if (string.Equals(StringOf(cell, "Value"), title, StringComparison.OrdinalIgnoreCase))
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>Report cell values arrive as strings ("12345.67"); attributes as [{ Id, Value }].</summary>
    private static decimal CellDecimal(JsonElement cell) =>
        decimal.TryParse(StringOf(cell, "Value"), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;

    private static string? CellAttribute(JsonElement cell, string id)
    {
        if (!cell.TryGetProperty("Attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var attribute in attributes.EnumerateArray())
            if (string.Equals(StringOf(attribute, "Id"), id, StringComparison.OrdinalIgnoreCase))
                return StringOf(attribute, "Value");
        return null;
    }

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
