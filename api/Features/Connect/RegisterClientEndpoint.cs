using System.Text.Json.Serialization;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// POST /api/oauth/register — dynamic client registration (RFC 7591). Claude and Perplexity
/// register themselves here before the first sign-in, so nobody has to hand out client ids.
/// The flow is protected by PKCE + the user's own portal sign-in on the consent page; a client
/// secret is nevertheless issued (hash at rest) because Perplexity's connector hard-errors on a
/// registration response without one, even while registering as a public client. Registration is
/// deliberately open (the spec's model) — a registered client grants nothing by itself; every
/// token still requires a portal user to sign in and approve.
/// </summary>
public sealed class RegisterClientEndpoint
{
    private readonly JpmsContext context;

    public RegisterClientEndpoint(JpmsContext context)
    {
        this.context = context;
    }

    public sealed record RegistrationRequest(
        [property: JsonPropertyName("client_name")] string? ClientName,
        [property: JsonPropertyName("redirect_uris")] IReadOnlyList<string>? RedirectUris,
        [property: JsonPropertyName("token_endpoint_auth_method")] string? TokenEndpointAuthMethod);

    [Function("OAuthRegisterClient")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oauth/register")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        RegistrationRequest? body;
        try { body = await request.ReadFromJsonAsync<RegistrationRequest>(cancellationToken); }
        catch { return InvalidMetadata("The registration body is not valid JSON."); }

        if (body?.RedirectUris is null || body.RedirectUris.Count == 0)
            return InvalidMetadata("redirect_uris is required.");
        if (body.RedirectUris.Count > 10)
            return InvalidMetadata("Too many redirect_uris.");

        foreach (var uri in body.RedirectUris)
        {
            if (!OAuthRedirects.IsAcceptable(uri))
                return InvalidMetadata($"redirect_uri '{uri}' must be an absolute https URL (or http on localhost).");
        }

        var clientName = (body.ClientName ?? "").Trim();
        if (clientName.Length == 0) clientName = "AI tool";
        if (clientName.Length > 128) clientName = clientName[..128];

        var clientId = AuthTokens.NewSecret();
        var clientSecret = AuthTokens.NewSecret();
        context.OAuthClients.Add(new OAuthClientEntity
        {
            ClientId = clientId,
            SecretHash = AuthTokens.Hash(clientSecret),
            ClientName = clientName,
            RedirectUrisJson = JsonSerializer.Serialize(body.RedirectUris),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);

        // The auth method is echoed back as requested ("none" when unstated — public client).
        // The secret is returned regardless: RFC 7591 makes it optional for public clients, but
        // Perplexity refuses the response without it, and a surplus secret breaks nothing.
        var authMethod = body.TokenEndpointAuthMethod is "client_secret_post" or "client_secret_basic"
            ? body.TokenEndpointAuthMethod
            : "none";
        return new ObjectResult(new Dictionary<string, object>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["client_secret_expires_at"] = 0,
            ["client_id_issued_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["client_name"] = clientName,
            ["redirect_uris"] = body.RedirectUris,
            ["token_endpoint_auth_method"] = authMethod,
            ["grant_types"] = new[] { "authorization_code", "refresh_token" },
            ["response_types"] = new[] { "code" }
        })
        { StatusCode = StatusCodes.Status201Created };
    }

    private static BadRequestObjectResult InvalidMetadata(string detail) =>
        new(new Dictionary<string, string>
        {
            ["error"] = "invalid_client_metadata",
            ["error_description"] = detail
        });
}

/// <summary>The one redirect-URI rule, shared by registration and authorisation: absolute https
/// anywhere, or http only on loopback (Claude Code and other CLI tools listen there).</summary>
public static class OAuthRedirects
{
    public static bool IsAcceptable(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri) || uri.Length > 1024) return false;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme == Uri.UriSchemeHttps) return true;
        return parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback;
    }
}
