namespace Jewel.JPMS.Api.Features.Sales.Imagine;

public sealed partial class ImaginePublicService
{
    public async Task<ImagineView> StopKeepingInTouchAsync(string token, CancellationToken ct)
    {
        var lead = await FindLeadAsync(token, ct) ?? throw new InvalidOperationException("This link isn't valid.");
        var consent = LeadMarketingConsents.Of(lead);
        if (consent == LeadMarketingConsent.Withdrawn) return await ViewAsync(lead, ct);

        var now = DateTimeOffset.UtcNow;
        LeadMarketingConsents.RecordWithdrawn(lead, now);
        context.LeadActivities.Add(Activity(lead.LeadId,
            "Asked us to stop keeping in touch, from their imagine page. Their concepts and any open proposal are unaffected; no further follow-up goes out."));
        await context.SaveChangesAsync(ct);
        return await ViewAsync(lead, ct);
    }
}
