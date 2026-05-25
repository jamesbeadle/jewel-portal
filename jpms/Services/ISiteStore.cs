using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ISiteStore
{
    IReadOnlyList<SiteReport> ReportsFor(string projectId);
    IReadOnlyList<ProgrammeTask> ProgrammeFor(string projectId);
    SiteReport SaveReport(SiteReport report);
    ProgrammeTask SaveProgrammeTask(ProgrammeTask task);
    event Action? OnChange;
}
