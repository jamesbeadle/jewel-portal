using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Worker.Bluebeam;

/// <summary>
/// Consumes the drawing-extractions queue. The whole run lives in DrawingExtractionRunner
/// (api-shared source, so a future trigger can reuse it); this class is just the binding. A throw
/// re-delivers the message — host.json's maxDequeueCount (5) is the retry policy, and the runner
/// stamps the row Failed with the error each attempt hits, so a poisoned message leaves an honest
/// row behind rather than a silent gap.
/// </summary>
public sealed class DrawingExtractionWorker
{
    private readonly DrawingExtractionRunner runner;

    public DrawingExtractionWorker(DrawingExtractionRunner runner)
    {
        this.runner = runner;
    }

    [Function(nameof(DrawingExtractionWorker))]
    public Task Run(
        [QueueTrigger(BluebeamQueues.DrawingExtractions, Connection = "MailboxQueuesConnection")] DrawingExtractionMessage message,
        CancellationToken cancellationToken) =>
        runner.RunAsync(message, cancellationToken);
}
