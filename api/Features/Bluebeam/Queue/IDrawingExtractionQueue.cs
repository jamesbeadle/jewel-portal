
namespace Jewel.JPMS.Api.Features.Bluebeam.Queue;

/// <summary>Producer side of the extraction queue — the api enqueues, the worker consumes.</summary>
public interface IDrawingExtractionQueue
{
    Task EnqueueAsync(DrawingExtractionMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Stands in when no storage connection is configured (local dev without Azurite). Queueing an
/// extraction then fails loudly rather than pretending — a row stuck on Queued forever with no
/// worker behind it would be a worse lie.
/// </summary>
public sealed class NullDrawingExtractionQueue : IDrawingExtractionQueue
{
    private readonly ILogger<NullDrawingExtractionQueue> logger;

    public NullDrawingExtractionQueue(ILogger<NullDrawingExtractionQueue> logger)
    {
        this.logger = logger;
    }

    public Task EnqueueAsync(DrawingExtractionMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning("Drawing extraction for revision {RevisionId} not queued — no queue storage configured.", message.DrawingRevisionId);
        return Task.FromException(new InvalidOperationException(
            "Extractions can't be queued — the storage queue connection isn't configured."));
    }
}
