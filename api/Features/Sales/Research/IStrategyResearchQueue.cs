using Azure.Storage.Queues;

namespace Jewel.JPMS.Api.Features.Sales.Research;

/// <summary>Producer side of the research queue — the api enqueues, the worker consumes.</summary>
public interface IStrategyResearchQueue
{
    bool IsConfigured { get; }
    Task EnqueueAsync(StrategyResearchMessage message, CancellationToken cancellationToken);
}

/// <summary>Stands in when no storage connection is configured: the command refuses loudly rather
/// than leaving a strategy on Queued with nothing behind it.</summary>
public sealed class NullStrategyResearchQueue : IStrategyResearchQueue
{
    public bool IsConfigured => false;

    public Task EnqueueAsync(StrategyResearchMessage message, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException(
            "Research can't be queued — the storage queue connection isn't configured on the API."));
}

/// <summary>Azure Storage Queues, matching the mailbox and Bluebeam queues: Base64 encoding (the
/// Functions queue trigger's default), queue created lazily on first use.</summary>
public sealed class StorageStrategyResearchQueue : IStrategyResearchQueue
{
    private readonly QueueServiceClient serviceClient;
    private readonly SemaphoreSlim creationLock = new(1, 1);
    private QueueClient? queue;

    public StorageStrategyResearchQueue(string connectionString)
    {
        serviceClient = new QueueServiceClient(
            connectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    public bool IsConfigured => true;

    public async Task EnqueueAsync(StrategyResearchMessage message, CancellationToken cancellationToken)
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
            var client = serviceClient.GetQueueClient(SalesQueues.StrategyResearch);
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
