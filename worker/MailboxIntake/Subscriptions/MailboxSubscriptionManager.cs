using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Ingestion;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Subscriptions;

/// <summary>
/// Creates / renews / recreates the Graph change-notification subscription for the Inbox.
/// Graph caps message-resource subscriptions at well under 3 days, so the renewal timer keeps it
/// alive; if the subscription has lapsed or been removed, this recreates it. The durable id +
/// expiry live in the sync-state row.
/// </summary>
public sealed class MailboxSubscriptionManager
{
    // Graph's documented maximum for message subscriptions is 4230 minutes (~2.9 days);
    // stay safely under it.
    private static readonly TimeSpan SubscriptionLifetime = TimeSpan.FromMinutes(4000);

    private readonly MailboxIntakeOptions _options;
    private readonly IGraphMailClient _graph;
    private readonly MailboxSyncStateStore _store;
    private readonly ILogger<MailboxSubscriptionManager> _logger;

    public MailboxSubscriptionManager(
        MailboxIntakeOptions options,
        IGraphMailClient graph,
        MailboxSyncStateStore store,
        ILogger<MailboxSubscriptionManager> logger)
    {
        _options = options;
        _graph = graph;
        _store = store;
        _logger = logger;
    }

    public async Task EnsureSubscriptionAsync(CancellationToken ct)
    {
        if (!_options.Enabled || !_options.EnableWebhook)
            return;

        if (string.IsNullOrWhiteSpace(_options.NotificationUrl) || string.IsNullOrWhiteSpace(_options.ClientState))
        {
            _logger.LogWarning("Webhook enabled but NotificationUrl/ClientState not configured; skipping subscription.");
            return;
        }

        var state = await _store.GetOrCreateAsync(ct);
        var expiry = DateTimeOffset.UtcNow.Add(SubscriptionLifetime);

        if (string.IsNullOrEmpty(state.SubscriptionId))
        {
            await CreateAsync(state, expiry, ct);
            return;
        }

        try
        {
            var renewed = await _graph.RenewSubscriptionAsync(state.SubscriptionId, expiry, ct);
            state.SubscriptionExpiresAt = renewed.ExpiresAt;
            await _store.SaveAsync(ct);
            _logger.LogInformation("Renewed mailbox subscription until {Expiry:o}.", renewed.ExpiresAt);
        }
        catch (GraphRequestException ex)
        {
            _logger.LogWarning(ex, "Mailbox subscription renew failed; recreating.");
            await CreateAsync(state, expiry, ct);
        }
    }

    private async Task CreateAsync(Jewel.JPMS.Api.Data.Entities.MailboxSyncStateEntity state, DateTimeOffset expiry, CancellationToken ct)
    {
        var created = await _graph.CreateSubscriptionAsync(_options.NotificationUrl!, _options.ClientState!, expiry, ct);
        state.SubscriptionId = created.Id;
        state.SubscriptionExpiresAt = created.ExpiresAt;
        await _store.SaveAsync(ct);
        _logger.LogInformation("Created mailbox subscription {Id} until {Expiry:o}.", created.Id, created.ExpiresAt);
    }
}
