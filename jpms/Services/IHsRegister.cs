using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IHsRegister
{
    IReadOnlyList<HsRecord> All();
    IReadOnlyList<HsRecord> ByKind(HsRecordKind kind);
    IReadOnlyList<HsRecord> ForProject(string projectId);
    HsRecord Upsert(HsRecord record);
    event Action? OnChange;
}
