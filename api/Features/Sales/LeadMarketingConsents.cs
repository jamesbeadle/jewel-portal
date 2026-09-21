using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>
/// The one reading of a lead's marketing consent from its two stamps: the later of "given" and
/// "withdrawn" wins, so a prospect who ticked the box, asked us to stop, and ticked it again on
/// a later round is Given. Neither stamp is NotRecorded — nobody has asked them.
/// </summary>
public static class LeadMarketingConsents
{
    public static LeadMarketingConsent Of(LeadEntity lead)
    {
        var hasNeverBeenAsked = lead.MarketingConsentGivenAt is null && lead.MarketingConsentWithdrawnAt is null;
        if (hasNeverBeenAsked) return LeadMarketingConsent.NotRecorded;
        var wasWithdrawnLast = lead.MarketingConsentWithdrawnAt >= (lead.MarketingConsentGivenAt ?? DateTimeOffset.MinValue);
        return wasWithdrawnLast ? LeadMarketingConsent.Withdrawn : LeadMarketingConsent.Given;
    }

    public static void RecordGiven(LeadEntity lead, DateTimeOffset now) => lead.MarketingConsentGivenAt = now;

    public static void RecordWithdrawn(LeadEntity lead, DateTimeOffset now) => lead.MarketingConsentWithdrawnAt = now;
}
