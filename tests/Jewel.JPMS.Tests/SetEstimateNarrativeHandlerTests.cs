using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The estimate document's three texts (2026-09-18) — their own command, so the two writes cannot
// reach into each other: the details edit never blanks the narrative, and a narrative save never
// carries a stale scope, total or note back over the record.
public sealed class SetEstimateNarrativeHandlerTests
{
    private const string Estimator = "nigel@jewelbb.co.uk";

    [Fact]
    public async Task WritesTheThreeTexts_andLeavesTheDetailsAlone()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        await new UpdateEstimateDetailsHandler(context).HandleAsync(
            new UpdateEstimateDetails(estimate.EstimateId, "Loft conversion", "Prewett Bizley", null, null, 185_000m, "Chased twice"),
            CancellationToken.None);

        var written = await new SetEstimateNarrativeHandler(context).HandleAsync(
            new SetEstimateNarrative(estimate.EstimateId, "  A rear dormer loft conversion.  ", "12–14 weeks on site", "VAT; kitchen units"),
            CancellationToken.None);

        Assert.Equal("A rear dormer loft conversion.", written.ExecutiveSummary);
        Assert.Equal("12–14 weeks on site", written.BuildTime);
        Assert.Equal("VAT; kitchen units", written.Exclusions);
        Assert.Equal("Loft conversion", written.Scope);
        Assert.Equal("Prewett Bizley", written.ArchitectName);
        Assert.Equal(185_000m, written.Total);
        Assert.Equal("Chased twice", written.Notes);
    }

    [Fact]
    public async Task TheDetailsEdit_leavesTheNarrativeAlone()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        await new SetEstimateNarrativeHandler(context).HandleAsync(
            new SetEstimateNarrative(estimate.EstimateId, "Summary", "12 weeks", "VAT"), CancellationToken.None);

        var edited = await new UpdateEstimateDetailsHandler(context).HandleAsync(
            new UpdateEstimateDetails(estimate.EstimateId, "Loft and garage", "", null, null, 190_000m, ""), CancellationToken.None);

        Assert.Equal("Summary", edited.ExecutiveSummary);
        Assert.Equal("12 weeks", edited.BuildTime);
        Assert.Equal("VAT", edited.Exclusions);
    }

    // A full-record write OF THE NARRATIVE: a text sent blank, or left out of the body and so
    // arriving null, clears its field.
    [Fact]
    public async Task ATextSentBlankOrMissing_isCleared()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        var handler = new SetEstimateNarrativeHandler(context);
        await handler.HandleAsync(new SetEstimateNarrative(estimate.EstimateId, "Summary", "12 weeks", "VAT"), CancellationToken.None);

        var cleared = await handler.HandleAsync(
            new SetEstimateNarrative(estimate.EstimateId, "Summary stands", "", null!), CancellationToken.None);

        Assert.Equal("Summary stands", cleared.ExecutiveSummary);
        Assert.Equal("", cleared.BuildTime);
        Assert.Equal("", cleared.Exclusions);
    }

    [Theory]
    [InlineData(EstimateStatus.Won)]
    [InlineData(EstimateStatus.Lost)]
    public async Task AClosedEstimate_refusesTheNarrative(EstimateStatus outcome)
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        await new MoveEstimateStatusHandler(context).HandleAsync(
            new MoveEstimateStatus(estimate.EstimateId, outcome, null, Estimator), CancellationToken.None);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SetEstimateNarrativeHandler(context).HandleAsync(
                new SetEstimateNarrative(estimate.EstimateId, "Summary", "", ""), CancellationToken.None));
        Assert.Contains(outcome.ToString(), refusal.Message);
    }

    private static async Task<LeadEstimate> OpenEstimateAsync(JpmsContext context)
    {
        var lead = await new CaptureLeadHandler(context).HandleAsync(
            new CaptureLead("Julia", "", "", "", LeadProspectKind.Homeowner, "16 Ravens Dene", "BR7 5FP", "Loft", "", LeadSource.Inbound, null, null, Estimator),
            CancellationToken.None);
        return await new CreateEstimateHandler(context).HandleAsync(
            new CreateEstimate(lead.LeadId, "Loft conversion", "", null, null, 90_000m, "", Estimator), CancellationToken.None);
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"estimate-narrative-{Guid.NewGuid():N}").Options);
}
