using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Services;

public sealed class HttpXeroLedgerStore : IXeroLedgerStore
{
    private readonly XeroLedgerReadModel readModel;
    private readonly ICommandSender commands;

    // Which statuses have had a load started — prevents an empty result from re-triggering a fetch
    // on every re-render (see HttpDrawingStore). Keyed per status now that reads are per status.
    private readonly HashSet<XeroAllocationStatus> requested = new();
    private bool countsRequested;

    public HttpXeroLedgerStore(XeroLedgerReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<XeroLedgerLine>? Lines(XeroAllocationStatus status)
    {
        EnsureRequested(status);
        return readModel.Current(status);
    }

    public XeroLedgerCounts? Counts()
    {
        EnsureCountsRequested();
        return readModel.Counts;
    }

    public async Task RefreshAsync(XeroAllocationStatus status, CancellationToken cancellationToken = default)
    {
        requested.Add(status);
        countsRequested = true;
        await Task.WhenAll(
            readModel.RefreshAsync(status, cancellationToken),
            readModel.RefreshCountsAsync(cancellationToken));
    }

    public async Task<XeroLedgerSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = await commands.SendAsync(new SyncXeroLedger(), cancellationToken);
        await ReloadAfterWriteAsync(cancellationToken);
        return result;
    }

    public async Task<int> ApplyAsync(SetXeroAllocation command, CancellationToken cancellationToken = default)
    {
        var affected = await commands.SendAsync(command, cancellationToken);
        await ReloadAfterWriteAsync(cancellationToken);
        return affected;
    }

    public async Task<int> AllocateSuggestedAsync(CancellationToken cancellationToken = default)
    {
        var allocated = await commands.SendAsync(new AllocateSuggestedXeroLines(), cancellationToken);
        await ReloadAfterWriteAsync(cancellationToken);
        return allocated;
    }

    public async Task<XeroWriteBackOutcome> RetryWriteBackAsync(string xeroInvoiceId, CancellationToken cancellationToken = default)
    {
        var outcome = await commands.SendAsync(new RetryXeroWriteBack(xeroInvoiceId), cancellationToken);
        await ReloadAfterWriteAsync(cancellationToken);
        return outcome;
    }

    /// <summary>
    /// A write can move a line from any status to any other — allocating takes a row out of
    /// Unallocated and puts it in Allocated — so every status already in hand is reloaded, along
    /// with the counts. That is at most the two or three tabs this session has actually opened,
    /// never the whole ledger.
    /// </summary>
    private async Task ReloadAfterWriteAsync(CancellationToken cancellationToken)
    {
        countsRequested = true;
        var reloads = readModel.LoadedStatuses
            .Select(status => readModel.RefreshAsync(status, cancellationToken))
            .ToList();
        reloads.Add(readModel.RefreshCountsAsync(cancellationToken));
        await Task.WhenAll(reloads);
    }

    private void EnsureRequested(XeroAllocationStatus status)
    {
        if (requested.Add(status)) _ = LoadAsync(status);
    }

    private async Task LoadAsync(XeroAllocationStatus status)
    {
        try { await readModel.RefreshAsync(status, CancellationToken.None); }
        catch { requested.Remove(status); }
    }

    private void EnsureCountsRequested()
    {
        if (countsRequested) return;
        countsRequested = true;
        _ = LoadCountsAsync();
    }

    private async Task LoadCountsAsync()
    {
        try { await readModel.RefreshCountsAsync(CancellationToken.None); }
        catch { countsRequested = false; }
    }
}
