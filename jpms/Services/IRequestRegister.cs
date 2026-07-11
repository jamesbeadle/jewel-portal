using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IRequestRegister
{
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

    /// <summary>Saves the official document's structured body: the itemised queries plus the
    /// basis-of-queries / response-action-required / impact sections (replace-all for the items).</summary>
    Task<Request> SaveFormAsync(UpdateRequestForm command, CancellationToken cancellationToken = default);

    /// <summary>Creates an Outlook draft in the projects mailbox carrying the official document PDF —
    /// recipients and cover note pre-filled. Nothing is sent; the draft waits in Drafts.</summary>
    Task<RequestEmailDraft> PrepareEmailDraftAsync(string requestId, string? recipientOverride = null, CancellationToken cancellationToken = default);

    /// <summary>Creates an Outlook draft REPLY to a conversation email linked to the request —
    /// same thread, official PDF attached, cover note above the quoted history. Nothing is sent;
    /// the draft waits in Drafts. <paramref name="mailboxMessageId"/> is the email's Graph id.</summary>
    Task<RequestEmailDraft> PrepareReplyDraftAsync(string requestId, string mailboxMessageId, CancellationToken cancellationToken = default);

    /// <summary>The bulk form of <see cref="PrepareEmailDraftAsync"/>: one Outlook draft per
    /// request id. Partial success is expected — every id gets an outcome carrying either its
    /// created draft or the user-fixable reason it couldn't be drafted. Selections larger than
    /// the server's per-call cap are chunked into successive calls transparently.</summary>
    Task<RequestEmailDraftBatch> PrepareEmailDraftsAsync(IReadOnlyList<string> requestIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestMessage>> ListMessagesAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Full body + attachment names of one inbound conversation email, fetched live on
    /// demand (the listed message body is only the mailbox's short preview snippet).</summary>
    Task<MailboxMessageDetail> GetEmailDetailAsync(string requestId, string mailboxId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<RequestMessage> PostMessageAsync(PostRequestMessage command, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task ReturnToTriageAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Request>> ListUnassignedAsync(CancellationToken cancellationToken = default);
}
