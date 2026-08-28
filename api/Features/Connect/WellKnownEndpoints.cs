using Jewel.JPMS.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// The OAuth discovery documents (RFC 8414 / RFC 9728) that let an AI tool find the whole flow
/// from the MCP URL alone. Served under /api/well-known/… and surfaced at the standard
/// /.well-known/… paths by rewrites in staticwebapp.config.json — Static Web Apps reserves the
/// site root for the SPA, so the functions cannot claim those paths directly.
/// </summary>
public sealed class WellKnownEndpoints
{
    private readonly IConfiguration configuration;

    public WellKnownEndpoints(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <summary>What triggers the sign-in flow: the MCP endpoint's 401 points here, and this
    /// points at the authorisation server (the site itself).</summary>
    [Function("OAuthProtectedResourceMetadata")]
    public IActionResult ProtectedResource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "well-known/oauth-protected-resource/{*rest}")] HttpRequest request)
    {
        var site = SiteBaseUrl.Resolve(configuration, request);
        return Json(new Dictionary<string, object>
        {
            ["resource"] = $"{site}/api/mcp",
            ["authorization_servers"] = new[] { site },
            ["bearer_methods_supported"] = new[] { "header" },
            ["scopes_supported"] = new[] { OAuthDefaults.Scope }
        });
    }

    [Function("OAuthAuthorizationServerMetadata")]
    public IActionResult AuthorizationServer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "well-known/oauth-authorization-server/{*rest}")] HttpRequest request)
    {
        var site = SiteBaseUrl.Resolve(configuration, request);
        return Json(new Dictionary<string, object>
        {
            ["issuer"] = site,
            ["authorization_endpoint"] = $"{site}/api/oauth/authorize",
            ["token_endpoint"] = $"{site}/api/oauth/token",
            ["registration_endpoint"] = $"{site}/api/oauth/register",
            ["response_types_supported"] = new[] { "code" },
            ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none" },
            ["scopes_supported"] = new[] { OAuthDefaults.Scope }
        });
    }

    private static OkObjectResult Json(object payload) => new(payload);
}
