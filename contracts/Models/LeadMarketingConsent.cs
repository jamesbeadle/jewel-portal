namespace Jewel.JPMS.Models;

/// <summary>
/// Whether a prospect has said Jewel may keep in touch about their home beyond the concepts they
/// asked for — the second tick on the imagine form, held apart from the service consent the
/// concepts need. NotRecorded is every lead captured before the question was asked (a letter, a
/// forwarded enquiry): a follow-up on the enquiry they made is still a fair reply to it, so only
/// Withdrawn stops one. Values persist as ints — append only.
/// </summary>
public enum LeadMarketingConsent
{
    NotRecorded = 0,
    Given = 1,
    Withdrawn = 2
}

public static class LeadMarketingConsentExtensions
{
    public static bool AllowsAFollowUp(this LeadMarketingConsent consent) => consent != LeadMarketingConsent.Withdrawn;

    public static string DisplayName(this LeadMarketingConsent consent) => consent switch
    {
        LeadMarketingConsent.Given => "Keep in touch",
        LeadMarketingConsent.Withdrawn => "Asked us to stop",
        _ => "Not asked"
    };
}
