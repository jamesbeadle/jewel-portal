using Azure.Storage.Queues;
using Jewel.JPMS.Api.Features.Sales.Research;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>Producer side of the render queue — the api enqueues a round, the worker renders it.</summary>
public interface IImagineRenderQueue
{
    bool IsConfigured { get; }
    Task EnqueueAsync(ImagineRenderMessage message, CancellationToken cancellationToken);
}

/// <summary>Stands in when no storage connection is configured: the submission is refused with a
/// reason rather than left Queued with nothing behind it.</summary>
public sealed class NullImagineRenderQueue : IImagineRenderQueue
{
    public bool IsConfigured => false;

    public Task EnqueueAsync(ImagineRenderMessage message, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException(
            "Renders can't be queued — the storage queue connection isn't configured on the API."));
}

/// <summary>Azure Storage Queues, matching the research queue: Base64 encoding (the Functions
/// queue trigger's default), queue created lazily on first use.</summary>
public sealed class StorageImagineRenderQueue : IImagineRenderQueue
{
    private readonly QueueServiceClient serviceClient;
    private readonly SemaphoreSlim creationLock = new(1, 1);
    private QueueClient? queue;

    public StorageImagineRenderQueue(string connectionString)
    {
        serviceClient = new QueueServiceClient(
            connectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    public bool IsConfigured => true;

    public async Task EnqueueAsync(ImagineRenderMessage message, CancellationToken cancellationToken)
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
            var client = serviceClient.GetQueueClient(ImagineQueues.Render);
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
