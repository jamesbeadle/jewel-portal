namespace Jewel.JPMS.Models;

public static class JpmsAdministrators
{
    private static readonly IReadOnlySet<string> Emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Master administrators. These accounts always have every role and can create/invite
        // other users, regardless of what is stored in the directory. To add another permanent
        // admin (e.g. Nigel's working address), add their email here.
        "james.beadle@jewelbb.co.uk",
        "admin.james@jewelenterprises.co.uk",
        "Nigel.Reilly@jewelenterprises.co.uk"
    };

    public static bool Contains(string email) =>
        !string.IsNullOrWhiteSpace(email) && Emails.Contains(email.Trim());
}
