using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The imagine form's second tick (data protection, 2026-09-21): a lead that has never been asked
// may still be followed up on the enquiry it made; one that said "keep in touch" is Given; one
// that asked us to stop is Withdrawn and no proposal goes out — until they tick again.
public sealed class LeadMarketingConsentTests
{
    [Fact]
    public void TheLaterStamp_wins()
    {
        var lead = new LeadEntity { LeadId = "lead-1" };
        Assert.Equal(LeadMarketingConsent.NotRecorded, LeadMarketingConsents.Of(lead));
        Assert.True(LeadMarketingConsents.Of(lead).AllowsAFollowUp());

        LeadMarketingConsents.RecordGiven(lead, new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
        Assert.Equal(LeadMarketingConsent.Given, LeadMarketingConsents.Of(lead));

        LeadMarketingConsents.RecordWithdrawn(lead, new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero));
        Assert.Equal(LeadMarketingConsent.Withdrawn, LeadMarketingConsents.Of(lead));
        Assert.False(LeadMarketingConsents.Of(lead).AllowsAFollowUp());

        LeadMarketingConsents.RecordGiven(lead, new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero));
        Assert.Equal(LeadMarketingConsent.Given, LeadMarketingConsents.Of(lead));
    }

    [Fact]
    public async Task Withdrawing_stampsTheLead_writesTheTimeline_andIsIdempotent()
    {
        await using var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"consent-{Guid.NewGuid():N}").Options);
        context.Leads.Add(new LeadEntity { LeadId = "lead-1", ContactEmail = "mary@example.com", MarketingConsentGivenAt = DateTimeOffset.UtcNow.AddDays(-3) });
        await context.SaveChangesAsync();
        var handler = new WithdrawLeadMarketingConsentHandler(context);

        var lead = await handler.HandleAsync(new WithdrawLeadMarketingConsent("lead-1", "sales@jewelbb.co.uk"), CancellationToken.None);
        await handler.HandleAsync(new WithdrawLeadMarketingConsent("lead-1", "sales@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal(LeadMarketingConsent.Withdrawn, lead.MarketingConsent);
        Assert.Equal(1, await context.LeadActivities.CountAsync());
    }
}
