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
    private readonly ICommandSender commands;

    // Keys that have had a load started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> linesRequested = new();
    private readonly HashSet<string> claimsRequested = new();
    private readonly HashSet<string> claimLinesRequested = new();

    public HttpValuationReportStore(
        ValuationLinesReadModel linesReadModel,
        ValuationClaimsReadModel claimsReadModel,
        ClaimLinesReadModel claimLinesReadModel,
        ICommandSender commands)
    {
        this.linesReadModel = linesReadModel;
        this.claimsReadModel = claimsReadModel;
        this.claimLinesReadModel = claimLinesReadModel;
        this.commands = commands;
        linesReadModel.OnChanged += () => OnChange?.Invoke();
        claimsReadModel.OnChanged += () => OnChange?.Invoke();
        claimLinesReadModel.OnChanged += () => OnChange?.Invoke();
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
        claimLinesRequested.Clear();
        _ = LoadLinesAsync(projectId);
        _ = LoadClaimsAsync(projectId);
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

    public async Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId)
    {
        var result = await commands.SendAsync(new PreapproveValuationClaim(claimId), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }

    public async Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId)
    {
        var result = await commands.SendAsync(new ConfirmValuationClaim(claimId), CancellationToken.None);
        await claimsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return result;
    }
}
