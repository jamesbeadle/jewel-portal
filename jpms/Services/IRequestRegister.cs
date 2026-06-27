using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IRequestRegister
{
    IReadOnlyList<Request> ForProject(string projectId);
    IReadOnlyList<Request> ForProject(string projectId, RequestType kind);
    Request? Find(string requestId);
    Request Upsert(Request record);
    event Action? OnChange;

    Task<Request?> GetAsync(string requestId, CancellationToken cancellationToken = default);
    Task<Request> RaiseAsync(RaiseRequest command, CancellationToken cancellationToken = default);
    Task<Request> UpdateAsync(UpdateRequestDetails command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestMessage>> ListMessagesAsync(string requestId, CancellationToken cancellationToken = default);
    Task<RequestMessage> PostMessageAsync(PostRequestMessage command, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
    Task ReturnToTriageAsync(string requestId, string projectId, CancellationToken cancellationToken = default);
}
