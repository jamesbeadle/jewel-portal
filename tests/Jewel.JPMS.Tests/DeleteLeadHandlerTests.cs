using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Deleting a lead (2026-09-15, Nigel): the lead and everything hung off it go — timeline,
// estimates, proposals, imagine rounds and images — and nothing else does. A Won lead is refused:
// it has a client and a project behind it. The next capture still takes the next number, because
// the sequence is max + 1 over what is left (the same rule as every other global sequence).
public sealed class DeleteLeadHandlerTests
{
    [Fact]
    public async Task RemovesTheLead_andEverythingOnIt_butNotItsNeighbours()
    {
        await using var context = NewContext();
        var kept = await new CaptureLeadHandler(context).HandleAsync(Capture("Keep Me"), CancellationToken.None);
        var doomed = await new CaptureLeadHandler(context).HandleAsync(Capture("Delete Me"), CancellationToken.None);
        await new CreateEstimateHandler(context).HandleAsync(
            new CreateEstimate(doomed.LeadId, "Rear extension", "", null, null, null, "", "nigel@jewelbb.co.uk"), CancellationToken.None);
        await new CreateEstimateHandler(context).HandleAsync(
            new CreateEstimate(kept.LeadId, "Loft", "", null, null, null, "", "nigel@jewelbb.co.uk"), CancellationToken.None);
        context.SalesProposals.Add(new SalesProposalEntity { ProposalId = "p1", LeadId = doomed.LeadId, Version = 1 });
        context.ImagineRounds.Add(new ImagineRoundEntity { RoundId = "r1", LeadId = doomed.LeadId, Number = 1 });
        context.ImagineImages.Add(new ImagineImageEntity { ImageId = "i1", LeadId = doomed.LeadId, RoundId = "r1" });
        await context.SaveChangesAsync();
        var keptActivities = await context.LeadActivities.CountAsync(row => row.LeadId == kept.LeadId);

        var outcome = await new DeleteLeadHandler(context).HandleAsync(new DeleteLead(doomed.LeadId, "nigel@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal(doomed.LeadId, outcome.EntityId);
        Assert.False(await context.Leads.AnyAsync(row => row.LeadId == doomed.LeadId));
        Assert.False(await context.LeadActivities.AnyAsync(row => row.LeadId == doomed.LeadId));
        Assert.False(await context.LeadEstimates.AnyAsync(row => row.LeadId == doomed.LeadId));
        Assert.False(await context.SalesProposals.AnyAsync(row => row.LeadId == doomed.LeadId));
        Assert.False(await context.ImagineRounds.AnyAsync(row => row.LeadId == doomed.LeadId));
        Assert.False(await context.ImagineImages.AnyAsync(row => row.LeadId == doomed.LeadId));

        // The neighbour is untouched — its row, its timeline (capture + estimate opened) and its estimate.
        Assert.True(await context.Leads.AnyAsync(row => row.LeadId == kept.LeadId));
        Assert.Equal(keptActivities, await context.LeadActivities.CountAsync(row => row.LeadId == kept.LeadId));
        Assert.Equal(1, await context.LeadEstimates.CountAsync(row => row.LeadId == kept.LeadId));
    }

    [Fact]
    public async Task AWonLead_isRefused()
    {
        await using var context = NewContext();
        var lead = await new CaptureLeadHandler(context).HandleAsync(Capture("Won Already"), CancellationToken.None);
        var entity = await context.Leads.SingleAsync(row => row.LeadId == lead.LeadId);
        entity.Stage = (int)LeadStage.Won;
        entity.ProjectId = "proj-1";
        await context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeleteLeadHandler(context).HandleAsync(new DeleteLead(lead.LeadId, "nigel@jewelbb.co.uk"), CancellationToken.None));

        Assert.Contains("Won", refusal.Message);
        Assert.True(await context.Leads.AnyAsync(row => row.LeadId == lead.LeadId));
    }

    [Fact]
    public async Task AnUnknownLead_isRefused()
    {
        await using var context = NewContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeleteLeadHandler(context).HandleAsync(new DeleteLead("nope", "nigel@jewelbb.co.uk"), CancellationToken.None));
    }

    [Fact]
    public async Task DeletingTheNewestLead_reissuesItsNumber_deletingAnOlderOneDoesNot()
    {
        await using var context = NewContext();
        await new CaptureLeadHandler(context).HandleAsync(Capture("One"), CancellationToken.None);
        var two = await new CaptureLeadHandler(context).HandleAsync(Capture("Two"), CancellationToken.None);
        await new DeleteLeadHandler(context).HandleAsync(new DeleteLead(two.LeadId, "nigel@jewelbb.co.uk"), CancellationToken.None);

        var three = await new CaptureLeadHandler(context).HandleAsync(Capture("Three"), CancellationToken.None);

        // Max + 1 over the rows left, like every global sequence here: the newest number comes
        // back once its row is gone (LD-0003 never existed), an older gap never does. Pinned so
        // the trait is a known one — an email tagged JPMS/LD-0002 before the delete would read on
        // the new LD-0002 — not a surprise.
        Assert.Equal("LD-0002", three.Reference);

        await new DeleteLeadHandler(context).HandleAsync(new DeleteLead(three.LeadId, "nigel@jewelbb.co.uk"), CancellationToken.None);
        var one = await context.Leads.SingleAsync();
        await new CaptureLeadHandler(context).HandleAsync(Capture("Four"), CancellationToken.None);
        var four = await context.Leads.SingleAsync(row => row.LeadId != one.LeadId);
        await new DeleteLeadHandler(context).HandleAsync(new DeleteLead(one.LeadId, "nigel@jewelbb.co.uk"), CancellationToken.None);

        var five = await new CaptureLeadHandler(context).HandleAsync(Capture("Five"), CancellationToken.None);
        Assert.Equal("LD-0002", four.Reference);
        Assert.Equal("LD-0003", five.Reference);
    }

    private static CaptureLead Capture(string name) =>
        new(name, "", "", "", LeadProspectKind.Homeowner, "", "", "", "", LeadSource.Manual, null, null, "nigel@jewelbb.co.uk");

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"delete-lead-{Guid.NewGuid():N}").Options);
}
