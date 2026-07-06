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
    /// List the identity (current Graph id + stable internetMessageId) of every message currently in
    /// the Inbox. Used by reconciliation to mirror the Inbox against the intake table: anything in the
    /// queue but no longer here has left the Inbox. Pages through the whole folder.
    /// </summary>
    Task<IReadOnlyList<GraphInboxItem>> ListInboxMessageIdentitiesAsync(CancellationToken ct);

    /// <summary>
    /// Move a message into a destination folder. Returns the message's NEW Graph id
    /// (the id changes on every move and must be persisted afresh).
    /// </summary>
    Task<string> MoveMessageAsync(string graphMessageId, string destinationFolderId, CancellationToken ct);

    /// <summary>
    /// The id of the folder a message currently lives in, or null if the message no longer exists.
    /// Used to avoid a same-folder move: Graph's /move duplicates a message when the destination is
    /// the folder it is already in, so callers compare this against the destination first.
    /// </summary>
    Task<string?> GetMessageParentFolderIdAsync(string graphMessageId, CancellationToken ct);

    /// <summary>
    /// Resolve a well-known folder name (e.g. "inbox") to its concrete folder id, or null if it
    /// cannot be resolved. Lets callers compare a message's parent folder against the Inbox.
    /// </summary>
    Task<string?> GetFolderIdAsync(string wellKnownName, CancellationToken ct);

    /// <summary>
    /// Find a message anywhere in the mailbox by its stable internetMessageId, returning its current
    /// folder-scoped Graph id, or null if no message matches. Used to heal a stale stored id: a
    /// message's Graph id changes on every move, but its internetMessageId never does.
    /// </summary>
    Task<string?> FindMessageIdByInternetMessageIdAsync(string internetMessageId, CancellationToken ct);

    /// <summary>
    /// Find a child folder by display name under the given parent (pass null for the mailbox root),
    /// creating it if it does not exist. Returns the folder's Graph id. Idempotent.
    /// </summary>
    Task<string> EnsureFolderAsync(string displayName, string? parentFolderId, CancellationToken ct);

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

/// <summary>A message's identity in the Inbox: its current (move-volatile) Graph id and its stable internetMessageId.</summary>
public sealed record GraphInboxItem(string Id, string InternetMessageId);

public sealed record GraphSubscription(string Id, DateTimeOffset ExpiresAt);

/// <summary>A single email recipient (address plus an optional display name).</summary>
public sealed record GraphRecipient(string Email, string? Name = null);

/// <summary>
/// A file to attach to an outbound message. Sent as a Graph fileAttachment (base64 contentBytes);
/// suitable for the small request-document PDFs, which sit well under the sendMail size limit.
/// </summary>
public sealed record GraphAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>
/// A new outbound email to send via Graph. Supports one or more recipients, optional open and
/// blind copies (the correspondence profile's Cc/Bcc), and zero or more attachments (e.g. the
/// request-document PDF). Bcc is carried on the wire only — nothing rendered or recorded on a
/// client-facing surface may be derived from it.
/// </summary>
public sealed record GraphOutboundMessage(
    IReadOnlyList<GraphRecipient> To,
    string Subject,
    string HtmlBody,
    IReadOnlyList<GraphAttachment>? Attachments = null,
    IReadOnlyList<GraphRecipient>? Cc = null,
    IReadOnlyList<GraphRecipient>? Bcc = null)
{
    /// <summary>Convenience constructor for a single recipient with no attachments.</summary>
    public GraphOutboundMessage(string toEmail, string? toName, string subject, string htmlBody)
        : this(new[] { new GraphRecipient(toEmail, toName) }, subject, htmlBody) { }
}
