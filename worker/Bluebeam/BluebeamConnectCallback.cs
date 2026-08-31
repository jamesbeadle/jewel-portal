using System.Net;
using Jewel.JPMS.Api.Features.Bluebeam;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Worker.Bluebeam;

/// <summary>
/// Where Bluebeam's consent redirect lands. It lives HERE, on the worker Function App, because
/// the portal's Static Web Apps edge intercepts any request carrying a ?code= query parameter as
/// one of its own auth callbacks and 500s it before a managed function runs — an OAuth redirect
/// can never safely land on the SWA. This is a plain Functions host, so the code arrives intact;
/// the signed ten-minute state minted by the api's Start endpoint is what proves the flow was
/// begun by a portal admin, and every outcome sends the browser back to Admin → Integrations
/// with a readable reason rather than an error page.
/// </summary>
public sealed class BluebeamConnectCallback
{
    private const string AdminPageUrl = "https://portal.jewelbb.co.uk/admin/integrations";

    private readonly BluebeamOptions options;
    private readonly IBluebeamClient client;
    private readonly BluebeamConnectionWriter writer;
    private readonly ILogger<BluebeamConnectCallback> logger;

    public BluebeamConnectCallback(
        BluebeamOptions options, IBluebeamClient client, BluebeamConnectionWriter writer,
        ILogger<BluebeamConnectCallback> logger)
    {
        this.options = options; this.client = client; this.writer = writer; this.logger = logger;
    }

    [Function("BluebeamConnectCallback")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "bluebeam/callback")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!options.IsConfigured) return Failed(request, "not-configured");

        var parameters = await ReadParametersAsync(request);
        var adminEmail = BluebeamConnectionState.VerifiedAdminEmail(
            parameters.GetValueOrDefault("state"), options.ClientSecret!);
        if (adminEmail is null) return Failed(request, "bad-state");
        var code = parameters.GetValueOrDefault("code", "");
        if (string.IsNullOrWhiteSpace(code)) return Failed(request, "no-code");

        try
        {
            var tokens = await client.ExchangeCodeAsync(code, cancellationToken);
            var connectedUser = await ReadConnectedUserAsync(tokens.AccessToken, cancellationToken);
            await writer.StoreAsync(tokens, connectedUser, adminEmail, cancellationToken);
            await writer.WriteConnectedAuditAsync(adminEmail, cancellationToken);
        }
        catch (BluebeamCallFailedException failure)
        {
            logger.LogWarning("Bluebeam code exchange failed: {Message}", failure.Message);
            return Failed(request, "exchange-failed");
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            // A browser redirect must never end on a bare error page — the person can't see logs.
            logger.LogError(failure, "Bluebeam connect callback failed after code exchange.");
            return Failed(request, "server-error");
        }

        return Redirect(request, $"{AdminPageUrl}?bluebeam=connected");
    }

    // The identity read is a nicety — a connection whose /users/me shape surprises us is still a
    // working connection, so any failure just leaves the email blank.
    private async Task<BluebeamUser> ReadConnectedUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        try { return await client.GetCurrentUserAsync(accessToken, cancellationToken); }
        catch (BluebeamCallFailedException) { return new BluebeamUser("", ""); }
    }

    // The code normally arrives in the query; a form_post response mode would put it in the body.
    // Both are read the same hand-rolled way — this host has no model binding to lean on.
    private static async Task<Dictionary<string, string>> ReadParametersAsync(HttpRequestData request)
    {
        var values = Parse(request.Url.Query);
        if (values.Count > 0) return values;
        using var reader = new StreamReader(request.Body);
        return Parse(await reader.ReadToEndAsync());
    }

    private static Dictionary<string, string> Parse(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0) continue;
            values[Uri.UnescapeDataString(pair[..separator])] =
                Uri.UnescapeDataString(pair[(separator + 1)..].Replace('+', ' '));
        }
        return values;
    }

    private static HttpResponseData Failed(HttpRequestData request, string reason) =>
        Redirect(request, $"{AdminPageUrl}?bluebeam=failed&reason={reason}");

    private static HttpResponseData Redirect(HttpRequestData request, string url)
    {
        var response = request.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", url);
        return response;
    }
}
