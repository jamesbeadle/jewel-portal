using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>
/// No-op Graph client used when the mailbox feature is unconfigured (no client secret present).
/// Lets the host start and the rest of the app run; every operation is a logged no-op so a
/// misconfiguration is visible in the logs rather than silently swallowed.
/// </summary>
public sealed class NullGraphMailClient : IGraphMailClient
{
    private readonly ILogger<NullGraphMailClient> _logger;

    public NullGraphMailClient(ILogger<NullGraphMailClient> logger) => _logger = logger;

    public Task<GraphMessagePage> GetDeltaPageAsync(string? link, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph delta fetch.");
        return Task.FromResult(new GraphMessagePage(Array.Empty<GraphMessage>(), null, null));
    }

    public Task<GraphMessage?> GetMessageAsync(string graphMessageId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph message fetch.");
        return Task.FromResult<GraphMessage?>(null);
    }

    public Task<IReadOnlyList<GraphInboxItem>> ListInboxMessageIdentitiesAsync(CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph inbox listing.");
        return Task.FromResult<IReadOnlyList<GraphInboxItem>>(Array.Empty<GraphInboxItem>());
    }

    public Task<string> MoveMessageAsync(string graphMessageId, string destinationFolderId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph move.");
        return Task.FromResult(graphMessageId);
    }

    public Task<string?> GetMessageParentFolderIdAsync(string graphMessageId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph parent-folder lookup.");
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetFolderIdAsync(string wellKnownName, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph folder lookup.");
        return Task.FromResult<string?>(null);
    }

    public Task<string> EnsureFolderAsync(string displayName, string? parentFolderId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph ensure-folder.");
        return Task.FromResult(string.Empty);
    }

    public Task SendMailAsync(GraphOutboundMessage message, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph sendMail.");
        return Task.CompletedTask;
    }

    public Task ReplyAsync(string graphMessageId, string htmlBody, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph reply.");
        return Task.CompletedTask;
    }

    public Task<GraphSubscription> CreateSubscriptionAsync(string notificationUrl, string clientState, DateTimeOffset expiry, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph subscription create.");
        return Task.FromResult(new GraphSubscription("", expiry));
    }

    public Task<GraphSubscription> RenewSubscriptionAsync(string subscriptionId, DateTimeOffset expiry, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph subscription renew.");
        return Task.FromResult(new GraphSubscription(subscriptionId, expiry));
    }

    public Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken ct)
    {
        _logger.LogWarning("Mailbox intake not configured; skipping Graph subscription delete.");
        return Task.CompletedTask;
    }
}
