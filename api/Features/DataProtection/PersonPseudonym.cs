using System.Security.Cryptography;
using System.Text;

namespace Jewel.JPMS.Api.Features.DataProtection;

/// <summary>
/// The one name an erased person keeps. Derived from the email, so every stamp that named them
/// still names one and the same person — the trail can say "the same actor approved both" —
/// while nobody can turn it back into a person. The .invalid domain is reserved (RFC 2606), so
/// the address can never be delivered to or mistaken for a real one.
/// </summary>
public static class PersonPseudonym
{
    public const string ErasedName = "Erased contact";
    public const string ErasedText = "[erased]";
    private const string Domain = "erased.invalid";
    private const int HashCharacters = 12;

    public static string For(string email)
    {
        var normalised = email.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        var stem = Convert.ToHexString(hash)[..HashCharacters].ToLowerInvariant();
        return $"erased-{stem}@{Domain}";
    }

    public static bool IsOne(string? email) =>
        email is not null && email.EndsWith("@" + Domain, StringComparison.OrdinalIgnoreCase);
}
