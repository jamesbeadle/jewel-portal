using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Minimal client for the Xero Accounting API over a custom connection (client-credentials grant).
/// Returns a snapshot rather than throwing so the UI can explain "not configured" and "Xero said no"
/// states instead of surfacing a 500. Reads purchase invoices with line items (Xero includes line
/// items on paged /Invoices responses) plus the chart of accounts for account names, and caches the
/// assembled snapshot briefly — a multi-year read costs dozens of calls against Xero's 60/min limit.
/// </summary>
public interface IXeroClient
{
    bool IsConfigured { get; }

    /// <summary>
    /// Lists purchase invoices (ACCPAY bills) from the configured start date, newest first.
    /// Serves the cached snapshot when fresh enough unless <paramref name="force"/> is set.
    /// </summary>
    Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct);
}

/// <summary>No-op used when no Xero client id/secret is configured; reports itself as such.</summary>
public sealed class NullXeroClient : IXeroClient
{
    public bool IsConfigured => false;

    public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroTransactionsSnapshot.NotConfigured());
}

/// <summary>REST implementation (hand-rolled HttpClient, matching the app's style — see ClaudeClient).</summary>
public sealed class XeroClient : IXeroClient
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";
    private const string CreditNotesUrl = "https://api.xero.com/api.xro/2.0/CreditNotes";
    private const string AccountsUrl = "https://api.xero.com/api.xro/2.0/Accounts";
    private const int PageSize = 100; // Xero's page size for the Invoices endpoint.

    private readonly HttpClient _http;
    private readonly XeroOptions _options;
    private readonly ILogger<XeroClient> _logger;

    // Client-credentials tokens last ~30 minutes; cache until shortly before expiry.
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    // Snapshot cache — one fetch serves every user for CacheMinutes. Guarded by a lock so two
    // simultaneous page loads don't both run a multi-page Xero read.
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);
    private XeroTransactionsSnapshot? _cachedSnapshot;
    private DateTimeOffset _cachedSnapshotAt = DateTimeOffset.MinValue;

    // Chart of accounts changes rarely; refresh it hourly at most.
    private IReadOnlyDictionary<string, string>? _accountNamesByCode;
    private DateTimeOffset _accountNamesFetchedAt = DateTimeOffset.MinValue;

    public XeroClient(HttpClient http, XeroOptions options, ILogger<XeroClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

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

    private async Task<XeroTransactionsSnapshot> FetchSnapshotAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroTransactionsSnapshot.Failed(tokenFailure.Message);
        }

        var accountNames = await GetAccountNamesAsync(token, ct);

        var transactions = new List<XeroTransaction>();
        var truncated = false;
        try
        {
            // Bills first, then supplier credit notes so spend can be netted off downstream.
            truncated |= await FetchAllPagesAsync(token, InvoicesUrl, "Invoices", "ACCPAY", accountNames, transactions, ct);
            truncated |= await FetchAllPagesAsync(token, CreditNotesUrl, "CreditNotes", "ACCPAYCREDIT", accountNames, transactions, ct);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroTransactionsSnapshot.Failed(callFailure.Message);
        }

        return new XeroTransactionsSnapshot(true, null, _options.FromDate, DateTimeOffset.UtcNow, truncated, transactions);
    }

    /// <summary>Pages through one Xero collection into <paramref name="into"/>; true = page cap hit with data left.</summary>
    private async Task<bool> FetchAllPagesAsync(
        string token, string baseUrl, string collectionProperty, string xeroType,
        IReadOnlyDictionary<string, string> accountNames, List<XeroTransaction> into, CancellationToken ct)
    {
        for (var page = 1; page <= _options.MaxPages; page++)
        {
            var pageOfTransactions = await FetchPageAsync(token, baseUrl, collectionProperty, xeroType, page, accountNames, ct);
            into.AddRange(pageOfTransactions);
            if (pageOfTransactions.Count < PageSize) return false; // Short page — no more to fetch.
        }
        return true;
    }

    private async Task<IReadOnlyList<XeroTransaction>> FetchPageAsync(
        string token, string baseUrl, string collectionProperty, string xeroType, int page,
        IReadOnlyDictionary<string, string> accountNames, CancellationToken ct)
    {
        // Purchase side only, inside the reporting window. Paged responses include line items,
        // which carry the site / cost-code tracking.
        var from = _options.FromDate;
        var where = $"Type==\"{xeroType}\" AND Date >= DateTime({from.Year},{from.Month:D2},{from.Day:D2})";
        var url = $"{baseUrl}?page={page}&where={Uri.EscapeDataString(where)}&order={Uri.EscapeDataString("Date DESC")}";

        using var doc = await GetJsonAsync(token, url, collectionProperty.ToLowerInvariant(), ct);

        if (!doc.RootElement.TryGetProperty(collectionProperty, out var items) || items.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroTransaction>();

        return items.EnumerateArray().Select(item => ReadTransaction(item, xeroType, accountNames)).ToList();
    }

    /// <summary>
    /// Account code → account name from the chart of accounts. Best effort: if the custom connection
    /// lacks the accounting.settings scope (or the call fails) lines simply show the bare code.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, string>> GetAccountNamesAsync(string token, CancellationToken ct)
    {
        if (_accountNamesByCode is not null && DateTimeOffset.UtcNow < _accountNamesFetchedAt.AddHours(1))
            return _accountNamesByCode;

        try
        {
            using var doc = await GetJsonAsync(token, AccountsUrl, "accounts", ct);
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (doc.RootElement.TryGetProperty("Accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array)
            {
                foreach (var account in accounts.EnumerateArray())
                {
                    var code = StringOf(account, "Code");
                    var name = StringOf(account, "Name");
                    if (code is not null && name is not null) names[code] = name;
                }
            }
            _accountNamesByCode = names;
            _accountNamesFetchedAt = DateTimeOffset.UtcNow;
            return names;
        }
        catch (XeroCallFailedException accountsFailure)
        {
            _logger.LogInformation("Chart of accounts unavailable ({Message}); showing account codes only.", accountsFailure.Message);
            return _accountNamesByCode ?? new Dictionary<string, string>();
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string token, string url, string what, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Xero {What} call failed: {Status} {Body}.", what, (int)response.StatusCode, Truncate(body));
            throw new XeroCallFailedException($"Xero rejected the {what} request with HTTP {(int)response.StatusCode}. {Truncate(body)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    /// <summary>
    /// Maps one invoice or credit note. The two shapes differ only in id/number field names
    /// (InvoiceID/InvoiceNumber vs CreditNoteID/CreditNoteNumber) and credit notes carry
    /// RemainingCredit instead of AmountDue.
    /// </summary>
    private XeroTransaction ReadTransaction(JsonElement item, string xeroType, IReadOnlyDictionary<string, string> accountNames) => new(
        TransactionId: StringOf(item, "InvoiceID") ?? StringOf(item, "CreditNoteID") ?? Guid.NewGuid().ToString(),
        Type: StringOf(item, "Type") ?? xeroType,
        Number: StringOf(item, "InvoiceNumber") ?? StringOf(item, "CreditNoteNumber"),
        Reference: StringOf(item, "Reference"),
        ContactName: item.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
        Date: DateOf(item, "DateString", "Date"),
        DueDate: DateOf(item, "DueDateString", "DueDate"),
        Status: StringOf(item, "Status") ?? "UNKNOWN",
        SubTotal: DecimalOf(item, "SubTotal"),
        TotalTax: DecimalOf(item, "TotalTax"),
        Total: DecimalOf(item, "Total"),
        AmountDue: item.TryGetProperty("AmountDue", out _) ? DecimalOf(item, "AmountDue") : DecimalOf(item, "RemainingCredit"),
        AmountPaid: DecimalOf(item, "AmountPaid"),
        CurrencyCode: StringOf(item, "CurrencyCode"),
        Lines: ReadLines(item, accountNames));

    private IReadOnlyList<XeroTransactionLine> ReadLines(JsonElement invoice, IReadOnlyDictionary<string, string> accountNames)
    {
        if (!invoice.TryGetProperty("LineItems", out var lineItems) || lineItems.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroTransactionLine>();

        // Invoices entered as amounts-inclusive-of-VAT carry VAT inside LineAmount
        // (LineAmountTypes = "Inclusive"); subtract the line's tax so LineAmount is always net.
        // Without this, VAT-inclusive bills overstate the cost-code split by their VAT.
        var vatInclusive = string.Equals(StringOf(invoice, "LineAmountTypes"), "Inclusive", StringComparison.OrdinalIgnoreCase);

        return lineItems.EnumerateArray().Select(line =>
        {
            var accountCode = StringOf(line, "AccountCode");
            var taxAmount = DecimalOf(line, "TaxAmount");
            var lineAmount = DecimalOf(line, "LineAmount");
            return new XeroTransactionLine(
                LineItemId: StringOf(line, "LineItemID"),
                Description: StringOf(line, "Description"),
                Quantity: DecimalOf(line, "Quantity"),
                UnitAmount: DecimalOf(line, "UnitAmount"),
                LineAmount: vatInclusive ? lineAmount - taxAmount : lineAmount,
                TaxAmount: taxAmount,
                AccountCode: accountCode,
                AccountName: accountCode is not null && accountNames.TryGetValue(accountCode, out var name) ? name : null,
                Site: TrackingOptionOf(line, _options.SiteTrackingCategory),
                CostCode: TrackingOptionOf(line, _options.CostCodeTrackingCategory));
        }).ToList();
    }

    /// <summary>
    /// Reads the option of the named tracking category off a line, tolerating spacing/case
    /// differences ("Cost code" vs "CostCode"). Line tracking shape: [{ "Name": …, "Option": … }].
    /// </summary>
    private static string? TrackingOptionOf(JsonElement line, string categoryName)
    {
        if (!line.TryGetProperty("Tracking", out var tracking) || tracking.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var entry in tracking.EnumerateArray())
        {
            var name = StringOf(entry, "Name");
            if (name is not null && Normalise(name) == Normalise(categoryName))
                return StringOf(entry, "Option");
        }
        return null;
    }

    private static string Normalise(string categoryName) =>
        categoryName.Replace(" ", "").ToLowerInvariant();

    private static string? StringOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal DecimalOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : 0m;

    /// <summary>
    /// Xero's JSON carries dates twice: an ISO "…String" field and a legacy "/Date(ms+0000)/" field.
    /// Prefer the ISO one; fall back to parsing the epoch milliseconds out of the legacy form.
    /// </summary>
    private static DateTime? DateOf(JsonElement element, string isoProperty, string legacyProperty)
    {
        var iso = StringOf(element, isoProperty);
        if (iso is not null && DateTime.TryParse(iso, out var parsed)) return parsed;

        var legacy = StringOf(element, legacyProperty);
        if (legacy is null) return null;

        var start = legacy.IndexOf('(');
        var end = legacy.IndexOfAny(new[] { '+', '-', ')' }, start + 1);
        if (start < 0 || end <= start) return null;

        return long.TryParse(legacy[(start + 1)..end], out var epochMs)
            ? DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime
            : null;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            return _accessToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
                return _accessToken;

            // Omit the scope parameter unless explicitly configured — Xero then grants everything
            // the custom connection holds, whereas a mismatch fails with invalid_scope.
            var form = new Dictionary<string, string> { ["grant_type"] = "client_credentials" };
            if (!string.IsNullOrWhiteSpace(_options.Scopes))
                form["scope"] = _options.Scopes;

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Xero token call failed: {Status} {Body}.", (int)response.StatusCode, Truncate(body));
                throw new XeroCallFailedException(
                    $"Xero rejected the credentials with HTTP {(int)response.StatusCode}. Check Xero__ClientId / Xero__ClientSecret and that the custom connection has an accounting scope (e.g. accounting.transactions) ticked in the Xero developer portal. {Truncate(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            var token = doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new XeroCallFailedException("Xero's token response contained no access_token.");
            var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var expiry) && expiry.TryGetInt32(out var seconds)
                ? seconds
                : 1800;

            _accessToken = token;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds - 60);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 300 ? value : value[..300] + "…";
}

/// <summary>Internal signal that a Xero call failed with a message safe to show in the snapshot.</summary>
internal sealed class XeroCallFailedException : Exception
{
    public XeroCallFailedException(string message) : base(message) { }
}
