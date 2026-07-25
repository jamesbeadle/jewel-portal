using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// Mints a single-use password-reset link and emails it. Shared by the public "forgot password"
/// endpoint and the admin "send reset link" action, so both produce exactly the same token, the
/// same expiry and the same email.
///
/// Issuing a reset voids every other live link for that address: whoever holds the newest email is
/// the only one who can get in, and an old invite link cannot be used to race the reset.
/// </summary>
public sealed class PasswordResetSender
{
    private readonly JpmsContext context;
    private readonly IInviteNotifier notifier;
    private readonly ILogger<PasswordResetSender> logger;

    public PasswordResetSender(JpmsContext context, IInviteNotifier notifier, ILogger<PasswordResetSender> logger)
    {
        this.context = context;
        this.notifier = notifier;
        this.logger = logger;
    }

    /// <summary>Why a reset could not be sent. The public endpoint swallows every one of these and
    /// answers identically, so an anonymous caller learns nothing about the address.</summary>
    public enum Outcome
    {
        Sent,
        NoSuchAccount,
        NotYetActive,
        Disabled,
        TooSoon
    }

    public sealed record Result(Outcome Outcome, InviteResult? Reset);

    public async Task<Result> SendAsync(string email, string baseUrl, bool bypassThrottle, CancellationToken cancellationToken)
    {
        var trimmed = email.Trim();
        var now = DateTimeOffset.UtcNow;

        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == trimmed, cancellationToken);
        if (credential is null) return new Result(Outcome.NoSuchAccount, null);
        if (credential.Status == (int)CredentialStatus.Disabled) return new Result(Outcome.Disabled, null);

        // An invited user who has never chosen a password has no password to reset — their invite
        // link is the way in. An admin re-inviting them is the correct fix, so say so distinctly
        // (the public endpoint still answers neutrally).
        if (credential.Status != (int)CredentialStatus.Active || string.IsNullOrEmpty(credential.PasswordHash))
            return new Result(Outcome.NotYetActive, null);

        if (!bypassThrottle && await WasRequestedRecentlyAsync(trimmed, now, cancellationToken))
            return new Result(Outcome.TooSoon, null);

        await VoidLiveTokensAsync(trimmed, now, cancellationToken);

        var secret = AuthTokens.NewSecret();
        var expiresAt = now.Add(ResetSettings.ResetLifetime);
        context.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            TokenHash = AuthTokens.Hash(secret),
            Email = trimmed,
            Purpose = (int)TokenPurpose.Reset,
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await context.SaveChangesAsync(cancellationToken);

        var displayName = await ResolveDisplayNameAsync(trimmed, cancellationToken);
        var resetLink = $"{baseUrl}/set-password?token={secret}";
        await TryEmailAsync(trimmed, displayName, resetLink, cancellationToken);

        return new Result(Outcome.Sent, new InviteResult(trimmed, displayName, resetLink, expiresAt));
    }

    /// <summary>True when a reset link was already minted for this address inside the throttle
    /// window and is still live.</summary>
    private async Task<bool> WasRequestedRecentlyAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var since = now - ResetSettings.MinimumTimeBetweenRequests;
        return await context.PasswordResetTokens.AnyAsync(
            row => row.Email == email
                && row.Purpose == (int)TokenPurpose.Reset
                && row.ConsumedAt == null
                && row.CreatedAt > since,
            cancellationToken);
    }

    private async Task VoidLiveTokensAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var live = await context.PasswordResetTokens
            .Where(row => row.Email == email && row.ConsumedAt == null && row.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var token in live) token.ConsumedAt = now;
    }

    private async Task<string> ResolveDisplayNameAsync(string email, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        return string.IsNullOrWhiteSpace(directoryUser?.DisplayName) ? email : directoryUser!.DisplayName;
    }

    private async Task TryEmailAsync(string email, string displayName, string resetLink, CancellationToken cancellationToken)
    {
        try
        {
            await notifier.SendPasswordResetAsync(email, displayName, resetLink, cancellationToken);
        }
        catch (Exception emailError)
        {
            // Never fail the request on a mail-provider wobble: the admin path still hands the link
            // back, and the user can ask again once the provider recovers.
            logger.LogError(emailError, "Could not email the password reset for {Email}.", email);
        }
    }
}
