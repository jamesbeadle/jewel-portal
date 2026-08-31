using System.Security.Cryptography;
using System.Text;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// The OAuth state parameter for the connect flow, stateless so it survives function instances:
/// "nonce|unix-expiry|HMAC-SHA256(nonce|expiry, client secret)", base64url-encoded, valid for ten
/// minutes. The callback verifies the signature and the clock — nothing is stored, and a state
/// minted by anyone without the client secret fails the comparison.
/// </summary>
public static class BluebeamConnectionState
{
    private static readonly TimeSpan Validity = TimeSpan.FromMinutes(10);

    public static string Mint(string clientSecret, string adminEmail)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var expiresAtUnix = DateTimeOffset.UtcNow.Add(Validity).ToUnixTimeSeconds();
        var email = Encode(adminEmail);
        var signature = Sign($"{nonce}|{expiresAtUnix}|{email}", clientSecret);
        return Encode($"{nonce}|{expiresAtUnix}|{email}|{signature}");
    }

    /// <summary>The admin email carried in a valid state, or null when the state fails the
    /// signature or the clock — null is the callback's cue to refuse.</summary>
    public static string? VerifiedAdminEmail(string? state, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;
        string decoded;
        try { decoded = Decode(state); }
        catch (FormatException) { return null; }

        var parts = decoded.Split('|');
        if (parts.Length != 4) return null;
        if (!long.TryParse(parts[1], out var expiresAtUnix)) return null;
        if (DateTimeOffset.UtcNow > DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix)) return null;

        var expected = Sign($"{parts[0]}|{parts[1]}|{parts[2]}", clientSecret);
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[3]));
        if (!matches) return null;

        try { return Decode(parts[2]); }
        catch (FormatException) { return null; }
    }

    private static string Sign(string payload, string clientSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
