using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// The real Bluebeam client — OAuth half. One instance over one HttpClient for the process
/// (XeroClient's arrangement); the session-workflow calls live in BluebeamClient.Sessions.cs.
/// Bluebeam's token endpoint wants HTTP Basic client credentials and rotates the refresh token on
/// every use — persisting the rotated token is BluebeamTokenService's job, not ours.
/// </summary>
public sealed partial class BluebeamClient : IBluebeamClient
{
    private readonly HttpClient http;
    private readonly BluebeamOptions options;
    private readonly ILogger<BluebeamClient> logger;

    public BluebeamClient(HttpClient http, BluebeamOptions options, ILogger<BluebeamClient> logger)
    {
        this.http = http; this.options = options; this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public Task<BluebeamTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.RedirectUri
        }, cancellationToken);

    public Task<BluebeamTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        RequestTokensAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        }, cancellationToken);

    private async Task<BluebeamTokens> RequestTokensAsync(
        Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.ApiBaseUrl}/oauth2/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Bluebeam token call failed: {Status} {Body}.", (int)response.StatusCode, Trimmed(body));
            throw new BluebeamCallFailedException(
                $"Bluebeam rejected the token request with HTTP {(int)response.StatusCode}. {Trimmed(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var accessToken = ReadString(root, "access_token")
            ?? throw new BluebeamCallFailedException("Bluebeam's token response contained no access_token.");
        var refreshToken = ReadString(root, "refresh_token") ?? "";
        var expiresInSeconds = root.TryGetProperty("expires_in", out var expiry) && expiry.TryGetInt32(out var seconds)
            ? seconds
            : 3600;
        return new BluebeamTokens(accessToken, refreshToken, expiresInSeconds);
    }

    // Bluebeam's JSON casing is not something to bet the parser on — match property names
    // case-insensitively everywhere.
    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Any(name => name.Equals(property.Name, StringComparison.OrdinalIgnoreCase))) continue;
            if (property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
            if (property.Value.ValueKind == JsonValueKind.Number) return property.Value.GetRawText();
        }
        return null;
    }

    private static string Trimmed(string value) =>
        value.Length <= 300 ? value : value[..300] + "…";
}
