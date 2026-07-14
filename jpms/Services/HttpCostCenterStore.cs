using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCostCenterStore : ICostCenterStore
{
    private readonly CostCentersReadModel readModel;
    private readonly ICommandSender commands;

    // Whether a load has been started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private bool requested;

    public HttpCostCenterStore(CostCentersReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<CostCenter> Active()
    {
        EnsureRequested();
        return readModel.Current;
    }

    public IReadOnlyList<CostCenter> ActiveAlphabetical()
    {
        EnsureRequested();
        return readModel.Alphabetical;
    }

    public IReadOnlyList<CostCenter> All()
    {
        EnsureRequested();
        return readModel.All;
    }

    private void EnsureRequested()
    {
        if (!requested) { requested = true; _ = LoadAsync(); }
    }

    private async Task LoadAsync()
    {
        try { await readModel.RefreshAllAsync(CancellationToken.None); }
        catch { requested = false; }
    }

    public async Task<IReadOnlyList<CostCenter>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        requested = true;
        await readModel.RefreshAllAsync(cancellationToken);
        return readModel.All;
    }

    public async Task<CostCenter> AddAsync(AddCostCenter command, CancellationToken cancellationToken = default)
    {
        var added = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAllAsync(cancellationToken);
        return added;
    }

    public async Task<CostCenter> ReviseAsync(ReviseCostCenter command, CancellationToken cancellationToken = default)
    {
        var revised = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAllAsync(cancellationToken);
        return revised;
    }
}
