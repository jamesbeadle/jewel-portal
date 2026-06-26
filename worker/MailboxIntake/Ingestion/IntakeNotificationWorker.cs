using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Queue;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;

/// <summary>
/// Processes queued webhook notifications: fetch the message from Graph by id, then idempotently
/// ingest it. Exceptions are allowed to propagate so the queue retries; after max dequeues the
/// host moves the message to the poison queue, which must be alerted on (a notification must
/// surface, never silently vanish).
/// </summary>
public sealed class IntakeNotificationWorker
{
    private readonly IGraphMailClient _graph;
    private readonly IntakeIngestionService _ingestion;
    private readonly ILogger<IntakeNotificationWorker> _logger;

    public IntakeNotificationWorker(
        IGraphMailClient graph, IntakeIngestionService ingestion, ILogger<IntakeNotificationWorker> logger)
    {
        _graph = graph;
        _ingestion = ingestion;
        _logger = logger;
    }

    [Function(nameof(IntakeNotificationWorker))]
    public async Task Run(
        [QueueTrigger(MailboxQueues.IntakeNotifications, Connection = "MailboxQueuesConnection")] string graphMessageId,
        CancellationToken ct)
    {
        var message = await _graph.GetMessageAsync(graphMessageId, ct);
        if (message is null)
        {
            // Message was moved/deleted before we fetched it; the delta sweep already has it or it's gone.
            _logger.LogInformation("Notified message {GraphId} no longer retrievable; skipping.", graphMessageId);
            return;
        }

        await _ingestion.IngestAsync(message, ct);
    }
}
