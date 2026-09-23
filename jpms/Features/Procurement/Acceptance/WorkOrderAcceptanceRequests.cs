using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Features.Procurement.Acceptance;

/// <summary>What a post to the public acceptance API answered: the order as it now stands, or why not.</summary>
public sealed record WorkOrderAcceptanceOutcome(WorkOrderAcceptanceView? View, string? Error);

/// <summary>The public acceptance API as the supplier's page talks to it — a raw HttpClient, no
/// sign-in, nothing through the signed-in query/command plumbing (ImagineRequests' shape).</summary>
public static class WorkOrderAcceptanceRequests
{
    private const string NoReasonGiven = "Something went wrong — please try again.";
    private const string ServerUnreachable = "We couldn't reach the server — check your connection and try again.";

    public static string UrlFor(string token) => $"/api{WorkOrderAcceptanceLink.PathFor(token)}";

    public static async Task<WorkOrderAcceptanceView?> ViewAsync(HttpClient http, string token)
    {
        try
        {
            using var response = await http.GetAsync(UrlFor(token));
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<WorkOrderAcceptanceView>();
        }
        catch { return null; }
    }

    public static async Task<WorkOrderAcceptanceOutcome> AcceptAsync(HttpClient http, string token, WorkOrderAcceptanceSignature signature)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(UrlFor(token), signature);
            if (response.IsSuccessStatusCode)
                return new WorkOrderAcceptanceOutcome(await response.Content.ReadFromJsonAsync<WorkOrderAcceptanceView>(), null);
            var message = await response.Content.ReadAsStringAsync();
            return new WorkOrderAcceptanceOutcome(null, string.IsNullOrWhiteSpace(message) ? NoReasonGiven : message.Trim().Trim('"'));
        }
        catch
        {
            return new WorkOrderAcceptanceOutcome(null, ServerUnreachable);
        }
    }
}
