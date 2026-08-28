using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// An OAuth client registered to connect an AI tool (Claude, Perplexity, Claude Code…) to the
/// portal's MCP endpoint. Rows arrive through dynamic client registration (RFC 7591) — the client
/// software registers itself before the first user signs in. The flow's real protection is PKCE
/// plus the user's own portal sign-in; a client secret is issued as well, purely because some
/// connectors (Perplexity) refuse a registration response without one.
/// </summary>
public sealed class OAuthClientEntity
{
    /// <summary>Random URL-safe id handed back to the client at registration.</summary>
    [Key, MaxLength(64)] public string ClientId { get; set; } = "";

    /// <summary>SHA-256 (hex) of the client secret issued at registration — the raw secret leaves
    /// once, same rule as every other credential here. Not security-load-bearing (PKCE is): it
    /// exists because Perplexity's connector errors when registration returns no client_secret,
    /// and it is verified only when the client presents it. Null on clients registered before the
    /// column existed.</summary>
    [MaxLength(128)] public string? SecretHash { get; set; }

    /// <summary>The name the client software gave for itself ("Claude", "Perplexity"). Shown on
    /// the consent page, so it is clamped and treated as untrusted display text.</summary>
    [MaxLength(128)] public string ClientName { get; set; } = "";

    /// <summary>JSON array of the exact redirect URIs registered. Authorisation requests must
    /// match one of these exactly — no prefix or wildcard matching.</summary>
    [MaxLength(4000)] public string RedirectUrisJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A short-lived, single-use authorisation code minted when a signed-in user approves a connect
/// request on the consent page. Only the SHA-256 hash of the code is stored (same rule as
/// sessions), so the database alone cannot complete a token exchange.
/// </summary>
public sealed class OAuthAuthCodeEntity
{
    /// <summary>SHA-256 (hex) of the code returned to the client. Primary key + lookup value.</summary>
    [Key, MaxLength(128)] public string CodeHash { get; set; } = "";

    [MaxLength(64)] public string ClientId { get; set; } = "";

    /// <summary>The portal user who approved — the identity every token minted from this code
    /// carries. Set server-side from the session cookie, never from the request.</summary>
    [MaxLength(256)] public string UserEmail { get; set; } = "";

    /// <summary>The exact redirect URI the code was issued for; the token exchange must present
    /// the same one (RFC 6749 §4.1.3).</summary>
    [MaxLength(1024)] public string RedirectUri { get; set; } = "";

    /// <summary>PKCE S256 code challenge. Mandatory — plain is refused at the authorise step.</summary>
    [MaxLength(128)] public string CodeChallenge { get; set; } = "";

    [MaxLength(256)] public string Scope { get; set; } = "";

    /// <summary>RFC 8707 resource the client asked for, when it sent one. Echoed into the token
    /// row so a token can be tied to the MCP endpoint it was requested for.</summary>
    [MaxLength(512)] public string? Resource { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Stamped on first exchange. A second exchange of the same code is refused.</summary>
    public DateTimeOffset? UsedAt { get; set; }
}

/// <summary>
/// A bearer token for the MCP endpoint — access or refresh. One row per token; only the SHA-256
/// hash is stored. Every row is pinned to the user who approved the connection, which is what
/// makes MCP calls attributable in the audit trail.
/// </summary>
public sealed class OAuthTokenEntity
{
    /// <summary>SHA-256 (hex) of the bearer secret. Primary key + lookup value.</summary>
    [Key, MaxLength(128)] public string TokenHash { get; set; } = "";

    /// <summary>0 = Access, 1 = Refresh.</summary>
    public int Kind { get; set; }

    [MaxLength(256)] public string UserEmail { get; set; } = "";

    [MaxLength(64)] public string ClientId { get; set; } = "";

    /// <summary>Denormalised from the client row at mint time so the user's "connected tools"
    /// list survives a client re-registration.</summary>
    [MaxLength(128)] public string ClientName { get; set; } = "";

    [MaxLength(256)] public string Scope { get; set; } = "";

    /// <summary>Ties an access token to the refresh token that minted it (the refresh token's
    /// hash), so revoking a connection can sweep the whole family in one pass.</summary>
    [MaxLength(128)] public string? FamilyId { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Bumped at most once a minute on MCP calls — powers the "last used" column on the
    /// connected-tools list without a write per call.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
}
