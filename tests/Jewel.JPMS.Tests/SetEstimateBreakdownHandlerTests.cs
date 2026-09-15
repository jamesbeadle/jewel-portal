using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The estimate's priced breakdown (2026-09-15, the tender's shape): a full-record write of
// sections and lines, totals computed here, the estimate's total the sum; cost codes checked
// against the master; a closed estimate refused; the lead's timeline told.
public sealed class SetEstimateBreakdownHandlerTests
{
    [Fact]
    public async Task WritesTheSectionsAndLines_inOrder_withComputedTotals_andSetsTheEstimateTotal()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);

        var saved = await new SetEstimateBreakdownHandler(context).HandleAsync(new SetEstimateBreakdown(estimate.EstimateId, new[]
        {
            new EstimateBreakdownSection("Preliminaries & preambles", false, new[]
            {
                new EstimateBreakdownLine("SCAFF-STD", "Scaffolding including temporary roof", 420m, "m²", 44m),
                new EstimateBreakdownLine("PRELIMS-SMG", "Site manager", 20m, "week", 1_000m),
            }),
            new EstimateBreakdownSection("Drainage", true, new[]
            {
                new EstimateBreakdownLine("", "Excavate & lay new underground drainage runs", 1m, "item", 5_000m),
            }),
        }, "nigel@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal(43_480m, saved.Total);
        var sections = saved.Sections;
        Assert.Equal(2, sections.Count);
        Assert.Equal("Preliminaries & preambles", sections[0].Name);
        Assert.False(sections[0].Provisional);
        Assert.Equal(38_480m, sections[0].Total);
        Assert.Equal(18_480m, sections[0].Lines[0].Total);
        Assert.True(sections[1].Provisional);
        Assert.Equal("", sections[1].Lines[0].CostCode);

        var rows = await context.LeadEstimateLines.Where(row => row.EstimateId == estimate.EstimateId).OrderBy(row => row.SectionOrder).ThenBy(row => row.SortOrder).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { 0, 0, 1 }, rows.Select(row => row.SectionOrder));
        Assert.Equal(new[] { 0, 1, 0 }, rows.Select(row => row.SortOrder));
        Assert.Equal(20_000m, rows[1].Total);

        var timeline = await context.LeadActivities.Where(row => row.LeadId == estimate.LeadId).OrderByDescending(row => row.OccurredAt).FirstAsync();
        Assert.Contains("breakdown set", timeline.Summary);
        Assert.Contains("2 sections", timeline.Summary);
        Assert.Contains("3 lines", timeline.Summary);
    }

    [Fact]
    public async Task ASecondWrite_replacesTheFirstWhole_andAnEmptyOne_clearsIt_leavingTheTotal()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        var handler = new SetEstimateBreakdownHandler(context);
        await handler.HandleAsync(Breakdown(estimate.EstimateId, ("A", 100m), ("B", 200m)), CancellationToken.None);

        var second = await handler.HandleAsync(Breakdown(estimate.EstimateId, ("C", 50m)), CancellationToken.None);
        Assert.Equal(50m, second.Total);
        Assert.Single(second.Sections);
        Assert.Equal(1, await context.LeadEstimateLines.CountAsync(row => row.EstimateId == estimate.EstimateId));

        var cleared = await handler.HandleAsync(new SetEstimateBreakdown(estimate.EstimateId, Array.Empty<EstimateBreakdownSection>(), "nigel@jewelbb.co.uk"), CancellationToken.None);
        Assert.Empty(cleared.Sections);
        Assert.Equal(50m, cleared.Total);
        Assert.Equal(0, await context.LeadEstimateLines.CountAsync(row => row.EstimateId == estimate.EstimateId));
    }

    [Fact]
    public async Task AnUnknownCostCode_isRefused_namingIt()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SetEstimateBreakdownHandler(context).HandleAsync(new SetEstimateBreakdown(estimate.EstimateId, new[]
            {
                new EstimateBreakdownSection("Steel", false, new[] { new EstimateBreakdownLine("NOT-A-CODE", "Beam", 1m, "nr", 100m) })
            }, "nigel@jewelbb.co.uk"), CancellationToken.None));

        Assert.Contains("NOT-A-CODE", refusal.Message);
        Assert.Equal(0, await context.LeadEstimateLines.CountAsync());
    }

    [Fact]
    public async Task AClosedEstimate_isRefused()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        var entity = await context.LeadEstimates.SingleAsync(row => row.EstimateId == estimate.EstimateId);
        entity.Status = (int)EstimateStatus.Lost;
        await context.SaveChangesAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SetEstimateBreakdownHandler(context).HandleAsync(Breakdown(estimate.EstimateId, ("A", 1m)), CancellationToken.None));
        Assert.Contains("Lost", refusal.Message);
    }

    [Fact]
    public async Task TheDetailsEdit_keepsTheBreakdownsTotal_whileLinesExist()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        await new SetEstimateBreakdownHandler(context).HandleAsync(Breakdown(estimate.EstimateId, ("A", 100m)), CancellationToken.None);

        var edited = await new UpdateEstimateDetailsHandler(context).HandleAsync(
            new UpdateEstimateDetails(estimate.EstimateId, "Scope", "", null, null, 999_999m, "", "Summary", "12 weeks", "VAT"), CancellationToken.None);

        Assert.Equal(100m, edited.Total);
        Assert.Equal("Summary", edited.ExecutiveSummary);
        Assert.Equal("12 weeks", edited.BuildTime);
        Assert.Equal("VAT", edited.Exclusions);
        Assert.Single(edited.Sections);
    }

    private static SetEstimateBreakdown Breakdown(string estimateId, params (string Section, decimal Price)[] items) =>
        new(estimateId, items.Select(item => new EstimateBreakdownSection(item.Section, false, new[]
        {
            new EstimateBreakdownLine("", $"{item.Section} line", 1m, "item", item.Price)
        })).ToList(), "nigel@jewelbb.co.uk");

    private static async Task<LeadEstimate> OpenEstimateAsync(JpmsContext context)
    {
        context.CostCenters.AddRange(
            new CostCenterEntity { CostCenterId = "cc1", Code = "SCAFF-STD", Name = "Scaffolding", SortOrder = 1 },
            new CostCenterEntity { CostCenterId = "cc2", Code = "PRELIMS-SMG", Name = "Site manager", SortOrder = 2 });
        await context.SaveChangesAsync();
        var lead = await new CaptureLeadHandler(context).HandleAsync(
            new CaptureLead("Julia", "", "", "", LeadProspectKind.Homeowner, "16 Ravens Dene", "BR7 5FP", "Loft", "", LeadSource.Inbound, null, null, "nigel@jewelbb.co.uk"),
            CancellationToken.None);
        return await new CreateEstimateHandler(context).HandleAsync(
            new CreateEstimate(lead.LeadId, "Loft conversion", "", null, null, null, "", "nigel@jewelbb.co.uk"), CancellationToken.None);
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"estimate-breakdown-{Guid.NewGuid():N}").Options);
}
