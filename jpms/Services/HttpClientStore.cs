using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Clients;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpClientStore : IClientStore
{
    private readonly ClientsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpClientStore(ClientsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Client> All()
    {
        if (readModel.Current.Count == 0) _ = readModel.RefreshAsync(CancellationToken.None);
        return readModel.Current;
    }

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default)
    {
        await readModel.RefreshAsync(cancellationToken);
        return readModel.Current;
    }

    public Task<Client?> GetAsync(string clientId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetClientById(clientId), cancellationToken);

    public async Task<Client> CreateAsync(CreateClient command, CancellationToken cancellationToken = default)
    {
        var created = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
        return created;
    }

    public async Task<Client> UpdateArchitectAsync(UpdateClientArchitect command, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
        return updated;
    }
}
