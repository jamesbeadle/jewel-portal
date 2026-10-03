namespace Jewel.JPMS.Api.Features.MailboxIntake.Sharing;

/// <summary>
/// The Azure SDK form-encodes the SAS query, writing a space as <c>+</c>, but Blob Storage
/// percent-decodes it and reads <c>+</c> as a literal plus. A signed value carrying a space — the
/// file name in the Content-Disposition override — then fails with "Signature did not match".
/// Every literal plus the SDK writes is already <c>%2B</c>, so each bare <c>+</c> is a space.
/// </summary>
internal static class SignedLinkQuery
{
    private const string FormEncodedSpace = "+";
    private const string PercentEncodedSpace = "%20";

    public static Uri WithPercentEncodedSpaces(Uri signedLink)
    {
        var link = new UriBuilder(signedLink);
        var query = link.Query.TrimStart('?');
        link.Query = query.Replace(FormEncodedSpace, PercentEncodedSpace);
        return link.Uri;
    }
}
