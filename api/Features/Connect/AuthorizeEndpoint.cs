using System.Text.Json;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// GET /api/oauth/authorize — the front door of the connect flow. Validates the request and hands
/// the browser to the SPA consent page (<c>/connect/authorize</c>), which shows who is asking and
/// lets the signed-in user approve. Splitting it this way keeps this endpoint free of any UI and
/// lets the consent page reuse the portal's own sign-in.
///
/// <para>Error handling follows RFC 6749 §4.1.2.1: an unknown client or an unregistered
/// redirect_uri gets a plain 400 (never a redirect — that would turn the endpoint into an open
/// redirector); every other problem redirects back to the client with an error code.</para>
/// </summary>
public sealed class AuthorizeEndpoint
{
    private readonly JpmsContext context;
    private readonly IConfiguration configuration;

    public AuthorizeEndpoint(JpmsContext context, IConfiguration configuration)
    {
        this.context = context;
        this.configuration = configuration;
    }

    [Function("OAuthAuthorize")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "oauth/authorize")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var query = request.Query;

        var clientId = query["client_id"].ToString();
        var redirectUri = query["redirect_uri"].ToString();

        var client = string.IsNullOrEmpty(clientId)
            ? null
            : await context.OAuthClients.AsNoTracking()
                .FirstOrDefaultAsync(row => row.ClientId == clientId, cancellationToken);
        if (client is null)
            return new BadRequestObjectResult("Unknown client_id. Register the client first (POST /api/oauth/register).");

        var registeredUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? new List<string>();
        if (!registeredUris.Contains(redirectUri, StringComparer.Ordinal))
            return new BadRequestObjectResult("redirect_uri is not registered for this client.");

        var state = query["state"].ToString();

        if (query["response_type"].ToString() != "code")
            return ErrorRedirect(redirectUri, "unsupported_response_type", state);

        var codeChallenge = query["code_challenge"].ToString();
        var challengeMethod = query["code_challenge_method"].ToString();
        if (string.IsNullOrEmpty(codeChallenge) || challengeMethod != "S256")
            return ErrorRedirect(redirectUri, "invalid_request", state,
                "PKCE with code_challenge_method=S256 is required.");

        // Everything checks out — hand over to the consent page with the request intact. The
        // approve endpoint re-validates against the database; nothing here is trusted later.
        var site = SiteBaseUrl.Resolve(configuration, request);
        var consent = new QueryBuilder
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "state", state },
            { "code_challenge", codeChallenge },
            { "scope", string.IsNullOrEmpty(query["scope"].ToString()) ? OAuthDefaults.Scope : query["scope"].ToString() },
            { "resource", query["resource"].ToString() }
        };
        return new RedirectResult($"{site}/connect/authorize{consent}");
    }

    private static RedirectResult ErrorRedirect(string redirectUri, string error, string state, string? description = null)
    {
        var query = new QueryBuilder { { "error", error } };
        if (!string.IsNullOrEmpty(description)) query.Add("error_description", description);
        if (!string.IsNullOrEmpty(state)) query.Add("state", state);
        return new RedirectResult($"{redirectUri}{query}");
    }
}
