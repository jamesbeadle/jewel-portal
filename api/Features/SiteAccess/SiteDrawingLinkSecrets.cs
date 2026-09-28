using Jewel.JPMS.Api.Auth;

namespace Jewel.JPMS.Api.Features.SiteAccess;

/// <summary>
/// The secret in a site link's URL: minted once, hashed for storage, and the one shape of URL a
/// QR code carries. Sixteen random bytes make a 22-character token that prints legibly at poster
/// size, where the invite's 32 bytes would not.
/// </summary>
public static class SiteDrawingLinkSecrets
{
    public static string NewToken() => AuthTokens.NewSecret(SiteDrawingLinkLimits.SecretBytes);

    public static string HashOf(string token) => AuthTokens.Hash(token.Trim());

    public static string UrlFor(string siteOrigin, string token) => $"{siteOrigin}/api/site/{token}";
}
