using Azure.Storage.Queues;

namespace Jewel.JPMS.Api.Features.Bluebeam.Queue;

/// <summary>
/// Azure Storage Queues implementation, matching StorageMailboxQueue: Base64 encoding (the
/// Functions queue trigger's default decoding), queue created lazily on first use.
/// </summary>
public sealed class StorageDrawingExtractionQueue : IDrawingExtractionQueue
{
    private readonly QueueServiceClient serviceClient;
    private readonly SemaphoreSlim creationLock = new(1, 1);
    private QueueClient? queue;

    public StorageDrawingExtractionQueue(string connectionString)
    {
        serviceClient = new QueueServiceClient(
            connectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    public async Task EnqueueAsync(DrawingExtractionMessage message, CancellationToken cancellationToken)
    {
        var client = await GetQueueAsync();
        await client.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken);
    }

    private async Task<QueueClient> GetQueueAsync()
    {
        if (queue is not null) return queue;
        await creationLock.WaitAsync();
        try
        {
            if (queue is not null) return queue;
            var client = serviceClient.GetQueueClient(BluebeamQueues.DrawingExtractions);
            await client.CreateIfNotExistsAsync();
            queue = client;
            return client;
        }
        finally
        {
            creationLock.Release();
        }
    }
}
