namespace Jewel.JPMS.Api.Features.Connect;

/// <summary>
/// The connector's fixed shape. One scope, code + PKCE only, public clients only — the smallest
/// OAuth 2.1 surface that Claude's and Perplexity's custom-connector flows both accept.
/// </summary>
public static class OAuthDefaults
{
    /// <summary>The only scope the connector issues. Authorisation is the signed-in user's own
    /// portal roles, applied per tool call — scopes add nothing on top, so there is exactly one.</summary>
    public const string Scope = "portal";

    /// <summary>Codes are exchanged within seconds of approval; five minutes is generous.</summary>
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>A week, matching the session cookie. The client refreshes silently after that.</summary>
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromDays(7);

    /// <summary>Ninety days without any refresh and the user signs in again.</summary>
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(90);

    public enum TokenKind
    {
        Access = 0,
        Refresh = 1
    }
}
