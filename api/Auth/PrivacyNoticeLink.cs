namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// The portal's privacy notice, at the same origin as the link an email carries — an invite to
/// portal.jewelbb.co.uk points at portal.jewelbb.co.uk/privacy, a test site's at its own.
/// Every email that asks a person to sign in tells them where to read what the portal will
/// hold about them (UK GDPR Articles 13 and 14).
/// </summary>
public static class PrivacyNoticeLink
{
    public const string Path = "/privacy";

    public static string Beside(string linkOnTheSameSite)
    {
        var isAbsolute = Uri.TryCreate(linkOnTheSameSite, UriKind.Absolute, out var link);
        return isAbsolute ? link!.GetLeftPart(UriPartial.Authority) + Path : Path;
    }
}
