using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero;

/// <summary>
/// Minimal client for the Xero Accounting API over a custom connection (client-credentials grant).
/// Returns a snapshot rather than throwing so the UI can explain "not configured" and "Xero said no"
/// states instead of surfacing a 500.
/// </summary>
public interface IXeroClient
{
    bool IsConfigured { get; }

    /// <summary>Lists purchase invoices (ACCPAY bills), newest first, across up to MaxPages pages.</summary>
    Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(CancellationToken ct);
}

/// <summary>No-op used when no Xero client id/secret is configured; reports itself as such.</summary>
public sealed class NullXeroClient : IXeroClient
{
    public bool IsConfigured => false;

    public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(CancellationToken ct) =>
        Task.FromResult(XeroTransactionsSnapshot.NotConfigured());
}

/// <summary>REST implementation (hand-rolled HttpClient, matching the app's style — see ClaudeClient).</summary>
public sealed class XeroClient : IXeroClient
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";
    private const int PageSize = 100; // Xero's page size for the Invoices endpoint.

    private readonly HttpClient _http;
    private readonly XeroOptions _options;
    private readonly ILogger<XeroClient> _logger;

    // Client-credentials tokens last ~30 minutes; cache until shortly before expiry.
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    public XeroClient(HttpClient http, XeroOptions options, ILogger<XeroClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroTransactionsSnapshot.NotConfigured();

        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroTransactionsSnapshot.Failed(tokenFailure.Message);
        }

        var transactions = new List<XeroTransaction>();
        try
        {
            for (var page = 1; page <= _options.MaxPages; page++)
            {
                var pageOfInvoices = await FetchInvoicePageAsync(token, page, ct);
                transactions.AddRange(pageOfInvoices);
                if (pageOfInvoices.Count < PageSize) break; // Short page — no more to fetch.
            }
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroTransactionsSnapshot.Failed(callFailure.Message);
        }

        return new XeroTransactionsSnapshot(true, null, transactions);
    }

    private async Task<IReadOnlyList<XeroTransaction>> FetchInvoicePageAsync(string token, int page, CancellationToken ct)
    {
        // Purchase invoices only (supplier bills) — the cost-of-sales side of the ledger.
        var url = $"{InvoicesUrl}?page={page}&where={Uri.EscapeDataString("Type==\"ACCPAY\"")}&order={Uri.EscapeDataString("Date DESC")}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Xero invoices call failed: {Status} {Body}.", (int)response.StatusCode, Truncate(body));
            throw new XeroCallFailedException($"Xero rejected the invoices request with HTTP {(int)response.StatusCode}. {Truncate(body)}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("Invoices", out var invoices) || invoices.ValueKind != JsonValueKind.Array)
            return Array.Empty<XeroTransaction>();

        return invoices.EnumerateArray().Select(ReadInvoice).ToList();
    }

    private static XeroTransaction ReadInvoice(JsonElement invoice) => new(
        TransactionId: StringOf(invoice, "InvoiceID") ?? Guid.NewGuid().ToString(),
        Type: StringOf(invoice, "Type") ?? "ACCPAY",
        Number: StringOf(invoice, "InvoiceNumber"),
        Reference: StringOf(invoice, "Reference"),
        ContactName: invoice.TryGetProperty("Contact", out var contact) ? StringOf(contact, "Name") : null,
        Date: DateOf(invoice, "DateString", "Date"),
        DueDate: DateOf(invoice, "DueDateString", "DueDate"),
        Status: StringOf(invoice, "Status") ?? "UNKNOWN",
        SubTotal: DecimalOf(invoice, "SubTotal"),
        TotalTax: DecimalOf(invoice, "TotalTax"),
        Total: DecimalOf(invoice, "Total"),
        AmountDue: DecimalOf(invoice, "AmountDue"),
        AmountPaid: DecimalOf(invoice, "AmountPaid"),
        CurrencyCode: StringOf(invoice, "CurrencyCode"));

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
