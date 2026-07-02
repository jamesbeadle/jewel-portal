using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IRequestRegister
{
    IReadOnlyList<Request> ForProject(string projectId);
    IReadOnlyList<Request> ForProject(string projectId, RequestType kind);
    Request Upsert(Request record);
    event Action? OnChange;

    Task<Request?> GetAsync(string requestId, CancellationToken cancellationToken = default);
    Task<Request> RaiseAsync(RaiseRequest command, CancellationToken cancellationToken = default);
    Task<Request> UpdateAsync(UpdateRequestDetails command, CancellationToken cancellationToken = default);
    Task<Request> PromoteToRfiAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<Request> EnableRfqAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<Request> LinkToClientAsync(string requestId, string? clientId, string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestMessage>> ListMessagesAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Full body + attachment names of one inbound conversation email, fetched live on
    /// demand (the listed message body is only the mailbox's short preview snippet).</summary>
    Task<MailboxMessageDetail> GetEmailDetailAsync(string requestId, string mailboxId, string? internetMessageId, CancellationToken cancellationToken = default);
    Task<RequestMessage> PostMessageAsync(PostRequestMessage command, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task ReturnToTriageAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Request>> ListUnassignedAsync(CancellationToken cancellationToken = default);
}
