using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProjectStore : IProjectStore
{
    private readonly ProjectListReadModel readModel;
    private readonly ICommandSender commands;

    public HttpProjectStore(ProjectListReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Project> All()
    {
        if (readModel.Current is null) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current ?? Array.Empty<Project>();
    }

    public Project? Find(string projectId) =>
        All().FirstOrDefault(project =>
            string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase));

    public Project Upsert(Project project)
    {
        if (Find(project.ProjectId) is null) _ = CreateAsync(project);
        else _ = UpdateAsync(project);
        return project;
    }

    private async Task CreateAsync(Project project)
    {
        await commands.SendAsync(
            new CreateProjectShell(project.Reference, project.Name, project.ClientName, project.Organisation, project.ProjectManagerEmail),
            CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }

    private async Task UpdateAsync(Project project)
    {
        await commands.SendAsync(
            new UpdateProjectDetails(project.ProjectId, project.Reference, project.Name, project.ClientName, project.Organisation, project.Stage, project.ProjectManagerEmail),
            CancellationToken.None);
        await readModel.RefreshAsync(CancellationToken.None);
    }
}
