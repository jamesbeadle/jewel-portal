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
