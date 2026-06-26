namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>
/// Thin wrapper over the Microsoft Graph REST endpoints the mailbox-intake feature needs.
/// One implementation talks to Graph; a no-op fallback is used when the feature is unconfigured.
/// </summary>
public interface IGraphMailClient
{
    /// <summary>
    /// Fetch one page of the Inbox /messages/delta feed. Pass null to start a fresh delta
    /// enumeration (the one-time backlog import pages through with no cursor); pass a previously
    /// returned nextLink to continue paging, or a deltaLink to fetch only what changed since.
    /// </summary>
    Task<GraphMessagePage> GetDeltaPageAsync(string? link, CancellationToken ct);

    /// <summary>Fetch a single message by its current Graph id (e.g. from a webhook notification).</summary>
    Task<GraphMessage?> GetMessageAsync(string graphMessageId, CancellationToken ct);

    /// <summary>
    /// Move a message into a destination folder. Returns the message's NEW Graph id
    /// (the id changes on every move and must be persisted afresh).
    /// </summary>
    Task<string> MoveMessageAsync(string graphMessageId, string destinationFolderId, CancellationToken ct);

    /// <summary>Send a brand-new message (new thread).</summary>
    Task SendMailAsync(GraphOutboundMessage message, CancellationToken ct);

    /// <summary>Reply to an existing message so the reply threads into the original conversation.</summary>
    Task ReplyAsync(string graphMessageId, string htmlBody, CancellationToken ct);

    Task<GraphSubscription> CreateSubscriptionAsync(string notificationUrl, string clientState, DateTimeOffset expiry, CancellationToken ct);
    Task<GraphSubscription> RenewSubscriptionAsync(string subscriptionId, DateTimeOffset expiry, CancellationToken ct);
    Task DeleteSubscriptionAsync(string subscriptionId, CancellationToken ct);
}

/// <summary>A page of delta results: the messages plus exactly one continuation link.</summary>
public sealed record GraphMessagePage(
    IReadOnlyList<GraphMessage> Messages,
    string? NextLink,
    string? DeltaLink);

/// <summary>The subset of a Graph message the intake layer cares about.</summary>
public sealed record GraphMessage(
    string Id,
    string InternetMessageId,
    string? ConversationId,
    string? InReplyTo,
    string? References,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    bool HasAttachments,
    DateTimeOffset ReceivedAt,
    bool IsRemoved);

public sealed record GraphSubscription(string Id, DateTimeOffset ExpiresAt);

/// <summary>A new outbound email to send via Graph.</summary>
public sealed record GraphOutboundMessage(
    string ToEmail,
    string? ToName,
    string Subject,
    string HtmlBody);
