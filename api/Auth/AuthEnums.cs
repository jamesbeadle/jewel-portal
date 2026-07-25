namespace Jewel.JPMS.Api.Auth;

/// <summary>Stored as int on UserCredentialEntity.Status.</summary>
public enum CredentialStatus
{
    Invited = 0,
    Active = 1,
    Disabled = 2
}

/// <summary>Stored as int on PasswordResetTokenEntity.Purpose.</summary>
public enum TokenPurpose
{
    Invite = 0,
    Reset = 1
}

public static class AuthLockout
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}

public static class InviteSettings
{
    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);
    public const string DefaultSenderAddress = "DoNotReply@mail.jewelbb.co.uk";
}

public static class ResetSettings
{
    /// <summary>Much shorter than an invite: a reset link reaches an inbox that already has a
    /// working account behind it, so the window in which a leaked link is useful is kept small.</summary>
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(2);

    /// <summary>How often one address may ask for a self-service reset. Stops a mailbox being
    /// flooded by someone hammering the public endpoint with another person's address.</summary>
    public static readonly TimeSpan MinimumTimeBetweenRequests = TimeSpan.FromMinutes(2);
}
