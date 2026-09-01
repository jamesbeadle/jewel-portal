using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Services;

public sealed class HttpXeroAgedPayablesStore : IXeroAgedPayablesStore
{
    private readonly XeroAgedPayablesReadModel readModel;

    // Whether a load has been started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private bool requested;

    public HttpXeroAgedPayablesStore(XeroAgedPayablesReadModel readModel)
    {
        this.readModel = readModel;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public XeroAgedPayablesSnapshot? Snapshot()
    {
        EnsureRequested();
        return readModel.Current;
    }

    public async Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        requested = true;
        await readModel.RefreshAsync(force, cancellationToken);
    }

    private void EnsureRequested()
    {
        if (!requested) { requested = true; _ = LoadAsync(); }
    }

    private async Task LoadAsync()
    {
        try { await readModel.RefreshAsync(CancellationToken.None); }
        catch { requested = false; }
    }
}
