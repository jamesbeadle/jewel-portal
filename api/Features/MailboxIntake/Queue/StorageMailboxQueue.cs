using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Queue;

/// <summary>
/// Azure Storage Queues implementation. Messages are Base64-encoded to match the Functions queue
/// trigger's default decoding. Queues are created on first use. Each queue is created lazily and
/// cached.
/// </summary>
public sealed class StorageMailboxQueue : IMailboxQueue
{
    private readonly QueueServiceClient _serviceClient;
    private readonly ILogger<StorageMailboxQueue> _logger;
    private readonly ConcurrentDictionary<string, Lazy<Task<QueueClient>>> _queues = new();

    public StorageMailboxQueue(string connectionString, ILogger<StorageMailboxQueue> logger)
    {
        _serviceClient = new QueueServiceClient(
            connectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        _logger = logger;
    }

    public async Task EnqueueIntakeNotificationAsync(string graphMessageId, CancellationToken ct)
    {
        var queue = await GetQueueAsync(MailboxQueues.IntakeNotifications, ct);
        await queue.SendMessageAsync(graphMessageId, ct);
    }

    public async Task EnqueueMailboxActionAsync(MailboxActionMessage action, CancellationToken ct)
    {
        var queue = await GetQueueAsync(MailboxQueues.MailboxActions, ct);
        await queue.SendMessageAsync(JsonSerializer.Serialize(action), ct);
    }

    private Task<QueueClient> GetQueueAsync(string name, CancellationToken ct)
    {
        var lazy = _queues.GetOrAdd(name, key => new Lazy<Task<QueueClient>>(async () =>
        {
            var client = _serviceClient.GetQueueClient(key);
            await client.CreateIfNotExistsAsync(cancellationToken: CancellationToken.None);
            return client;
        }));
        return lazy.Value;
    }
}
