using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Api.Features.Sales.Research;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Sales;

/// <summary>
/// Consumes the sales-imagine-render queue (2026-09-06). The whole render lives in
/// ImagineRenderRunner (api-shared source); this class is just the binding. The runner never
/// rethrows — it stamps the round Failed with the reason — so a message is consumed once: a
/// render is minutes and money, and one honest failure the lead page can retry beats five
/// silent repeats.
/// </summary>
public sealed class ImagineRenderWorker
{
    private readonly ImagineRenderRunner runner;

    public ImagineRenderWorker(ImagineRenderRunner runner)
    {
        this.runner = runner;
    }

    [Function(nameof(ImagineRenderWorker))]
    public Task Run(
        [QueueTrigger(ImagineQueues.Render, Connection = "MailboxQueuesConnection")] ImagineRenderMessage message,
        CancellationToken cancellationToken) =>
        runner.RunAsync(message, cancellationToken);
}
