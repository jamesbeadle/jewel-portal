using System.Security.Cryptography;
using System.Text;
using Jewel.JPMS.Api.Auth;

namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// POST /api/oauth/token — exchanges an authorisation code (with its PKCE verifier) or a refresh
/// token for a fresh access + refresh pair. Form-encoded per RFC 6749. The PKCE verifier is what
/// proves the caller is the same software that started the flow; the client secret issued at
/// registration (for connectors like Perplexity that insist on one) is verified only when the
/// client chooses to present it — by form field (client_secret_post) or Basic header — and never
/// demanded, so public clients like Claude are untouched.
/// </summary>
public sealed class TokenEndpoint
{
    private readonly JpmsContext context;
    private readonly OAuthTokenManager tokens;

    public TokenEndpoint(JpmsContext context, OAuthTokenManager tokens)
    {
        this.context = context;
        this.tokens = tokens;
    }

    [Function("OAuthToken")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oauth/token")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch { return Error("invalid_request", "The token request must be form-encoded."); }

        return form["grant_type"].ToString() switch
        {
            "authorization_code" => await ExchangeCodeAsync(request, form, cancellationToken),
            "refresh_token" => await RefreshAsync(form, cancellationToken),
            _ => Error("unsupported_grant_type", "Use authorization_code or refresh_token.")
        };
    }

    private async Task<IActionResult> ExchangeCodeAsync(HttpRequest request, IFormCollection form, CancellationToken cancellationToken)
    {
        var code = form["code"].ToString();
        var verifier = form["code_verifier"].ToString();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(verifier))
            return Error("invalid_request", "code and code_verifier are required.");

        var now = DateTimeOffset.UtcNow;
        var codeHash = AuthTokens.Hash(code);
        var row = await context.OAuthAuthCodes
            .FirstOrDefaultAsync(candidate => candidate.CodeHash == codeHash, cancellationToken);
        if (row is null || row.UsedAt is not null || row.ExpiresAt <= now)
            return Error("invalid_grant", "The authorisation code is unknown, used, or expired.");

        // One-time use: burn the code before any other check can fail and be retried.
        row.UsedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        var clientId = form["client_id"].ToString();
        if (!string.IsNullOrEmpty(clientId) && clientId != row.ClientId)
            return Error("invalid_grant", "client_id does not match the authorisation code.");

        var redirectUri = form["redirect_uri"].ToString();
        if (!string.IsNullOrEmpty(redirectUri) && redirectUri != row.RedirectUri)
            return Error("invalid_grant", "redirect_uri does not match the authorisation code.");

        if (!PkceMatches(row.CodeChallenge, verifier))
            return Error("invalid_grant", "The PKCE code_verifier does not match.");

        var client = await context.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ClientId == row.ClientId, cancellationToken);

        var presentedSecret = PresentedClientSecret(request, form);
        if (!string.IsNullOrEmpty(presentedSecret) && client?.SecretHash is not null
            && !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(AuthTokens.Hash(presentedSecret)),
                Encoding.ASCII.GetBytes(client.SecretHash)))
            return Error("invalid_client", "The client_secret does not match.");

        var minted = await tokens.MintAsync(
            row.UserEmail, row.ClientId, client?.ClientName ?? "AI tool", row.Scope, cancellationToken);
        return TokenResponse(minted, row.Scope);
    }

    private async Task<IActionResult> RefreshAsync(IFormCollection form, CancellationToken cancellationToken)
    {
        var refreshToken = form["refresh_token"].ToString();
        if (string.IsNullOrEmpty(refreshToken))
            return Error("invalid_request", "refresh_token is required.");

        var minted = await tokens.RefreshAsync(refreshToken, cancellationToken);
        if (minted is null)
            return Error("invalid_grant", "The refresh token is unknown, revoked, or expired.");
        return TokenResponse(minted, OAuthDefaults.Scope);
    }

    /// <summary>The client secret, when the client chose to send one: the client_secret form
    /// field (client_secret_post) or the password half of a Basic Authorization header
    /// (client_secret_basic). Empty when the client authenticates as a public client.</summary>
    private static string PresentedClientSecret(HttpRequest request, IFormCollection form)
    {
        var fromForm = form["client_secret"].ToString();
        if (!string.IsNullOrEmpty(fromForm)) return fromForm;

        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return "";
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            var split = decoded.IndexOf(':');
            return split < 0 ? "" : Uri.UnescapeDataString(decoded[(split + 1)..]);
        }
        catch (FormatException) { return ""; }
    }

    /// <summary>S256: challenge == BASE64URL(SHA256(verifier)) — RFC 7636 §4.6.</summary>
    private static bool PkceMatches(string challenge, string verifier)
    {
        if (verifier.Length is < 43 or > 128) return false;
        var computed = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
    }

    private static OkObjectResult TokenResponse(OAuthTokenManager.MintedTokens minted, string scope) =>
        new(new Dictionary<string, object>
        {
            ["access_token"] = minted.AccessToken,
            ["token_type"] = "Bearer",
            ["expires_in"] = minted.ExpiresInSeconds,
            ["refresh_token"] = minted.RefreshToken,
            ["scope"] = scope
        });

    private static BadRequestObjectResult Error(string error, string description) =>
        new(new Dictionary<string, string>
        {
            ["error"] = error,
            ["error_description"] = description
        });
}
