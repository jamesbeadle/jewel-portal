using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    // -- "Cost Code" tracking options, written deliberately (2026-09-03) ------------------
    //
    // The approval and §6a coding paths create a missing option lazily as a side effect of a
    // bill line needing it (XeroClient.Writes.cs). These two are the explicit versions the Cost
    // codes page and the connector call: create by name, rename by id. Both drop the cached
    // tracking lookups so the next read — the page's refresh, the coding run's existence check —
    // sees Xero's new state rather than a stale "missing". Nothing here deletes or archives.

    public async Task<string> CreateCostCodeOptionAsync(string optionName, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new XeroCallFailedException("Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

        var token = await GetAccessTokenAsync(ct);
        var categories = await GetTrackingCategoriesAsync(token, ct);
        var body = new JsonObject { ["Name"] = optionName };
        using var doc = await SendJsonAsync(HttpMethod.Put, token,
            $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options",
            body, $"create Cost Code option {optionName}", ct);

        categories.CostCodeOptions.Add(optionName);
        InvalidateTrackingCaches();
        return CreatedOptionId(doc) ?? "";
    }

    public async Task<string> RenameCostCodeOptionAsync(string trackingOptionId, string newName, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            throw new XeroCallFailedException("Xero isn't connected — add the Xero__ClientId / Xero__ClientSecret app settings.");

        var token = await GetAccessTokenAsync(ct);
        var categories = await GetTrackingCategoriesAsync(token, ct);
        var body = new JsonObject { ["Name"] = newName };
        using var doc = await SendJsonAsync(HttpMethod.Post, token,
            $"{TrackingCategoriesUrl}/{categories.CostCodeCategoryId}/Options/{trackingOptionId}",
            body, $"rename Cost Code option to {newName}", ct);

        InvalidateTrackingCaches();
        return CreatedOptionId(doc) ?? trackingOptionId;
    }

    /// <summary>Xero echoes the written option back under Options[0].TrackingOptionID.</summary>
    private static string? CreatedOptionId(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("Options", out var options)
            && options.ValueKind == JsonValueKind.Array && options.GetArrayLength() > 0)
            return StringOf(options[0], "TrackingOptionID");
        return null;
    }

    private void InvalidateTrackingCaches()
    {
        _trackingLookup = null;
        _trackingLookupAt = DateTimeOffset.MinValue;
        _cachedTrackingCategories = null;
        _cachedTrackingCategoriesAt = DateTimeOffset.MinValue;
    }
}
