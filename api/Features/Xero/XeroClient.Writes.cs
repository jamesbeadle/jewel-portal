using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
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
                // The lookup is cached (see GetTrackingCategoriesAsync); record the option we
                // just created so an approval moments later doesn't try to create it again.
                categories.CostCodeOptions.Add(code);
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

    // -- §6a settlement-schedule coding: draft-only writes ------------------------------

    /// <summary>
    /// Shared validation + line building for the two §6a writes: every site option must already
    /// exist in Xero (a missing one is a mapping fault, reported loudly), and missing cost-code
    /// options are created (JPMS owns the master list, same as approval).
    /// </summary>
    private async Task<(JsonArray? Lines, string? Error)> BuildScheduleLineItemsAsync(
        string token, IReadOnlyList<XeroScheduleLine> lines, CancellationToken ct)
    {
        var categories = await GetTrackingCategoriesAsync(token, ct);

        var missingSites = lines.Select(line => line.SiteOption)
            .Where(site => !categories.SiteOptions.Contains(site))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingSites.Count > 0)
            return (null,
                $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                + " — check the site's Xero mapping against Xero's tracking options.");

        var missingCodes = lines.Select(line => line.CostCodeOption)
            .Where(code => !categories.CostCodeOptions.Contains(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var code in missingCodes)
        {
            var optionBody = new JsonObject { ["Name"] = code };
            using var _ = await SendJsonAsync(HttpMethod.Put, token,
                $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options",
                optionBody, $"create Cost Code option {code}", ct);
            categories.CostCodeOptions.Add(code);
        }

        var result = new JsonArray();
        foreach (var line in lines)
        {
            result.Add(new JsonObject
            {
                ["Description"] = line.Description,
                ["Quantity"] = 1m,
                ["UnitAmount"] = line.Net,
                ["LineAmount"] = line.Net,
                ["AccountCode"] = line.AccountCode,
                ["Tracking"] = TrackingFor(categories, line.SiteOption, line.CostCodeOption)
            });
        }
        return (result, null);
    }

    public async Task<XeroApprovalResult> RecodeDraftBillAsync(XeroDraftCodingRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        try
        {
            var token = await GetAccessTokenAsync(ct);

            using var doc = await GetJsonAsync(token, $"{InvoicesUrl}/{request.InvoiceId}", "invoices", ct);
            if (!doc.RootElement.TryGetProperty("Invoices", out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroApprovalResult.Failed("Xero returned no bill for this id — it may have been deleted.");

            var invoice = items[0];
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            // The automation only ever touches unapproved bills — approval stays human, in Xero.
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed(
                    $"The bill is {status} in Xero — the coding run only recodes DRAFT or SUBMITTED bills.");

            var (lineItems, error) = await BuildScheduleLineItemsAsync(token, request.Lines, ct);
            if (lineItems is null) return XeroApprovalResult.Failed(error!);

            var payload = new JsonObject
            {
                ["InvoiceID"] = request.InvoiceId,
                ["LineAmountTypes"] = "Exclusive",
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{InvoicesUrl}/{request.InvoiceId}", payload, "recode draft bill", ct);

            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;
            return XeroApprovalResult.Ok(status.ToUpperInvariant());
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    public async Task<XeroApprovalResult> CreateDraftBillAsync(XeroDraftBillRequest request, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return XeroApprovalResult.Failed(
                "Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");
        try
        {
            var token = await GetAccessTokenAsync(ct);

            var (lineItems, error) = await BuildScheduleLineItemsAsync(token, request.Lines, ct);
            if (lineItems is null) return XeroApprovalResult.Failed(error!);

            var payload = new JsonObject
            {
                ["Type"] = "ACCPAY",
                ["Contact"] = new JsonObject { ["Name"] = request.ContactName },
                ["Date"] = request.Date.ToString("yyyy-MM-dd"),
                ["DueDate"] = request.DueDate.ToString("yyyy-MM-dd"),
                ["Reference"] = request.Reference,
                ["Status"] = "DRAFT",
                ["LineAmountTypes"] = "Exclusive",
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Put, token, InvoicesUrl, payload, "stage draft bill", ct);

            var billId = "";
            if (response.RootElement.TryGetProperty("Invoices", out var created)
                && created.ValueKind == JsonValueKind.Array && created.GetArrayLength() > 0)
                billId = StringOf(created[0], "InvoiceID") ?? "";

            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;
            return XeroApprovalResult.Ok(billId);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    // -- site-only tracking update (SetProject half-step, no approval) -----------------

    public async Task<XeroApprovalResult> SetSiteTrackingAsync(XeroSiteTrackingRequest request, CancellationToken ct)
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

            // Fresh read, same as approval: the update below replaces the whole line list.
            using var doc = await GetJsonAsync(token, $"{baseUrl}/{request.InvoiceId}", collection.ToLowerInvariant(), ct);
            if (!doc.RootElement.TryGetProperty(collection, out var items)
                || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return XeroApprovalResult.Failed("Xero returned no invoice for this id — it may have been deleted.");

            var invoice = items[0];
            var status = StringOf(invoice, "Status") ?? "UNKNOWN";
            // Approved-but-unpaid bills accept the tracking update (decision 2026-08-14:
            // a cost moved between projects after approval must follow through to Xero's
            // Sites tracking). Paid bills are locked — Xero refuses line edits once
            // payments are applied — so those are skipped as a silent success, same as
            // the approval path: the JPMS move stands, Xero keeps its record.
            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.SkippedAlreadyApproved(status);
            if (!status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase)
                && !status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase))
                return XeroApprovalResult.Failed(
                    $"The invoice is {status} in Xero — its tracking can't be updated.");

            var categories = await GetTrackingCategoriesAsync(token, ct);

            var missingSites = request.Lines
                .Select(line => line.SiteOption)
                .Where(site => site is not null && !categories.SiteOptions.Contains(site))
                .Select(site => site!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (missingSites.Count > 0)
                return XeroApprovalResult.Failed(
                    $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                    + string.Join(", ", missingSites.Select(site => $"\"{site}\""))
                    + " — check the project's Xero site mapping against Xero's tracking options.");

            // Rebuild the full line list: every line passes through as-is except the
            // targets, whose Sites entry is replaced. No amount checks needed — nothing
            // about the money changes, and no lines are split.
            var sitesByLineId = request.Lines.ToDictionary(
                line => line.LineItemId, line => line.SiteOption, StringComparer.OrdinalIgnoreCase);
            var seenLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lineItems = new JsonArray();
            if (invoice.TryGetProperty("LineItems", out var originals) && originals.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in originals.EnumerateArray())
                {
                    var lineItemId = StringOf(line, "LineItemID");
                    if (lineItemId is not null && sitesByLineId.TryGetValue(lineItemId, out var siteOption))
                    {
                        seenLineIds.Add(lineItemId);
                        var copy = CopyLine(line, keepLineItemId: true, keepTracking: false, includeTaxAmount: true);
                        copy["Tracking"] = ReplaceSiteTracking(line, categories, siteOption);
                        lineItems.Add(copy);
                    }
                    else
                    {
                        lineItems.Add(CopyLine(line, keepLineItemId: true, keepTracking: true, includeTaxAmount: true));
                    }
                }
            }

            var unmatched = sitesByLineId.Keys.Where(id => !seenLineIds.Contains(id)).ToList();
            if (unmatched.Count > 0)
                return XeroApprovalResult.Failed(
                    "The bill's lines have changed in Xero since they were synced "
                    + $"({unmatched.Count} line(s) no longer exist). Sync from Xero and try again.");

            // No Status in the payload: the bill keeps whatever status it has (draft
            // bills stay draft, approved bills stay approved) — approval only ever
            // happens through the full write-back once every line is allocated.
            var payload = new JsonObject
            {
                [idProperty] = request.InvoiceId,
                ["LineItems"] = lineItems
            };
            using var response = await SendJsonAsync(HttpMethod.Post, token,
                $"{baseUrl}/{request.InvoiceId}", payload, "set site tracking", ct);

            // The cached snapshot now holds stale tracking for this invoice.
            _cachedSnapshot = null;
            _cachedSnapshotAt = DateTimeOffset.MinValue;

            return XeroApprovalResult.Ok(status);
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message);
        }
    }

    /// <summary>
    /// The target line's new tracking: the Sites entry replaced with <paramref name="siteOption"/>
    /// — or removed outright when it is null (unset) — with every other category
    /// (Xero's own Cost Code, anything else) carried over untouched.
    /// </summary>
    private static JsonArray ReplaceSiteTracking(JsonElement line, TrackingCategoryLookup categories, string? siteOption)
    {
        var tracking = new JsonArray();
        if (siteOption is not null)
            tracking.Add(new JsonObject { ["TrackingCategoryID"] = categories.SiteCategoryId, ["Option"] = siteOption });
        if (line.TryGetProperty("Tracking", out var existing) && existing.ValueKind == JsonValueKind.Array)
            foreach (var entry in existing.EnumerateArray())
            {
                var categoryId = StringOf(entry, "TrackingCategoryID");
                if (categoryId is not null
                    && !categoryId.Equals(categories.SiteCategoryId, StringComparison.OrdinalIgnoreCase))
                    tracking.Add(JsonNode.Parse(entry.GetRawText()));
            }
        return tracking;
    }

    // -- site P&L: the job's own monthly income and cost, as the accounts hold it ------

}
