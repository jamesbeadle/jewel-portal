using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// Local username/password credential for a directory user. One row per email.
/// The password is never stored in plaintext — only a PBKDF2 hash (see PasswordHasher).
/// </summary>
public sealed class UserCredentialEntity
{
    [Key, MaxLength(256)] public string Email { get; set; } = "";

    /// <summary>Null until the user has completed the "set your password" step.</summary>
    [MaxLength(512)] public string? PasswordHash { get; set; }

    /// <summary>0 = Invited (no password yet), 1 = Active, 2 = Disabled.</summary>
    public int Status { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PasswordSetAt { get; set; }
}

/// <summary>
/// Single-use token backing the "set your password" invite link and future password resets.
/// Only the SHA-256 hash of the secret is stored; the raw secret lives only in the emailed link.
/// </summary>
public sealed class PasswordResetTokenEntity
{
    /// <summary>SHA-256 (hex) of the secret carried in the link. Primary key + lookup value.</summary>
    [Key, MaxLength(128)] public string TokenHash { get; set; } = "";

    [MaxLength(256)] public string Email { get; set; } = "";

    /// <summary>0 = Invite, 1 = Reset.</summary>
    public int Purpose { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }
}

/// <summary>
/// Server-side session backing the HTTP-only session cookie. Only the SHA-256 hash of the
/// cookie secret is stored, so a database leak cannot be replayed as a live session.
/// </summary>
public sealed class UserSessionEntity
{
    /// <summary>SHA-256 (hex) of the cookie secret. Primary key + lookup value.</summary>
    [Key, MaxLength(128)] public string SessionId { get; set; } = "";

    [MaxLength(256)] public string Email { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
