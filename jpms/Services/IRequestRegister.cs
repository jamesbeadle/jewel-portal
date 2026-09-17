
namespace Jewel.JPMS.Services;

public interface IRequestRegister
{
    /// <summary>False until the project's requests have been fetched at least once. ForProject
    /// answers with an empty list until then, which is indistinguishable from a project with no
    /// requests — so anything rendering a count or an empty state has to gate on this first.</summary>
    bool LoadedFor(string projectId);

    IReadOnlyList<Request> ForProject(string projectId);
    IReadOnlyList<Request> ForProject(string projectId, RequestType kind);

    /// <summary>Starts a background refetch of the project's requests even if they are already
    /// cached — call on page entry so navigating back to a tab shows fresh data (stale-while-
    /// revalidate: cached rows render immediately and OnChange fires when the reload lands).</summary>
    void Refresh(string projectId);
    Request Upsert(Request record);
    event Action? OnChange;

    Task<Request?> GetAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Merges one General request into another before either reaches RFI stage. The
    /// survivor keeps its reference/title and absorbs the other's description, conversation,
    /// items and emails; the merged-away request closes with a "merged into" audit link.</summary>
    Task<Request> MergeAsync(string survivorRequestId, string mergedRequestId, string projectId, CancellationToken cancellationToken = default);
    Task<Request> RaiseAsync(RaiseRequest command, CancellationToken cancellationToken = default);
    Task<Request> UpdateAsync(UpdateRequestDetails command, CancellationToken cancellationToken = default);
    Task<Request> PromoteToRfiAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<Request> EnableRfqAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<Request> LinkToPartyAsync(string requestId, PartyKind partyKind, string? partyId, string? onBehalfOfClientId, string projectId, CancellationToken cancellationToken = default);

    /// <summary>Closes the request as at the user-chosen date (today or earlier; null closes as at
    /// now). Returns false only when the request no longer exists.</summary>
    Task<bool> CloseAsync(string requestId, string projectId, DateTimeOffset? closedAt = null, CancellationToken cancellationToken = default);

    /// <summary>Saves the official document's structured body: the itemised queries plus the
    /// basis-of-queries / response-action-required / impact sections (replace-all for the items).</summary>
    Task<Request> SaveFormAsync(UpdateRequestForm command, CancellationToken cancellationToken = default);

    /// <summary>Emails the official document PDF from the projects mailbox as a new thread —
    /// recipients and cover note resolved server-side. saveAsDraftOnly leaves it in Drafts for
    /// Outlook instead of sending.</summary>
    Task<RequestEmailOutcome> EmailDocumentAsync(string requestId, string? recipientOverride = null, bool saveAsDraftOnly = false, CancellationToken cancellationToken = default);

    /// <summary>Emails the official document as a REPLY to a conversation email linked to the
    /// request — same thread, PDF attached, cover note above the quoted history. saveAsDraftOnly
    /// leaves it in Drafts. <paramref name="mailboxMessageId"/> is the email's Graph id.</summary>
    Task<RequestEmailOutcome> EmailDocumentReplyAsync(string requestId, string mailboxMessageId, bool saveAsDraftOnly = false, CancellationToken cancellationToken = default);

    /// <summary>The bulk form of <see cref="EmailDocumentAsync"/>: one email per request id.
    /// Partial success is expected — every id gets an outcome carrying either what became of its
    /// email or the user-fixable reason there was none. Selections larger than the server's
    /// per-call cap are chunked into successive calls transparently.</summary>
    Task<RequestEmailBatch> EmailDocumentsAsync(IReadOnlyList<string> requestIds, bool saveAsDraftOnly = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestMessage>> ListMessagesAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Full body + attachment names of one inbound conversation email, fetched live on
    /// demand (the listed message body is only the mailbox's short preview snippet).</summary>
    Task<MailboxMessageDetail> GetEmailDetailAsync(string requestId, string mailboxId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<RequestMessage> PostMessageAsync(PostRequestMessage command, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task ReturnToTriageAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Request>> ListUnassignedAsync(CancellationToken cancellationToken = default);
}
