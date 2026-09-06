using Jewel.JPMS.Api.Features.Sales.Research;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Sales;

/// <summary>
/// Consumes the sales-strategy-research queue (2026-09-06). The whole run lives in
/// StrategyResearchRunner (api-shared source); this class is just the binding. The runner never
/// rethrows — it stamps the strategy Failed with the reason instead — so a message is consumed
/// once: a research call costs real money and minutes, and one honest failure on the page beats
/// five silent retries.
/// </summary>
public sealed class StrategyResearchWorker
{
    private readonly StrategyResearchRunner runner;

    public StrategyResearchWorker(StrategyResearchRunner runner)
    {
        this.runner = runner;
    }

    [Function(nameof(StrategyResearchWorker))]
    public Task Run(
        [QueueTrigger(SalesQueues.StrategyResearch, Connection = "MailboxQueuesConnection")] StrategyResearchMessage message,
        CancellationToken cancellationToken) =>
        runner.RunAsync(message, cancellationToken);
}
