namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Minimum password strength rules enforced when a user sets or resets a password.
/// Length 12–128, with at least one lowercase letter, one uppercase letter and one digit.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static string Requirements =>
        $"Use {MinLength}–{MaxLength} characters, including an uppercase letter, a lowercase letter and a number.";

    public static bool IsAcceptable(string? password) => Validate(password) is null;

    /// <summary>Returns null when the password is acceptable, otherwise a human-readable reason.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password)) return "Enter a password.";
        if (password.Length < MinLength) return $"Password must be at least {MinLength} characters.";
        if (password.Length > MaxLength) return $"Password must be {MaxLength} characters or fewer.";
        if (!password.Any(char.IsLower)) return "Password must contain a lowercase letter.";
        if (!password.Any(char.IsUpper)) return "Password must contain an uppercase letter.";
        if (!password.Any(char.IsDigit)) return "Password must contain a number.";
        return null;
    }
}
