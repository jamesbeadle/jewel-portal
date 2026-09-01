
namespace Jewel.JPMS.Services;

public sealed class HttpClientCostReferenceStore : IClientCostReferenceStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpClientCostReferenceStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<ClientCostReference>> ListAsync(string projectId) =>
        queries.AskAsync(new ListClientCostReferencesForProject(projectId), CancellationToken.None);

    public Task<IReadOnlyList<ClientCostReference>> SaveAsync(SetClientCostReferences command) =>
        commands.SendAsync(command, CancellationToken.None);
}
