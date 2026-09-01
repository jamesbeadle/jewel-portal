using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Creates and validates server-side sessions. The raw secret is returned only when a session
/// is created (to be placed in the cookie); thereafter sessions are looked up by the SHA-256
/// hash of the presented cookie value.
/// </summary>
public sealed class SessionManager
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public SessionManager(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    /// <summary>Creates a session for the email and returns the raw secret for the cookie.</summary>
    public async Task<string> CreateAsync(string email, CancellationToken cancellationToken)
    {
        var secret = AuthTokens.NewSecret();
        var now = DateTimeOffset.UtcNow;
        context.UserSessions.Add(new UserSessionEntity
        {
            SessionId = AuthTokens.Hash(secret),
            Email = email,
            CreatedAt = now,
            ExpiresAt = now.Add(SessionCookie.Lifetime)
        });
        await context.SaveChangesAsync(cancellationToken);
        return secret;
    }

    /// <summary>Returns the email for a valid (unexpired, unrevoked) session secret, else null.</summary>
    public async Task<string?> ResolveEmailAsync(string secret, CancellationToken cancellationToken)
        => (await ResolveAsync(secret, cancellationToken))?.Email;

    /// <summary>The email and expiry of a valid (unexpired, unrevoked) session, else null. The
    /// expiry is returned so callers that cache the resolved user can bound the cached entry by
    /// the session's own lifetime — see SignedInUserCache.</summary>
    public async Task<ResolvedSession?> ResolveAsync(string secret, CancellationToken cancellationToken)
    {
        var sessionId = AuthTokens.Hash(secret);
        var now = DateTimeOffset.UtcNow;
        var session = await context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.SessionId == sessionId, cancellationToken);
        if (session is null) return null;
        if (session.RevokedAt is not null) return null;
        if (session.ExpiresAt <= now) return null;
        return new ResolvedSession(sessionId, session.Email, session.ExpiresAt);
    }

    /// <summary>Revokes a session by its cookie secret (used on logout). Safe if already gone.</summary>
    public async Task RevokeAsync(string secret, CancellationToken cancellationToken)
    {
        var sessionId = AuthTokens.Hash(secret);
        // Drop the cached caller first, so a revoked cookie stops working immediately on this
        // instance rather than at the end of the cache TTL.
        userCache.RemoveSession(sessionId);
        var session = await context.UserSessions
            .FirstOrDefaultAsync(row => row.SessionId == sessionId, cancellationToken);
        if (session is null || session.RevokedAt is not null) return;
        session.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>A validated session: the hashed cookie it was looked up by, whose it is, and when it
/// lapses.</summary>
public sealed record ResolvedSession(string SessionId, string Email, DateTimeOffset ExpiresAt);
