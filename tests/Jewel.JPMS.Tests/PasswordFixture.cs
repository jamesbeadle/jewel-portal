using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests;

/// <summary>One login holding everything a new password must end — an old password part way to a
/// lockout, a live session, a connected AI tool and a live reset link — and the administrator who
/// sets the new one.</summary>
internal static class PasswordFixture
{
    public const string Email = "jack.eastly@jewelbb.co.uk";
    public const string Administrator = "nigel@jewelbb.co.uk";
    public const string OldPassword = "OldPassword2025";
    public const string NewPassword = "SiteOffice2026";
    public const string OpenSession = "session-opened-before";
    public const int FailedAttemptsSoFar = 3;

    public static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"passwords-{Guid.NewGuid():N}").Options);

    public static PasswordSetter Setter(JpmsContext context, SignedInUserCache cache) =>
        new(context, new OAuthTokenManager(context), cache);

    public static AuditTrail Audit(JpmsContext context) =>
        new(context, new AuditActor { Email = Administrator }, NullLogger<AuditTrail>.Instance);

    public static async Task<JpmsContext> SeededAsync(DateTimeOffset? revokedAt = null)
    {
        var context = NewContext();
        var now = DateTimeOffset.UtcNow;
        context.DirectoryUsers.Add(new DirectoryUserEntity { Email = Email, DisplayName = "Jack Eastly", RevokedAt = revokedAt });
        context.UserCredentials.Add(new UserCredentialEntity
        {
            Email = Email, PasswordHash = PasswordHasher.Hash(OldPassword), Status = (int)CredentialStatus.Active,
            FailedAttempts = FailedAttemptsSoFar, CreatedAt = now
        });
        context.UserSessions.Add(new UserSessionEntity { SessionId = OpenSession, Email = Email, CreatedAt = now, ExpiresAt = now.AddDays(1) });
        context.OAuthTokens.Add(new OAuthTokenEntity { TokenHash = "connected-tool", UserEmail = Email, ClientId = "claude", IssuedAt = now, ExpiresAt = now.AddDays(1) });
        context.PasswordResetTokens.Add(Link("live-reset-link", TokenPurpose.Reset));
        await context.SaveChangesAsync();
        return context;
    }

    public static PasswordResetTokenEntity Link(string tokenHash, TokenPurpose purpose) => new()
    {
        TokenHash = tokenHash,
        Email = Email,
        Purpose = (int)purpose,
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.Add(ResetSettings.ResetLifetime)
    };
}
