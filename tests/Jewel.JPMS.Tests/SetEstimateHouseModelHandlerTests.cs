using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The estimate's 3D model (2026-09-16): stored whole with its source, the timeline told, a closed estimate refused.
public sealed class SetEstimateHouseModelHandlerTests
{
    private const string Definition = """
        { "name": "16 Ravens Dene", "blocks": [ { "name": "house" }, { "name": "garage" } ],
          "openings": [ {}, {}, {} ], "elements": [ {} ],
          "programme": { "stages": [ {}, {} ] } }
        """;

    [Fact]
    public async Task StoresTheDefinitionWithItsSource_andTellsTheTimeline()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);

        var saved = await new SetEstimateHouseModelHandler(context).HandleAsync(
            new SetEstimateHouseModel(estimate.EstimateId, Model(Definition), " Resi B369214-3100 rev B ", "james@jewelbb.co.uk"), CancellationToken.None);

        Assert.True(saved.HasHouseModel);
        Assert.Equal("Resi B369214-3100 rev B", saved.HouseModelSource);
        Assert.NotNull(saved.HouseModelSetAt);
        Assert.Equal("16 Ravens Dene", Model(saved.HouseModelJson!).GetProperty("name").GetString());
        Assert.Equal("Estimate EST-0001 3D model set — 2 blocks, 3 openings, 1 element, 2 stages, from Resi B369214-3100 rev B",
            await LatestTimelineSummaryAsync(context, estimate.LeadId));
    }

    [Fact]
    public async Task ReplacesThePreviousDefinitionWhole()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        var handler = new SetEstimateHouseModelHandler(context);
        await handler.HandleAsync(new SetEstimateHouseModel(estimate.EstimateId, Model(Definition), "rev A", "james@jewelbb.co.uk"), CancellationToken.None);

        var saved = await handler.HandleAsync(
            new SetEstimateHouseModel(estimate.EstimateId, Model("""{ "name": "16 Ravens Dene", "blocks": [ {} ] }"""), "rev B", "james@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal("rev B", saved.HouseModelSource);
        Assert.DoesNotContain("openings", saved.HouseModelJson);
    }

    [Fact]
    public async Task RefusesAClosedEstimate()
    {
        await using var context = NewContext();
        var estimate = await OpenEstimateAsync(context);
        await new MoveEstimateStatusHandler(context).HandleAsync(new MoveEstimateStatus(estimate.EstimateId, EstimateStatus.Lost, null, "nigel@jewelbb.co.uk"), CancellationToken.None);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => new SetEstimateHouseModelHandler(context).HandleAsync(
            new SetEstimateHouseModel(estimate.EstimateId, Model(Definition), "rev B", "james@jewelbb.co.uk"), CancellationToken.None));

        Assert.Contains("history", refusal.Message);
    }

    [Fact]
    public void TheGateAsksForANamedHouseWithABlock_andASource()
    {
        var validation = new SetEstimateHouseModelValidation();

        Assert.False(validation.Check(new SetEstimateHouseModel("est", Model(Definition), "Resi rev B")).HasFailed);
        Assert.Contains(validation.Check(new SetEstimateHouseModel("est", Model("""{ "blocks": [ {} ] }"""), "Resi rev B")).Errors, error => error.Contains("name"));
        Assert.Contains(validation.Check(new SetEstimateHouseModel("est", Model("""{ "name": "x", "blocks": [] }"""), "Resi rev B")).Errors, error => error.Contains("block"));
        Assert.Contains(validation.Check(new SetEstimateHouseModel("est", Model("\"not an object\""), "Resi rev B")).Errors, error => error.Contains("definition object"));
        Assert.Contains(validation.Check(new SetEstimateHouseModel("est", Model(Definition), " ")).Errors, error => error.Contains("source"));
    }

    private static JsonElement Model(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static async Task<string> LatestTimelineSummaryAsync(JpmsContext context, string leadId)
    {
        var activities = context.LeadActivities.Where(row => row.LeadId == leadId);
        return (await activities.OrderByDescending(row => row.OccurredAt).FirstAsync()).Summary;
    }

    private static async Task<LeadEstimate> OpenEstimateAsync(JpmsContext context)
    {
        var lead = await new CaptureLeadHandler(context).HandleAsync(
            new CaptureLead("Julia", "", "", "", LeadProspectKind.Homeowner, "16 Ravens Dene", "BR7 5FP", "Loft", "", LeadSource.Inbound, null, null, "nigel@jewelbb.co.uk"),
            CancellationToken.None);
        return await new CreateEstimateHandler(context).HandleAsync(
            new CreateEstimate(lead.LeadId, "Loft conversion", "", null, null, null, "", "nigel@jewelbb.co.uk"), CancellationToken.None);
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"estimate-house-model-{Guid.NewGuid():N}").Options);
}
