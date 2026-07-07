using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Services;

public sealed class HttpXeroTransactionStore : IXeroTransactionStore
{
    private readonly XeroTransactionsReadModel readModel;

    // Whether a load has been started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private bool requested;

    public HttpXeroTransactionStore(XeroTransactionsReadModel readModel)
    {
        this.readModel = readModel;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public XeroTransactionsSnapshot? Snapshot()
    {
        EnsureRequested();
        return readModel.Current;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        requested = true;
        await readModel.RefreshAsync(cancellationToken);
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
