using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IChangeRegister
{
    IReadOnlyList<ChangeRecord> ForProject(string projectId);
    IReadOnlyList<ChangeRecord> ForProject(string projectId, ChangeKind kind);
    ChangeRecord? Find(string changeRecordId);
    ChangeRecord Upsert(ChangeRecord record);
    event Action? OnChange;
}
