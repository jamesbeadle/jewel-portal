using System.Security.Cryptography;
using System.Text;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Generates cryptographically random secrets (for invite links and session cookies) and
/// the SHA-256 hashes that are persisted. The raw secret is returned to the caller exactly
/// once; only its hash is ever stored, so the database alone cannot reconstruct a live link
/// or session.
/// </summary>
public static class AuthTokens
{
    private const int SecretBytes = 32;

    /// <summary>A new URL-safe random secret to embed in a link or cookie.</summary>
    public static string NewSecret() =>
        Base64Url(RandomNumberGenerator.GetBytes(SecretBytes));

    /// <summary>SHA-256 (lowercase hex) of a secret — the value stored and looked up by.</summary>
    public static string Hash(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
