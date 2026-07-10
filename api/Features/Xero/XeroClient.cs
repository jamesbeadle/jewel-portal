using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    /// <summary>
    /// Confirms an allocated draft (or submitted) bill / credit note back into Xero and
    /// approves it: re-reads the invoice fresh, stamps Sites + Cost Code tracking on each
    /// instructed line — physically splitting a line into one Xero line per cost centre
    /// when the allocation is split, amounts pro-rated so the invoice total is unchanged —
    /// then sets the status to AUTHORISED in the same update. Missing Cost Code tracking
    /// options are created in Xero; a missing Sites option fails loudly instead (sites are
    /// an explicit per-project mapping, never invented here). Returns a result rather than
    /// throwing so callers can stamp the outcome onto the stored ledger lines.
    /// </summary>
    Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct);

    /// <summary>
    /// Lists the attachments Xero holds for one invoice or credit note — the supplier's
    /// document(s), typically published by Dext. Requires the custom connection's
    /// accounting.attachments scope; throws <see cref="XeroCallFailedException"/> (message
    /// safe to surface) when Xero refuses.
    /// </summary>
    Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct);

    /// <summary>
    /// Streams one attachment's bytes by file name (Xero's attachment content endpoint is
    /// addressed by file name, with the Accept header naming the content type). Null when
    /// the invoice has no attachment by that name.
    /// </summary>
    Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct);
}

/// <summary>One attachment's bytes plus the content type Xero reported for it.</summary>
public sealed record XeroAttachmentContent(byte[] Content, string ContentType, string FileName);

/// <summary>No-op used when no Xero client id/secret is configured; reports itself as such.</summary>
public sealed class NullXeroClient : IXeroClient
{
    public bool IsConfigured => false;

    public Task<XeroTransactionsSnapshot> GetPurchaseInvoicesAsync(bool force, CancellationToken ct) =>
        Task.FromResult(XeroTransactionsSnapshot.NotConfigured());

    public Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct) =>
        Task.FromResult(XeroApprovalResult.Failed(
            "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings."));

    public Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<XeroInvoiceAttachment>>(Array.Empty<XeroInvoiceAttachment>());

    public Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct) =>
        Task.FromResult<XeroAttachmentContent?>(null);
}

