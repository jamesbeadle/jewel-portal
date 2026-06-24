using System.Security.Cryptography;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// PBKDF2 (HMAC-SHA256) password hashing with a per-password random salt.
///
/// Stored format (dot-separated, all ASCII):
///   v1.pbkdf2-sha256.&lt;iterations&gt;.&lt;base64 salt&gt;.&lt;base64 subkey&gt;
///
/// This is intentionally a small, explicit scheme (rather than ASP.NET Identity's binary
/// format) so the exact same hash can be reproduced byte-for-byte by an external script
/// — e.g. Python's hashlib.pbkdf2_hmac('sha256', ...) — when seeding the first admin.
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "v1.pbkdf2-sha256";
    private const int Iterations = 210_000;
    private const int SaltBytes = 16;
    private const int SubkeyBytes = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, SubkeyBytes);
        return $"{Prefix}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

        var parts = storedHash.Split('.');
        // v1 . pbkdf2-sha256 . iterations . salt . subkey  => 5 parts
        if (parts.Length != 5) return false;
        if (parts[0] != "v1" || parts[1] != "pbkdf2-sha256") return false;
        if (!int.TryParse(parts[2], out var iterations) || iterations < 1) return false;

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedSubkey = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedSubkey.Length);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
