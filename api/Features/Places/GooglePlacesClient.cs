using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Places;

/// <summary>
/// Text search against the Google Places API (New). Returns null on failure — callers surface a
/// friendly message rather than an exception. A page of results plus Google's nextPageToken.
/// </summary>
public interface IGooglePlacesClient
{
    Task<PlacesTextSearchPage?> SearchTextAsync(string textQuery, string? pageToken, CancellationToken ct);
}

/// <summary>One page of Places text-search hits.</summary>
public sealed record PlacesTextSearchPage(IReadOnlyList<PlaceHit> Places, string? NextPageToken);

/// <summary>The fields we request per place (Places never returns an email address).</summary>
public sealed record PlaceHit(
    string PlaceId,
    string Name,
    string Address,
    string? Phone,
    string? Website,
    double? Rating,
    int RatingCount);

/// <summary>No-op fallback when no API key is configured — the app runs, the search explains itself.</summary>
public sealed class NullGooglePlacesClient : IGooglePlacesClient
{
    public Task<PlacesTextSearchPage?> SearchTextAsync(string textQuery, string? pageToken, CancellationToken ct) =>
        Task.FromResult<PlacesTextSearchPage?>(null);
}

/// <summary>REST implementation (HttpClient + X-Goog-Api-Key header), matching the app's hand-rolled style.</summary>
public sealed class GooglePlacesClient : IGooglePlacesClient
{
    private const string SearchUrl = "https://places.googleapis.com/v1/places:searchText";

    // Field mask keeps the response (and billing SKU) to what the UI shows.
    private const string FieldMask =
        "places.id,places.displayName,places.formattedAddress,places.rating," +
        "places.userRatingCount,places.nationalPhoneNumber,places.websiteUri,nextPageToken";

    private readonly HttpClient _http;
    private readonly GooglePlacesOptions _options;
    private readonly ILogger<GooglePlacesClient> _logger;

    public GooglePlacesClient(HttpClient http, GooglePlacesOptions options, ILogger<GooglePlacesClient> logger)
    {
        _http = http; _options = options; _logger = logger;
    }

    public async Task<PlacesTextSearchPage?> SearchTextAsync(string textQuery, string? pageToken, CancellationToken ct)
    {
        var payload = new Dictionary<string, object?>
        {
            ["textQuery"] = textQuery,
            ["pageSize"] = _options.PageSize,
            ["regionCode"] = _options.RegionCode
        };
        if (!string.IsNullOrWhiteSpace(pageToken))
            payload["pageToken"] = pageToken;

        using var request = new HttpRequestMessage(HttpMethod.Post, SearchUrl);
        request.Content = JsonContent.Create(payload);
        request.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Places text search failed: {Status}. {Detail}",
                (int)response.StatusCode, await SafeBodyAsync(response, ct));
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var places = new List<PlaceHit>();
        if (root.TryGetProperty("places", out var placesEl) && placesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var place in placesEl.EnumerateArray())
            {
                var id = GetString(place, "id");
                if (string.IsNullOrEmpty(id)) continue;

                var name = place.TryGetProperty("displayName", out var dn)
                    ? GetString(dn, "text") ?? ""
                    : "";
                if (string.IsNullOrWhiteSpace(name)) continue;

                places.Add(new PlaceHit(
                    PlaceId: id,
                    Name: name,
                    Address: GetString(place, "formattedAddress") ?? "",
                    Phone: GetString(place, "nationalPhoneNumber"),
                    Website: GetString(place, "websiteUri"),
                    Rating: place.TryGetProperty("rating", out var rating) && rating.ValueKind == JsonValueKind.Number
                        ? rating.GetDouble()
                        : null,
                    RatingCount: place.TryGetProperty("userRatingCount", out var count) && count.ValueKind == JsonValueKind.Number
                        ? count.GetInt32()
                        : 0));
            }
        }

        return new PlacesTextSearchPage(places, GetString(root, "nextPageToken"));
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); }
        catch { return "(unreadable body)"; }
    }
}
