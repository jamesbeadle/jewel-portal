using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Estimates on a lead (2026-09-15): the EST-#### sequence, the rules on the ladder (Submitted
// needs a price; Won and Lost are history) and the timeline every event lands on.
public sealed partial class EstimateHandlerTests
{
    private const string LeadId = "lead-1";
    private const string Estimator = "estimator@jewelbb.co.uk";

    [Fact]
    public async Task Create_mintsTheNextReference_andOpensReceived()
    {
        await using var context = await ContextWithALeadAsync();
        var create = new CreateEstimateHandler(context);

        var first = await create.HandleAsync(NewEstimate(), CancellationToken.None);
        var second = await create.HandleAsync(NewEstimate(), CancellationToken.None);

        Assert.Equal("EST-0001", first.Reference);
        Assert.Equal("EST-0002", second.Reference);
        Assert.Equal(EstimateStatus.Received, first.Status);
        Assert.Null(first.SubmittedAt);
        Assert.Equal(Estimator, first.CreatedByEmail);
        var opened = await context.LeadActivities.Where(activity => activity.Kind == (int)LeadActivityKind.Estimate).ToListAsync();
        Assert.Contains(opened, activity => activity.Summary == "Estimate EST-0001 opened");
    }

    [Fact]
    public async Task Create_refusesAnUnknownLead()
    {
        await using var context = await ContextWithALeadAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CreateEstimateHandler(context).HandleAsync(NewEstimate() with { LeadId = "nobody" }, CancellationToken.None));
    }

    [Fact]
    public async Task Submitted_needsATotal_andStampsWhenItWent()
    {
        await using var context = await ContextWithALeadAsync();
        var estimate = await new CreateEstimateHandler(context).HandleAsync(NewEstimate(), CancellationToken.None);
        var move = new MoveEstimateStatusHandler(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            move.HandleAsync(new MoveEstimateStatus(estimate.EstimateId, EstimateStatus.Submitted, null, Estimator), CancellationToken.None));

        await new UpdateEstimateDetailsHandler(context).HandleAsync(
            new UpdateEstimateDetails(estimate.EstimateId, "Rear extension", "", null, null, 185_000m, ""), CancellationToken.None);
        var submitted = await move.HandleAsync(
            new MoveEstimateStatus(estimate.EstimateId, EstimateStatus.Submitted, "Sent by email", Estimator), CancellationToken.None);

        Assert.Equal(EstimateStatus.Submitted, submitted.Status);
        Assert.NotNull(submitted.SubmittedAt);
        Assert.True(submitted.StatusChangedAt >= estimate.StatusChangedAt);
        var timeline = await context.LeadActivities.Where(activity => activity.Kind == (int)LeadActivityKind.Estimate).ToListAsync();
        Assert.Contains(timeline, activity => activity.Summary == "Estimate EST-0001 Received → Submitted. Sent by email");
    }

    private static CreateEstimate NewEstimate() =>
        new(LeadId, "Rear extension and loft", "Studio Arch", new DateOnly(2026, 10, 1), 150_000m, null, "", Estimator);

    private static async Task<JpmsContext> ContextWithALeadAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"estimates-{Guid.NewGuid():N}").Options);
        context.Leads.Add(new LeadEntity { LeadId = LeadId, Number = 1, ContactName = "Jane Coombe", Stage = (int)LeadStage.Engaged });
        await context.SaveChangesAsync();
        return context;
    }
}
