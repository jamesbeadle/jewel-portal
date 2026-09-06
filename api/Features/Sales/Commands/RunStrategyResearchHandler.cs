using Jewel.JPMS.Api.Features.Sales.Research;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

/// <summary>Marks the strategy Queued and hands it to the worker. Refuses a strategy already in
/// flight, and refuses outright when no queue is configured (nothing would ever pick it up).</summary>
public sealed class RunStrategyResearchHandler : ICommandHandler<RunStrategyResearch, SalesStrategy>
{
    private readonly JpmsContext context;
    private readonly IStrategyResearchQueue queue;

    public RunStrategyResearchHandler(JpmsContext context, IStrategyResearchQueue queue)
    {
        this.context = context;
        this.queue = queue;
    }

    public async Task<SalesStrategy> HandleAsync(RunStrategyResearch command, CancellationToken cancellationToken)
    {
        var entity = await context.SalesStrategies.FirstOrDefaultAsync(row => row.StrategyId == command.StrategyId, cancellationToken)
            ?? throw new InvalidOperationException($"Strategy {command.StrategyId} not found.");
        if (!queue.IsConfigured)
            throw new InvalidOperationException("Research can't run: the API has no storage queue connection configured, so nothing would pick the job up.");
        var status = (StrategyResearchStatus)entity.ResearchStatus;
        // A run stuck in flight for over an hour is dead (the worker stamps Failed on any error it
        // sees; a killed host cannot) — let it be re-queued rather than blocking forever.
        var stale = entity.ResearchRequestedAt is { } at && at < DateTimeOffset.UtcNow.AddHours(-1);
        if (status.IsInProgress() && !stale)
            throw new InvalidOperationException($"Research is already {status.DisplayName().ToLowerInvariant()} for this strategy — give it a few minutes.");
        if (string.IsNullOrWhiteSpace(entity.Brief) && string.IsNullOrWhiteSpace(entity.Hypothesis))
            throw new InvalidOperationException("Write the brief first — the idea in your own words is what the research works from.");

        entity.ResearchStatus = (int)StrategyResearchStatus.Queued;
        entity.ResearchRequestedAt = DateTimeOffset.UtcNow;
        entity.ResearchCompletedAt = null;
        entity.ResearchError = null;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await queue.EnqueueAsync(new StrategyResearchMessage(entity.StrategyId), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            entity.ResearchStatus = (int)StrategyResearchStatus.Failed;
            entity.ResearchError = "Couldn't queue the research: " + ex.Message;
            await context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(entity.ResearchError);
        }
        return entity.ToModel();
    }
}
