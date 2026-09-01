using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

public sealed partial class MailboxGraphClient
{
    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, HttpContent? content, CancellationToken ct,
        bool allowNotFound = false, bool consistencyEventual = false)
    {
        var token = await _tokens.GetTokenAsync(ct);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Immutable ids so a stored id keeps resolving; eventual consistency for $count + negated filters.
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");
        if (consistencyEventual)
            request.Headers.TryAddWithoutValidation("ConsistencyLevel", "eventual");

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && !(allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
            _logger.LogWarning("Graph {Method} {Status}.", method, (int)response.StatusCode);
        return response;
    }

    private static async Task<string> SafeBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try { return await response.Content.ReadAsStringAsync(ct); } catch { return "(no body)"; }
    }
}
