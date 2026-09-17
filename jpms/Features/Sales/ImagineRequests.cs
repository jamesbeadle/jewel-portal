using System.Net.Http.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Sales;

/// <summary>What a post to the public imagine API answered: the prospect's page as it now stands, or why not.</summary>
public sealed record ImaginePostOutcome(ImagineView? View, string? Error);

/// <summary>The public imagine API as the prospect's page talks to it — a raw HttpClient, no sign-in.</summary>
public static class ImagineRequests
{
    private const string NoReasonGiven = "Something went wrong — please try again.";
    private const string ServerUnreachable = "We couldn't reach the server — check your connection and try again.";

    public static string BaseFor(string token) => $"/api/imagine/{Uri.EscapeDataString(token)}";

    public static string ImageUrl(string token, string imageId) => $"{BaseFor(token)}/images/{imageId}";

    public static async Task<ImaginePostOutcome> PostAsync<T>(HttpClient http, string url, T body)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(url, body);
            if (response.IsSuccessStatusCode)
                return new ImaginePostOutcome(await response.Content.ReadFromJsonAsync<ImagineView>(), null);
            var message = await response.Content.ReadAsStringAsync();
            return new ImaginePostOutcome(null, string.IsNullOrWhiteSpace(message) ? NoReasonGiven : message.Trim().Trim('"'));
        }
        catch
        {
            return new ImaginePostOutcome(null, ServerUnreachable);
        }
    }
}
