using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpValuationReportStore : IValuationReportStore
{
    private readonly ValuationLinesReadModel linesReadModel;
    private readonly ValuationClaimsReadModel claimsReadModel;
    private readonly ClaimLinesReadModel claimLinesReadModel;
    private readonly ValuationReportSnapshotsReadModel snapshotsReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Keys that have had a load started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> linesRequested = new();
    private readonly HashSet<string> claimsRequested = new();
    private readonly HashSet<string> claimLinesRequested = new();
    private readonly HashSet<string> snapshotsRequested = new();

    public HttpValuationReportStore(
        ValuationLinesReadModel linesReadModel,
        ValuationClaimsReadModel claimsReadModel,
        ClaimLinesReadModel claimLinesReadModel,
        ValuationReportSnapshotsReadModel snapshotsReadModel,
        IQueryClient queries,
        ICommandSender commands)
    {
        this.linesReadModel = linesReadModel;
        this.claimsReadModel = claimsReadModel;
        this.claimLinesReadModel = claimLinesReadModel;
        this.snapshotsReadModel = snapshotsReadModel;
        this.queries = queries;
        this.commands = commands;
        linesReadModel.OnChanged += () => OnChange?.Invoke();
        claimsReadModel.OnChanged += () => OnChange?.Invoke();
        claimLinesReadModel.OnChanged += () => OnChange?.Invoke();
        snapshotsReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<ValuationLineItem> LinesFor(string projectId)
    {
        if (linesRequested.Add(projectId)) _ = LoadLinesAsync(projectId);
        return linesReadModel.Current(projectId);
    }

    private async Task LoadLinesAsync(string projectId)
    {
        try { await linesReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { linesRequested.Remove(projectId); }
    }

    public IReadOnlyList<ValuationClaim> ClaimsFor(string projectId)
    {
        if (claimsRequested.Add(projectId)) _ = LoadClaimsAsync(projectId);
        return claimsReadModel.Current(projectId);
    }

    private async Task LoadClaimsAsync(string projectId)
    {
        try { await claimsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { claimsRequested.Remove(projectId); }
    }

    public IReadOnlyList<ClaimLine> EntriesFor(string claimId)
    {
        if (claimLinesRequested.Add(claimId)) _ = LoadClaimLinesAsync(claimId);
        return claimLinesReadModel.Current(claimId);
    }

    private async Task LoadClaimLinesAsync(string claimId)
    {
        try { await claimLinesReadModel.RefreshAsync(claimId, CancellationToken.None); }
        catch { claimLinesRequested.Remove(claimId); }
    }

    // Forces a background reload of lines and claims even when cached, and marks per-claim
    // entries stale so the next EntriesFor read refetches. Pages call this once on entry
    // (never from render): cached data renders immediately, OnChange fires when fresh data
    // lands — so navigating back to the tab picks up changes made elsewhere.
    public void Refresh(string projectId)
    {
        linesRequested.Add(projectId);
        claimsRequested.Add(projectId);
        snapshotsRequested.Add(projectId);
        claimLinesRequested.Clear();
        _ = LoadLinesAsync(projectId);
        _ = LoadClaimsAsync(projectId);
        _ = LoadSnapshotsAsync(projectId);
    }

    public async Task<ValuationLineItem> AddLineAsync(AddValuationLineItem command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await linesReadModel.RefreshAsync(command.ProjectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationLineItem> UpdateLineAsync(UpdateValuationLineItem command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await linesReadModel.RefreshAsync(result.ProjectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationLineItem> SetLineCostCentreAsync(SetValuationLineCostCentre command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await linesReadModel.RefreshAsync(result.ProjectId, CancellationToken.None);
        return result;
    }

    public async Task RemoveLineAsync(string projectId, string lineItemId)
    {
        await commands.SendAsync(new RemoveValuationLineItem(lineItemId), CancellationToken.None);
        await linesReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    public async Task<ValuationClaim> StartClaimAsync(StartValuationClaim command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await claimsReadModel.RefreshAsync(command.ProjectId, CancellationToken.None);
        return result;
    }

    public async Task<ClaimLine> RecordEntryAsync(string projectId, RecordClaimEntry command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await claimLinesReadModel.RefreshAsync(command.ValuationClaimId, CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<IReadOnlyList<ClaimLine>> RecordEntriesAsync(string projectId, RecordClaimEntries command)
    {
        var result = await commands.SendAsync(command, CancellationToken.None);
        await claimLinesReadModel.RefreshAsync(command.ValuationClaimId, CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId)
    {
        var result = await commands.SendAsync(new PreapproveValuationClaim(claimId), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationClaim> ReopenClaimAsync(string projectId, string claimId)
    {
        var result = await commands.SendAsync(new ReopenValuationClaim(claimId), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId)
    {
        var result = await commands.SendAsync(new ConfirmValuationClaim(claimId), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationClaim> RenameClaimAsync(string projectId, string claimId, string name)
    {
        var result = await commands.SendAsync(new RenameValuationClaim(claimId, name), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task DeleteClaimAsync(string projectId, string claimId)
    {
        await commands.SendAsync(new DeleteValuationClaim(claimId), CancellationToken.None);
        // The claim's entries died with it and any invoice links were cleared server-side —
        // refetch claims now and mark per-claim entries stale for the next read.
        claimLinesRequested.Clear();
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
    }

    public IReadOnlyList<ValuationReportSnapshot> SnapshotsFor(string projectId)
    {
        if (snapshotsRequested.Add(projectId)) _ = LoadSnapshotsAsync(projectId);
        return snapshotsReadModel.Current(projectId);
    }

    private async Task LoadSnapshotsAsync(string projectId)
    {
        try { await snapshotsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { snapshotsRequested.Remove(projectId); }
    }

    public async Task<ValuationReportSnapshot> TakeSnapshotAsync(string projectId, string label)
    {
        var result = await commands.SendAsync(new TakeValuationReportSnapshot(projectId, label), CancellationToken.None);
        await snapshotsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    // Detail (header + frozen lines) is fetched per snapshot on demand — the viewer is
    // read-only and rarely opened, so there's nothing to keep in a read model.
    public Task<ValuationReportSnapshotDetail> GetSnapshotAsync(string snapshotId) =>
        queries.AskAsync(new GetValuationReportSnapshot(snapshotId), CancellationToken.None);

    public async Task DeleteSnapshotAsync(string projectId, string snapshotId)
    {
        await commands.SendAsync(new DeleteValuationReportSnapshot(snapshotId), CancellationToken.None);
        await snapshotsReadModel.RefreshAsync(projectId, CancellationToken.None);
    }
}
