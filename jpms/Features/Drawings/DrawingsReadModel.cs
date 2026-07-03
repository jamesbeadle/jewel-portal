using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Drawings;

public sealed class DrawingsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Drawing>> drawingsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<DrawingRevision>> revisionsByDrawing = new();

    // Tracks which keys have had a fetch started, so an *empty* result does not
    // keep re-triggering a refresh on every re-render. Add() returns false when the
    // key is already present, which also guards against duplicate in-flight fetches.
    private readonly HashSet<string> drawingsRequested = new();
    private readonly HashSet<string> revisionsRequested = new();

    public DrawingsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>True once the project's drawing register has been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely not found".</summary>
    public bool DrawingsLoaded(string projectId) => drawingsByProject.ContainsKey(projectId);

    public IReadOnlyList<Drawing> DrawingsCurrent(string projectId) =>
        drawingsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Drawing>();

    public IReadOnlyList<DrawingRevision> RevisionsCurrent(string drawingId) =>
        revisionsByDrawing.TryGetValue(drawingId, out var list) ? list : Array.Empty<DrawingRevision>();

    /// <summary>
    /// Fires a one-time background load for the project's drawings. Safe to call from
    /// render: it fetches at most once per project (until a mutation forces a refresh)
    /// and does nothing on subsequent calls, so it cannot drive a render/fetch loop.
    /// </summary>
    public void EnsureDrawings(string projectId, CancellationToken cancellationToken)
    {
        if (!drawingsRequested.Add(projectId)) return;
        _ = LoadDrawingsAsync(projectId, cancellationToken);
    }

    public void EnsureRevisions(string drawingId, CancellationToken cancellationToken)
    {
        if (!revisionsRequested.Add(drawingId)) return;
        _ = LoadRevisionsAsync(drawingId, cancellationToken);
    }

    private async Task LoadDrawingsAsync(string projectId, CancellationToken cancellationToken)
    {
        try { await RefreshDrawingsAsync(projectId, cancellationToken); }
        catch { drawingsRequested.Remove(projectId); } // allow a later retry; failure does not raise OnChanged, so no loop
    }

    private async Task LoadRevisionsAsync(string drawingId, CancellationToken cancellationToken)
    {
        try { await RefreshRevisionsAsync(drawingId, cancellationToken); }
        catch { revisionsRequested.Remove(drawingId); }
    }

    /// <summary>Marks every cached revision list stale: the values stay readable, but the next
    /// EnsureRevisions call per drawing starts a fresh background fetch. Used on page entry so
    /// revision data (approval state, ambiguity) catches up with changes made elsewhere.</summary>
    public void MarkRevisionsStale() => revisionsRequested.Clear();

    public async Task RefreshDrawingsAsync(string projectId, CancellationToken cancellationToken)
    {
        drawingsByProject[projectId] = await queries.AskAsync(new ListDrawingsForProject(projectId), cancellationToken);
        drawingsRequested.Add(projectId);
        OnChanged?.Invoke();
    }

    public async Task RefreshRevisionsAsync(string drawingId, CancellationToken cancellationToken)
    {
        revisionsByDrawing[drawingId] = await queries.AskAsync(new ListRevisionsForDrawing(drawingId), cancellationToken);
        revisionsRequested.Add(drawingId);
        OnChanged?.Invoke();
    }
}
