using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpProjectRetentionStore : IProjectRetentionStore
{
    private readonly ICommandSender commands;

    // Null is a legitimate cached value (project has no retention terms yet), and the cache's
    // fetch-once semantics stop an empty result driving a render → fetch → render loop.
    private readonly AsyncQueryCache<string, ProjectRetention?> retentions;

    public HttpProjectRetentionStore(IQueryClient queries, ICommandSender commands)
    {
        this.commands = commands;
        retentions = new((id, ct) => queries.AskAsync(new GetProjectRetention(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public ProjectRetention? RetentionFor(string projectId) =>
        retentions.Get(projectId, null);

    public void Refresh(string projectId) => retentions.Refetch(projectId);

    public async Task<ProjectRetention> SetAsync(SetProjectRetention command)
    {
        var retention = await commands.SendAsync(command, CancellationToken.None);
        retentions.Invalidate(command.ProjectId);
        return retention;
    }

    public async Task<ProjectRetention> ConfirmReleaseAsync(ConfirmRetentionRelease command)
    {
        var retention = await commands.SendAsync(command, CancellationToken.None);
        retentions.Invalidate(command.ProjectId);
        return retention;
    }
}