/// <summary>REST implementation (hand-rolled HttpClient, matching the app's style — see ClaudeClient).</summary>
public sealed class XeroClient : IXeroClient
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";
    private const string CreditNotesUrl = "https://api.xero.com/api.xro/2.0/CreditNotes";
    private const string AccountsUrl = "https://api.xero.com/api.xro/2.0/Accounts";
    private const string TrackingCategoriesUrl = "https://api.xero.com/api.xro/2.0/TrackingCategories";
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

    // -- write-back: tracking confirmation + approval ---------------------------------

    public async Task<XeroApprovalResult> ApproveInvoiceAsync(XeroApprovalRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

        try
        {
            var token = await GetAccessTokenAsync(ct);

            var baseUrl = request.IsCreditNote ? CreditNotesUrl : InvoicesUrl;
            var collection = request.IsCreditNote ? "CreditNotes" : "Invoices";
            var idProperty = request.IsCreditNote ? "CreditNoteID" : "InvoiceID";

            // Always work from a fresh read — the stored ledger line may be minutes or
            // days old, and the update below replaces the invoice's entire line list.
            using var doc = await GetJsonAsync(token, $"{baseUrl}/{request.InvoiceId}", collection.ToLowerInvariant(), ct);
            if (!doc.RootElement.TryGetProperty(collection, out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var invoice = items[0];
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            if (status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed($"The invoice is {status} in Xero and can't be approved.");

            var categories = await GetTrackingCategoriesAsync(token, ct);

            // Sites are an explicit per-project mapping — a missing option means the
            // mapping is wrong (or the option was renamed in Xero), so fail loudly.
            var missingSites = request.Lines.SelectMany(line => line.Shares)
                .Select(share => share.SiteOption)
                .Where(site => !categories.SiteOptions.Contains(site))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingSites.Count > 0)
                return XeroApprovalResult.Failed(
                    $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                    + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                    + " — check the project's Xero site mapping against Xero's tracking options.");

            // Build (and thereby validate against drift) BEFORE touching Xero — creating
            // tracking options for an approval that then fails would mutate Xero for nothing.
            var lineItems = BuildLineItems(invoice, request, categories, out var buildError);
            if (lineItems is null)
                return XeroApprovalResult.Failed(buildError!);

            // Master cost codes are JPMS-owned; create any that Xero doesn't hold yet so
            // the confirmation can be recorded. (Xero caps a category at 100 options — a
            // rejection here surfaces verbatim for the finance team to resolve.)
            var missingCodes = request.Lines.SelectMany(line => line.Shares)
                .Select(share => share.CostCenterCode)
                .Where(code => !categories.CostCodeOptions.Contains(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var code in missingCodes)
            {
                var optionBody = new JsonObject { ["Name"] = code };
                using var _ = await SendJsonAsync(HttpMethod.Put, token,
                    $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options",
                    optionBody, $"create Cost Code option {code}", ct);
            }

            var payload = new JsonObject
            {
                [idProperty] = request.InvoiceId,
                ["Status"] = "AUTHORISED",
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{baseUrl}/{request.InvoiceId}", payload, "approve invoice", ct);

            // The cached snapshot now lies about this invoice's status; drop it so the
            // next sync re-reads rather than resurrecting DRAFT for up to CacheMinutes.
            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;

            return XeroApprovalResult.Ok("AUTHORISED");
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    // -- attachments: the supplier's document, viewed from the allocation page ---------

    public async Task<IReadOnlyList<XeroInvoiceAttachment>> ListAttachmentsAsync(
        string invoiceId, bool isCreditNote, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        var baseUrl = isCreditNote ? CreditNotesUrl : InvoicesUrl;

        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, $"{baseUrl}/{invoiceId}/Attachments", "attachments", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            throw new XeroCallFailedException(
                "Couldn't read the invoice's attachments — the Xero custom connection needs the "
                + "accounting.attachments scope ticked in the Xero developer portal. " + failure.Message);
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("Attachments", out var items)
                || items.ValueKind != JsonValueKind.Array)
                return Array.Empty<XeroInvoiceAttachment>();

            return items.EnumerateArray()
                .Select(item => new XeroInvoiceAttachment(
                    AttachmentId: StringOf(item, "AttachmentID") ?? "",
                    FileName: StringOf(item, "FileName") ?? "attachment",
                    MimeType: StringOf(item, "MimeType") ?? "application/octet-stream",
                    ContentLength: item.TryGetProperty("ContentLength", out var length)
                        && length.ValueKind == JsonValueKind.Number ? length.GetInt64() : 0))
                .Where(attachment => attachment.AttachmentId.Length > 0)
                .ToList();
        }
    }

    public async Task<XeroAttachmentContent?> GetAttachmentAsync(
        string invoiceId, bool isCreditNote, string fileName, CancellationToken ct)
    {
        // The list gives the attachment's real MimeType — Xero's content endpoint wants it
        // in the Accept header — and confirms the file actually belongs to this invoice.
        var attachments = await ListAttachmentsAsync(invoiceId, isCreditNote, ct);
        var attachment = attachments.FirstOrDefault(candidate =>
            candidate.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (attachment is null) return null;

        var token = await GetAccessTokenAsync(ct);
        var baseUrl = isCreditNote ? CreditNotesUrl : InvoicesUrl;
        var url = $"{baseUrl}/{invoiceId}/Attachments/{Uri.EscapeDataString(attachment.FileName)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(attachment.MimeType));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Xero attachment call failed: {Status} {Body}.", (int)response.StatusCode, Truncate(body));
            throw new XeroCallFailedException(
                $"Xero rejected the attachment request with HTTP {(int)response.StatusCode}. {Truncate(body)}");
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? attachment.MimeType;
        return new XeroAttachmentContent(content, contentType, attachment.FileName);
    }

    /// <summary>
    /// Rebuilds the invoice's full line list for the update: untouched lines pass through
    /// as-is (keyed by LineItemID so Xero updates in place), single-centre lines get their
    /// tracking replaced, and split lines are replaced by one new line per cost centre with
    /// pro-rated amounts. Returns null (with an error) when the invoice no longer matches
    /// the stored allocation — edited amounts or removed lines — because silently approving
    /// figures nobody allocated would corrupt the accounts.
    /// </summary>
    private JsonArray? BuildLineItems(
        JsonElement invoice, XeroApprovalRequest request, TrackingCategoryLookup categories, out string? error)
    {
        error = null;
        var instructionsByLineId = request.Lines.ToDictionary(
            line => line.LineItemId, StringComparer.OrdinalIgnoreCase);
        var seenLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var vatInclusive = string.Equals(StringOf(invoice, "LineAmountTypes"), "Inclusive", StringComparison.OrdinalIgnoreCase);
        var result = new JsonArray();

        if (invoice.TryGetProperty("LineItems", out var lineItems) && lineItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var line in lineItems.EnumerateArray())
            {
                var lineItemId = StringOf(line, "LineItemID");
                if (lineItemId is null || !instructionsByLineId.TryGetValue(lineItemId, out var instruction))
                {
                    // Not a queued cost-of-sales line — pass through untouched.
                    result.Add(CopyLine(line, keepLineItemId: true, keepTracking: true, includeTaxAmount: true));
                    continue;
                }
                seenLineIds.Add(lineItemId);

                // Guard against drift: the allocation was made against the stored net;
                // if the bill was edited in Xero since the last sync, stop and ask for
                // a re-sync + re-allocation instead of approving changed figures.
                var lineAmount = DecimalOf(line, "LineAmount");
                var taxAmount = DecimalOf(line, "TaxAmount");
                var freshNet = vatInclusive ? lineAmount - taxAmount : lineAmount;
                var allocatedNet = instruction.Shares.Sum(share => share.Net);
                if (Math.Abs(freshNet - allocatedNet) > 0.01m)
                {
                    error = $"Line \"{StringOf(line, "Description")}\" is {freshNet:0.00} net in Xero but was allocated as "
                            + $"{allocatedNet:0.00} — the bill has changed since it was synced. Sync from Xero and re-allocate.";
                    return null;
                }

                if (instruction.Shares.Count == 1)
                {
                    var share = instruction.Shares[0];
                    var copy = CopyLine(line, keepLineItemId: true, keepTracking: false, includeTaxAmount: true);
                    copy["Tracking"] = TrackingFor(categories, share.SiteOption, share.CostCenterCode);
                    result.Add(copy);
                }
                else
                {
                    // One Xero line per share (its own site + cost code — shares can point
                    // at different projects). Both the raw LineAmount (VAT-inclusive or not
                    // — proportions are identical) and the original TaxAmount are pro-rated
                    // with the same penny-safe maths, so the bill's net, tax and gross
                    // totals are all unchanged to the penny by the split — per-line tax
                    // recalculation by Xero could otherwise drift by a penny per piece.
                    var weights = instruction.Shares.Select(share => share.Net).ToList();
                    var amounts = XeroSplitMaths.ProportionalShares(lineAmount, weights);
                    var taxes = XeroSplitMaths.ProportionalShares(taxAmount, weights);
                    var description = StringOf(line, "Description");
                    for (var i = 0; i < instruction.Shares.Count; i++)
                    {
                        var share = instruction.Shares[i];
                        var piece = CopyLine(line, keepLineItemId: false, keepTracking: false, includeTaxAmount: false);
                        piece["Description"] = $"{description} [{share.CostCenterCode}]";
                        piece["Quantity"] = 1m;
                        piece["UnitAmount"] = amounts[i];
                        piece["LineAmount"] = amounts[i];
                        piece["TaxAmount"] = taxes[i];
                        piece["Tracking"] = TrackingFor(categories, share.SiteOption, share.CostCenterCode);
                        result.Add(piece);
                    }
                }
            }
        }

        var unmatched = instructionsByLineId.Keys.Where(id => !seenLineIds.Contains(id)).ToList();
        if (unmatched.Count > 0)
        {
            error = "The bill's lines have changed in Xero since they were synced "
                    + $"({unmatched.Count} allocated line(s) no longer exist). Sync from Xero and re-allocate.";
            return null;
        }

        return result;
    }

    /// <summary>Copies the fields the update needs off one original line item.</summary>
    private static JsonObject CopyLine(JsonElement line, bool keepLineItemId, bool keepTracking, bool includeTaxAmount)
    {
        var copy = new JsonObject();
        if (keepLineItemId && StringOf(line, "LineItemID") is { } id) copy["LineItemID"] = id;
        if (StringOf(line, "Description") is { } description) copy["Description"] = description;
        if (line.TryGetProperty("Quantity", out var quantity) && quantity.ValueKind == JsonValueKind.Number)
            copy["Quantity"] = quantity.GetDecimal();
        if (line.TryGetProperty("UnitAmount", out var unitAmount) && unitAmount.ValueKind == JsonValueKind.Number)
            copy["UnitAmount"] = unitAmount.GetDecimal();
        if (line.TryGetProperty("LineAmount", out var lineAmount) && lineAmount.ValueKind == JsonValueKind.Number)
            copy["LineAmount"] = lineAmount.GetDecimal();
        if (includeTaxAmount && line.TryGetProperty("TaxAmount", out var taxAmount) && taxAmount.ValueKind == JsonValueKind.Number)
            copy["TaxAmount"] = taxAmount.GetDecimal();
        if (StringOf(line, "AccountCode") is { } accountCode) copy["AccountCode"] = accountCode;
        if (StringOf(line, "TaxType") is { } taxType) copy["TaxType"] = taxType;
        if (StringOf(line, "ItemCode") is { } itemCode) copy["ItemCode"] = itemCode;
        if (keepTracking && line.TryGetProperty("Tracking", out var tracking) && tracking.ValueKind == JsonValueKind.Array)
            copy["Tracking"] = JsonNode.Parse(tracking.GetRawText());
        return copy;
    }

    private static JsonArray TrackingFor(TrackingCategoryLookup categories, string siteOption, string costCode) => new(
        new JsonObject { ["TrackingCategoryID"] = categories.SiteCategoryId, ["Option"] = siteOption },
        new JsonObject { ["TrackingCategoryID"] = categories.CostCodeCategoryId, ["Option"] = costCode });

    private sealed record TrackingCategoryLookup(
        string SiteCategoryId,
        HashSet<string> SiteOptions,
        string CostCodeCategoryId,
        HashSet<string> CostCodeOptions);

    /// <summary>
    /// Reads the organisation's tracking categories and finds the Sites and Cost Code ones
    /// by their configured names (spacing/case tolerant). Requires the custom connection's
    /// accounting.settings scope — the failure message says so when Xero refuses.
    /// </summary>
    private async Task<TrackingCategoryLookup> GetTrackingCategoriesAsync(string token, CancellationToken ct)
    {
        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, TrackingCategoriesUrl, "tracking categories", ct);
        }
        catch (XeroCallFailedException failure)
        {
            throw new XeroCallFailedException(
                "Couldn't read Xero's tracking categories — the custom connection needs the "
                + "accounting.settings scope to confirm cost codes. " + failure.Message);
        }

        using (doc)
        {
            (string Id, HashSet<string> Options)? sites = null, costCodes = null;
            if (doc.RootElement.TryGetProperty("TrackingCategories", out var trackingCategories)
                && trackingCategories.ValueKind == JsonValueKind.Array)
            {
                foreach (var category in trackingCategories.EnumerateArray())
                {
                    var name = StringOf(category, "Name");
                    var id = StringOf(category, "TrackingCategoryID");
                    if (name is null || id is null) continue;

                    var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (category.TryGetProperty("Options", out var optionElements) && optionElements.ValueKind == JsonValueKind.Array)
                        foreach (var option in optionElements.EnumerateArray())
                            if (StringOf(option, "Name") is { } optionName)
                                options.Add(optionName);

                    if (Normalise(name) == Normalise(_options.SiteTrackingCategory)) sites = (id, options);
                    else if (Normalise(name) == Normalise(_options.CostCodeTrackingCategory)) costCodes = (id, options);
                }
            }

            if (sites is null)
                throw new XeroCallFailedException(
                    $"Xero has no tracking category named \"{_options.SiteTrackingCategory}\".");
            if (costCodes is null)
                throw new XeroCallFailedException(
                    $"Xero has no tracking category named \"{_options.CostCodeTrackingCategory}\".");

            return new TrackingCategoryLookup(sites.Value.Id, sites.Value.Options, costCodes.Value.Id, costCodes.Value.Options);
        }
    }

    /// <summary>POST/PUT with a JSON body; failures throw with Xero's validation messages extracted.</summary>
    private async Task<JsonDocument> SendJsonAsync(
        HttpMethod method, string token, string url, JsonObject body, string what, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            request.Headers.Add("xero-tenant-id", _options.TenantId);

        using var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Xero {What} call failed: {Status} {Body}.", what, (int)response.StatusCode, Truncate(responseBody));
            throw new XeroCallFailedException(
                $"Xero rejected the {what} request with HTTP {(int)response.StatusCode}. {ExtractXeroErrors(responseBody)}");
        }

        return JsonDocument.Parse(responseBody);
    }

    /// <summary>
    /// Xero's 400s bury the useful text in ValidationErrors[].Message several levels deep;
    /// pull every Message out so "Account code X is not valid" reaches the allocation page
    /// instead of a truncated JSON blob.
    /// </summary>
    private static string ExtractXeroErrors(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var messages = new List<string>();
            CollectMessages(doc.RootElement, messages);
            if (messages.Count > 0) return string.Join(" ", messages.Distinct().Take(5));
        }
        catch (JsonException) { }
        return Truncate(body);

        static void CollectMessages(JsonElement element, List<string> messages)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Name is "Message" && property.Value.ValueKind == JsonValueKind.String)
                            messages.Add(property.Value.GetString()!);
                        else
                            CollectMessages(property.Value, messages);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray()) CollectMessages(item, messages);
                    break;
            }
        }
    }

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
        Lines: ReadLines(item, accountNames),
        HasAttachments: BoolOf(item, "HasAttachments"));

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

    private static bool BoolOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

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
