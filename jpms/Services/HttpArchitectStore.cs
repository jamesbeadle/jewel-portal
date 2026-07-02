using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Architects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpArchitectStore : IArchitectStore
{
    private readonly ArchitectsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Whether an architects load has been started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpClientStore).
    private bool requested;

    public HttpArchitectStore(ArchitectsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Architect> All()
    {
        if (!requested) { requested = true; _ = LoadAsync(); }
        return readModel.Current;
    }

    private async Task LoadAsync()
    {
        try { await readModel.RefreshAsync(CancellationToken.None); }
        catch { requested = false; }
    }

    public async Task<IReadOnlyList<Architect>> ListAsync(CancellationToken cancellationToken = default)
    {
        await readModel.RefreshAsync(cancellationToken);
        return readModel.Current;
    }

    public Task<Architect?> GetAsync(string architectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetArchitectById(architectId), cancellationToken);

    public async Task<Architect> CreateAsync(CreateArchitect command, CancellationToken cancellationToken = default)
    {
        var created = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
        return created;
    }

    public async Task<Architect> UpdateAsync(UpdateArchitect command, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(cancellationToken);
        return updated;
    }
}
