using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Creates and validates server-side sessions. The raw secret is returned only when a session
/// is created (to be placed in the cookie); thereafter sessions are looked up by the SHA-256
/// hash of the presented cookie value.
/// </summary>
public sealed class SessionManager
{
    private readonly JpmsContext context;

    public SessionManager(JpmsContext context) { this.context = context; }

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
    {
        var sessionId = AuthTokens.Hash(secret);
        var now = DateTimeOffset.UtcNow;
        var session = await context.UserSessions
            .FirstOrDefaultAsync(row => row.SessionId == sessionId, cancellationToken);
        if (session is null) return null;
        if (session.RevokedAt is not null) return null;
        if (session.ExpiresAt <= now) return null;
        return session.Email;
    }

    /// <summary>Revokes a session by its cookie secret (used on logout). Safe if already gone.</summary>
    public async Task RevokeAsync(string secret, CancellationToken cancellationToken)
    {
        var sessionId = AuthTokens.Hash(secret);
        var session = await context.UserSessions
            .FirstOrDefaultAsync(row => row.SessionId == sessionId, cancellationToken);
        if (session is null || session.RevokedAt is not null) return;
        session.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}
