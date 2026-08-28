using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Resolves the signed-in user for an incoming request from the HTTP-only session cookie.
/// The principal comes from a validated session opened by the email/password login flow.
/// </summary>
public sealed class SignedInUserResolver
{
    private readonly JpmsContext context;
    private readonly SessionManager sessions;
    private readonly SignedInUserCache cache;

    public SignedInUserResolver(JpmsContext context, SessionManager sessions, SignedInUserCache cache)
    {
        this.context = context;
        this.sessions = sessions;
        this.cache = cache;
    }

    public async Task<SignedInUser?> ResolveAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var secret = SessionCookie.Read(request);
        if (secret is null) return null;

        // One round-trip to validate the session (a primary-key seek), then — on a cache hit — none
        // at all for the directory user and the role list. Those two used to run on every
        // authenticated call across ~340 endpoints, before any business data was touched.
        var session = await sessions.ResolveAsync(secret, cancellationToken);
        if (session is null || string.IsNullOrWhiteSpace(session.Email)) return null;

        var now = DateTimeOffset.UtcNow;
        if (cache.Get(session.SessionId, now) is { } cached) return cached;

        var user = await ResolveByEmailAsync(session.Email, cancellationToken);
        if (user is null) return null;
        cache.Set(session.SessionId, user, session.ExpiresAt, now);
        return user;
    }

    /// <summary>
    /// The directory identity and roles behind an already-authenticated email — the shared tail of
    /// the cookie path above and the MCP connector's bearer-token path (Features/Mcp). Null when
    /// the directory row has been revoked: however the caller authenticated, a revoked user must
    /// read as "not signed in".
    /// </summary>
    public async Task<SignedInUser?> ResolveByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        // Belt-and-braces: revocation already revokes every session and disables the credential,
        // but if a session somehow survives (e.g. created on another instance in the same
        // moment), a revoked directory row must still read as "not signed in".
        if (directoryUser?.RevokedAt is not null) return null;
        var displayName = string.IsNullOrWhiteSpace(directoryUser?.DisplayName) ? email : directoryUser!.DisplayName;
        var roles = await ResolveRolesAsync(email, cancellationToken);

        return new SignedInUser(email, displayName, roles, directoryUser?.SubcontractorId);
    }

    private async Task<IReadOnlyList<Role>> ResolveRolesAsync(string email, CancellationToken cancellationToken)
    {
        var roles = await context.DirectoryUserRoles
            .AsNoTracking()
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
        // A directory Admin role expands to EVERY role — administrators are administered in the
        // directory like anyone else (the old hard-coded JpmsAdministrators list is gone), but
        // Admin keeps its carries-every-role meaning that gates across the app rely on.
        if (roles.Contains(Role.Admin)) return Enum.GetValues<Role>();
        // Finance Directors keep their own identity: their role list stays exactly what the
        // directory assigns. Admin-equivalent permissions are granted where they matter via
        // AdminGate, not by rewriting the role list (which made the client treat FDs as
        // admins and land them on the admin dashboard). Keep in sync with the other resolver.
        return roles;
    }
}
