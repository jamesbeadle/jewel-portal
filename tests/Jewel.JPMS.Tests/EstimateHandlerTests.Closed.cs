using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// A Won or Lost estimate is history, and an estimate answers to its id or its reference.
public sealed partial class EstimateHandlerTests
{
    [Theory]
    [InlineData(EstimateStatus.Won)]
    [InlineData(EstimateStatus.Lost)]
    public async Task AClosedEstimate_refusesEdits(EstimateStatus outcome)
    {
        await using var context = await ContextWithALeadAsync();
        var estimate = await new CreateEstimateHandler(context).HandleAsync(NewEstimate() with { Total = 90_000m }, CancellationToken.None);
        await new MoveEstimateStatusHandler(context).HandleAsync(
            new MoveEstimateStatus(estimate.EstimateId, outcome, null, Estimator), CancellationToken.None);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateEstimateDetailsHandler(context).HandleAsync(
                new UpdateEstimateDetails(estimate.EstimateId, "Changed", "", null, null, 1m, ""), CancellationToken.None));
        Assert.Contains(outcome.ToString(), refusal.Message);
    }

    [Fact]
    public async Task Get_acceptsTheIdOrTheReference()
    {
        await using var context = await ContextWithALeadAsync();
        var estimate = await new CreateEstimateHandler(context).HandleAsync(NewEstimate(), CancellationToken.None);
        var get = new GetEstimateHandler(context);

        Assert.Equal(estimate.EstimateId, (await get.HandleAsync(new GetEstimate(estimate.EstimateId), CancellationToken.None))!.EstimateId);
        Assert.Equal(estimate.EstimateId, (await get.HandleAsync(new GetEstimate("EST-0001"), CancellationToken.None))!.EstimateId);
        Assert.Equal(estimate.EstimateId, (await get.HandleAsync(new GetEstimate("est 1"), CancellationToken.None))!.EstimateId);
        Assert.Null(await get.HandleAsync(new GetEstimate("EST-0099"), CancellationToken.None));
    }
}
