using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// Creates or refreshes an invited user, mints a single-use set-password link and emails it. The
/// link is also returned so an admin can relay it by hand. A failed email never fails the invite,
/// because the link is still handed back.
/// </summary>
public sealed class UserInviter
{
    private readonly JpmsContext context;
    private readonly InviteDirectoryWriter directory;
    private readonly IInviteNotifier notifier;
    private readonly ILogger<UserInviter> logger;

    public UserInviter(JpmsContext context, InviteDirectoryWriter directory, IInviteNotifier notifier, ILogger<UserInviter> logger)
    {
        this.context = context;
        this.directory = directory;
        this.notifier = notifier;
        this.logger = logger;
    }

    public async Task<InviteResult> InviteAsync(
        string email, string displayName, IReadOnlyList<Role> roles, string baseUrl, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await directory.PrepareAsync(email, displayName, roles, now, cancellationToken);

        var secret = AuthTokens.NewSecret();
        var expiresAt = now.Add(InviteSettings.InviteLifetime);
        context.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            TokenHash = AuthTokens.Hash(secret),
            Email = email,
            Purpose = (int)TokenPurpose.Invite,
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await context.SaveChangesAsync(cancellationToken);

        var inviteLink = $"{baseUrl}/set-password?token={secret}";
        await TryEmailAsync(email, displayName, inviteLink, cancellationToken);
        return new InviteResult(email, displayName, inviteLink, expiresAt);
    }

    private async Task TryEmailAsync(string email, string displayName, string inviteLink, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.SendInviteAsync(email, displayName, inviteLink, cancellationToken);
        }
        catch (Exception emailError)
        {
            logger.LogError(emailError, "Could not email the invite to {Email}; the link is still available to the admin.", email);
        }
    }
}
