using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IRequestRegister
{
    IReadOnlyList<Request> ForProject(string projectId);
    IReadOnlyList<Request> ForProject(string projectId, RequestType kind);
    Request? Find(string requestId);
    Request Upsert(Request record);
    event Action? OnChange;
}
