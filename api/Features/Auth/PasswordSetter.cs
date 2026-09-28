using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Connect;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// Gives a login a new password and ends everything opened under the old one. The credential takes
/// the hash and becomes Active — an invited user who never chose a password can now sign in —
/// every invite or reset link still live for the address is voided, and every session and
/// connected AI tool the person had is ended: a password changing hands is the answer to "someone
/// else may have my password", so nothing opened before it survives. Shared by the set-password
/// link and an administrator setting one by hand (SetUserPassword), so both leave the login in
/// exactly the same state.
/// </summary>
public sealed class PasswordSetter
{
    private readonly JpmsContext context;
    private readonly OAuthTokenManager tokens;
    private readonly SignedInUserCache userCache;

    public PasswordSetter(JpmsContext context, OAuthTokenManager tokens, SignedInUserCache userCache)
    {
        this.context = context;
        this.tokens = tokens;
        this.userCache = userCache;
    }

    public async Task SetAsync(string email, string password, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var credential = await CredentialForAsync(email, now, cancellationToken);
        ActivateWith(credential, password, now);
        await PasswordLinks.VoidEveryLiveLinkAsync(context, email, now, cancellationToken);
        await RevokeLiveSessionsAsync(email, now, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tokens.RevokeAllForUserAsync(email, cancellationToken);
        userCache.InvalidateEmail(email);
    }

    private async Task<UserCredentialEntity> CredentialForAsync(
        string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (credential is not null) return credential;

        credential = new UserCredentialEntity { Email = email, CreatedAt = now };
        context.UserCredentials.Add(credential);
        return credential;
    }

    private static void ActivateWith(UserCredentialEntity credential, string password, DateTimeOffset now)
    {
        credential.PasswordHash = PasswordHasher.Hash(password);
        credential.Status = (int)CredentialStatus.Active;
        credential.PasswordSetAt = now;
        credential.FailedAttempts = 0;
        credential.LockedUntil = null;
    }

    private async Task RevokeLiveSessionsAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var liveSessions = await context.UserSessions
            .Where(row => row.Email == email && row.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in liveSessions) session.RevokedAt = now;
    }
}
