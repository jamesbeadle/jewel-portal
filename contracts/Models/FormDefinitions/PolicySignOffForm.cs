using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// Policy sign-off (2026-09-24, the FD's task from Jeremy): one published policy revision, chosen by
/// whoever sends the link, read and signed by anyone — an operative or a subcontractor with no portal
/// login as much as a member of staff. The policy and its declaration are fixed by the sender and
/// stamped again by the api from the invite, so the signature is recorded against that exact revision.
/// Filed under the person; not in the new starter pack unless the office ticks it in.
/// </summary>
public static class PolicySignOffForm
{
    public const string PolicyKey = "policy";
    public const string DeclarationKey = "declaration";
    public const string ReadKey = "read";
    public const string NameKey = "name";
    public const string CompanyKey = "company";
    public const string PositionKey = "position";
    public const string SignatureKey = "signature";

    public static readonly FormDefinition Definition = new(
        FormSlugs.PolicySignOff,
        "Policy Sign-off",
        "Please open the policy below and read it in full, then sign to confirm you have read it and will comply "
            + "with it. Your signature is recorded against this revision of the policy. Fields marked * are required.",
        new[]
        {
            Fixed(PolicyKey, "Policy and revision"),
            Fixed(DeclarationKey, "Declaration"),
            Declaration(ReadKey, "I have read the policy", Required),
            Text(NameKey, "Full name", Required),
            Text(CompanyKey, "Company", Optional, "For a subcontractor, the company you work for."),
            Text(PositionKey, "Position in company", Required),
            Signature(SignatureKey, "Signature", Required, "Signing confirms the declaration above.")
        },
        IsSentByLinkOnly: true);
}

/// <summary>What a person signs to: the policy's own declaration when its publisher wrote one, else the standard line.</summary>
public static class PolicyDeclarations
{
    public const string Standard = "I have read and understood this policy and will comply with it";

    public static string Of(string written) => string.IsNullOrWhiteSpace(written) ? Standard : written.Trim();

    public static string PolicyLine(string title, int revision) => $"{title} — revision {revision}";
}
