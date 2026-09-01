using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
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
        HashSet<string> CostCodeOptions,
        // Option name → TrackingOptionID for the Sites category — the P&L report is filtered
        // by option ID, not name. Same case-insensitive matching as SiteOptions.
        IReadOnlyDictionary<string, string> SiteOptionIdsByName);

    /// <summary>
    /// Reads the organisation's tracking categories and finds the Sites and Cost Code ones
    /// by their configured names (spacing/case tolerant). Requires the custom connection's
    /// accounting.settings scope — the failure message says so when Xero refuses.
    /// </summary>
    private async Task<TrackingCategoryLookup> GetTrackingCategoriesAsync(string token, CancellationToken ct)
    {
        if (_trackingLookup is not null
            && DateTimeOffset.UtcNow < _trackingLookupAt.AddMinutes(_options.CacheMinutes))
            return _trackingLookup;

        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, TrackingCategoriesUrl, "tracking categories", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            // Only a 403 is a scope problem. Anything else (rate limit, outage) keeps its own
            // story — a 429 dressed up as "missing scope" sends whoever reads it hunting the
            // Xero portal for a setting that is already ticked.
            throw new XeroCallFailedException(
                "Couldn't read Xero's tracking categories — the custom connection needs the "
                + "accounting.settings scope to confirm cost codes. " + failure.Message);
        }

        using (doc)
        {
            (string Id, HashSet<string> Options, Dictionary<string, string> OptionIds)? sites = null, costCodes = null;
            if (doc.RootElement.TryGetProperty("TrackingCategories", out var trackingCategories)
                && trackingCategories.ValueKind == JsonValueKind.Array)
            {
                foreach (var category in trackingCategories.EnumerateArray())
                {
                    var name = StringOf(category, "Name");
                    var id = StringOf(category, "TrackingCategoryID");
                    if (name is null || id is null) continue;

                    var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var optionIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (category.TryGetProperty("Options", out var optionElements) && optionElements.ValueKind == JsonValueKind.Array)
                        foreach (var option in optionElements.EnumerateArray())
                            if (StringOf(option, "Name") is { } optionName)
                            {
                                options.Add(optionName);
                                if (StringOf(option, "TrackingOptionID") is { } optionId)
                                    optionIds[optionName] = optionId;
                            }

                    if (Normalise(name) == Normalise(_options.SiteTrackingCategory)) sites = (id, options, optionIds);
                    else if (Normalise(name) == Normalise(_options.CostCodeTrackingCategory)) costCodes = (id, options, optionIds);
                }
            }

            if (sites is null)
                throw new XeroCallFailedException(
                    $"Xero has no tracking category named \"{_options.SiteTrackingCategory}\".");
            if (costCodes is null)
                throw new XeroCallFailedException(
                    $"Xero has no tracking category named \"{_options.CostCodeTrackingCategory}\".");

            var lookup = new TrackingCategoryLookup(
                sites.Value.Id, sites.Value.Options, costCodes.Value.Id, costCodes.Value.Options,
                sites.Value.OptionIds);
            _trackingLookup = lookup;
            _trackingLookupAt = DateTimeOffset.UtcNow;
            return lookup;
        }
    }

    /// <summary>POST/PUT with a JSON body; failures throw with Xero's validation messages extracted.</summary>
}
