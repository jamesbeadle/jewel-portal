using System.Security.Cryptography;
using System.Text;

namespace Jewel.JPMS.Api.Features.Forms.Links;

/// <summary>
/// The secret in a one-time link (lib/invites.js): 32 random bytes as base64url — long enough that
/// guessing is not a threat model, short enough to survive a mail client. Only the SHA-256 of it is
/// stored, so a copy of the database is not a set of working links.
/// </summary>
internal static class FormTokens
{
    private const int SecretBytes = 32;
    private const int ShortestToken = 20;
    private const int LongestToken = 64;

    public static string NewSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretBytes))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    public static bool IsWellFormed(string? token)
    {
        var length = token?.Length ?? 0;
        var isTheRightLength = length >= ShortestToken && length <= LongestToken;
        return isTheRightLength && token!.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }
}
