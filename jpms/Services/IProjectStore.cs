using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IProjectStore
{
    IReadOnlyList<Project> All();

    Project? Find(string projectId);

    Project Upsert(Project project);

    event Action? OnChange;
}
