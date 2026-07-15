using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Progress;

public sealed class ProgressReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProgressUpdate>> updatesByProject = new();
    private readonly Dictionary<string, IReadOnlyList<ProgressReport>> reportsByProject = new();

    // Tracks which keys have had a fetch started, so an *empty* result does not keep
    // re-triggering a refresh on every re-render. Add() returns false when the key is already
    // present, which also guards against duplicate in-flight fetches.
    private readonly HashSet<string> updatesRequested = new();
    private readonly HashSet<string> reportsRequested = new();

    public ProgressReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>True once the project's progress updates have been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely nothing recorded".</summary>
    public bool UpdatesLoaded(string projectId) => updatesByProject.ContainsKey(projectId);

    public IReadOnlyList<ProgressUpdate> UpdatesCurrent(string projectId) =>
        updatesByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProgressUpdate>();

    public IReadOnlyList<ProgressReport> ReportsCurrent(string projectId) =>
        reportsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProgressReport>();

    /// <summary>
    /// Fires a one-time background load for the project's progress updates. Safe to call from
    /// render: it fetches at most once per project (until a mutation forces a refresh) and does
    /// nothing on subsequent calls, so it cannot drive a render/fetch loop.
    /// </summary>
    public void EnsureUpdates(string projectId, CancellationToken cancellationToken)
    {
        if (!updatesRequested.Add(projectId)) return;
        _ = LoadUpdatesAsync(projectId, cancellationToken);
    }

    public void EnsureReports(string projectId, CancellationToken cancellationToken)
    {
        if (!reportsRequested.Add(projectId)) return;
        _ = LoadReportsAsync(projectId, cancellationToken);
    }

    private async Task LoadUpdatesAsync(string projectId, CancellationToken cancellationToken)
    {
        try { await RefreshUpdatesAsync(projectId, cancellationToken); }
        catch { updatesRequested.Remove(projectId); } // allow a later retry; failure does not raise OnChanged, so no loop
    }

    private async Task LoadReportsAsync(string projectId, CancellationToken cancellationToken)
    {
        try { await RefreshReportsAsync(projectId, cancellationToken); }
        catch { reportsRequested.Remove(projectId); }
    }

    public async Task RefreshUpdatesAsync(string projectId, CancellationToken cancellationToken)
    {
        updatesByProject[projectId] = await queries.AskAsync(new ListProgressUpdatesForProject(projectId), cancellationToken);
        updatesRequested.Add(projectId);
        OnChanged?.Invoke();
    }

    public async Task RefreshReportsAsync(string projectId, CancellationToken cancellationToken)
    {
        reportsByProject[projectId] = await queries.AskAsync(new ListProgressReportsForProject(projectId), cancellationToken);
        reportsRequested.Add(projectId);
        OnChanged?.Invoke();
    }
}
