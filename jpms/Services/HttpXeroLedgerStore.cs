using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Services;

public sealed class HttpXeroLedgerStore : IXeroLedgerStore
{
    private readonly XeroLedgerReadModel readModel;
    private readonly ICommandSender commands;

    // Whether a load has been started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private bool requested;

    public HttpXeroLedgerStore(XeroLedgerReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<XeroLedgerLine>? Lines()
    {
        EnsureRequested();
        return readModel.Current;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        requested = true;
        await readModel.RefreshAsync(cancellationToken);
    }

    public async Task<XeroLedgerSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = await commands.SendAsync(new SyncXeroLedger(), cancellationToken);
        await RefreshAsync(cancellationToken);
        return result;
    }

    public async Task<int> ApplyAsync(SetXeroAllocation command, CancellationToken cancellationToken = default)
    {
        var affected = await commands.SendAsync(command, cancellationToken);
        await RefreshAsync(cancellationToken);
        return affected;
    }

    public async Task<int> AllocateSuggestedAsync(CancellationToken cancellationToken = default)
    {
        var allocated = await commands.SendAsync(new AllocateSuggestedXeroLines(), cancellationToken);
        await RefreshAsync(cancellationToken);
        return allocated;
    }

    public async Task<XeroWriteBackOutcome> RetryWriteBackAsync(string xeroInvoiceId, CancellationToken cancellationToken = default)
    {
        var outcome = await commands.SendAsync(new RetryXeroWriteBack(xeroInvoiceId), cancellationToken);
        await RefreshAsync(cancellationToken);
        return outcome;
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
