namespace Jewel.JPMS.Models;

/// <summary>
/// The portal's privacy notice, at the same origin as the link an email carries — an invite to
/// portal.jewelbb.co.uk points at portal.jewelbb.co.uk/privacy, a test site's at its own.
/// Every email that tells a person where to read what the portal holds about them (UK GDPR
/// Articles 13 and 14) says it from here: the invite and reset emails the api sends, and the
/// prospect emails whose source the worker compiles too.
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
