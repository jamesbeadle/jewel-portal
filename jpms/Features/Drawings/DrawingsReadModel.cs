using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Drawings;

public sealed class DrawingsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Drawing>> drawingsByProject = new();
    private readonly Dictionary<string, IReadOnlyList<DrawingRevision>> revisionsByDrawing = new();
    private readonly Dictionary<string, IReadOnlyList<DrawingFolder>> foldersByProject = new();

    // Tracks which keys have had a fetch started, so an *empty* result does not
    // keep re-triggering a refresh on every re-render. Add() returns false when the
    // key is already present, which also guards against duplicate in-flight fetches.
    private readonly HashSet<string> drawingsRequested = new();
    private readonly HashSet<string> revisionsRequested = new();
    private readonly HashSet<string> foldersRequested = new();

    // Keys whose LAST fetch failed. The key stays in the requested set — removing it there would
    // let the failure's own OnChanged re-render restart the fetch in a fail/notify loop — so
    // recovery is explicit: the Retry methods below, or MarkRevisionsStale on page entry. Without
    // these flags a failed fetch left the gate closed and pulsing forever ("Loading revisions"
    // doing nothing, reported 2026-08-28).
    private readonly HashSet<string> drawingsFailed = new();
    private readonly HashSet<string> revisionsFailed = new();

    public DrawingsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>True once the project's drawing register has been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely not found".</summary>
    public bool DrawingsLoaded(string projectId) => drawingsByProject.ContainsKey(projectId);

    public IReadOnlyList<Drawing> DrawingsCurrent(string projectId) =>
        drawingsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Drawing>();

    /// <summary>True once this drawing's revisions have been fetched at least once. The register
    /// landing says nothing about the revisions, so the preview and the revision list need this
    /// before they can claim there is no file.</summary>
    public bool RevisionsLoaded(string drawingId) => revisionsByDrawing.ContainsKey(drawingId);

    /// <summary>True when the last revisions fetch for this drawing failed. Gates pair this with
    /// <see cref="RevisionsLoaded"/> so a failure opens the gate with a message and a retry
    /// instead of pulsing forever.</summary>
    public bool RevisionsLoadFailed(string drawingId) => revisionsFailed.Contains(drawingId);

    /// <summary>True when the last register fetch for this project failed — same pairing as
    /// <see cref="RevisionsLoadFailed"/>, for the drawings register gate.</summary>
    public bool DrawingsLoadFailed(string projectId) => drawingsFailed.Contains(projectId);

    public IReadOnlyList<DrawingRevision> RevisionsCurrent(string drawingId) =>
        revisionsByDrawing.TryGetValue(drawingId, out var list) ? list : Array.Empty<DrawingRevision>();

    /// <summary>True once the project's drawing folders have been fetched at least once.</summary>
    public bool FoldersLoaded(string projectId) => foldersByProject.ContainsKey(projectId);

    public IReadOnlyList<DrawingFolder> FoldersCurrent(string projectId) =>
        foldersByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<DrawingFolder>();

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

    public void EnsureFolders(string projectId, CancellationToken cancellationToken)
    {
        if (!foldersRequested.Add(projectId)) return;
        _ = LoadFoldersAsync(projectId, cancellationToken);
    }

    private async Task LoadDrawingsAsync(string projectId, CancellationToken cancellationToken)
    {
        try { await RefreshDrawingsAsync(projectId, cancellationToken); }
        catch
        {
            // The key stays requested (see the failed-set note above) — the flag plus OnChanged
            // is what lets the waiting gate open and say the load failed rather than pulse on.
            drawingsFailed.Add(projectId);
            OnChanged?.Invoke();
        }
    }

    private async Task LoadRevisionsAsync(string drawingId, CancellationToken cancellationToken)
    {
        try { await RefreshRevisionsAsync(drawingId, cancellationToken); }
        catch
        {
            revisionsFailed.Add(drawingId);
            OnChanged?.Invoke();
        }
    }

    private async Task LoadFoldersAsync(string projectId, CancellationToken cancellationToken)
    {
        try { await RefreshFoldersAsync(projectId, cancellationToken); }
        catch { foldersRequested.Remove(projectId); }
    }

    /// <summary>Marks every cached revision list stale: the values stay readable, but the next
    /// EnsureRevisions call per drawing starts a fresh background fetch. Used on page entry so
    /// revision data (approval state, ambiguity) catches up with changes made elsewhere.</summary>
    public void MarkRevisionsStale()
    {
        revisionsRequested.Clear();
        // A fresh page entry is a fresh chance: failed drawings drop back to plain unloaded, so
        // the next read starts their fetch again.
        revisionsFailed.Clear();
    }

    /// <summary>Clears a drawing's failed state and starts its revisions fetch again — wired to
    /// the retry control a failed gate shows.</summary>
    public void RetryRevisions(string drawingId, CancellationToken cancellationToken)
    {
        revisionsFailed.Remove(drawingId);
        revisionsRequested.Remove(drawingId);
        EnsureRevisions(drawingId, cancellationToken);
    }

    /// <summary>Same retry, for a project's drawing register.</summary>
    public void RetryDrawings(string projectId, CancellationToken cancellationToken)
    {
        drawingsFailed.Remove(projectId);
        drawingsRequested.Remove(projectId);
        EnsureDrawings(projectId, cancellationToken);
    }

    public async Task RefreshDrawingsAsync(string projectId, CancellationToken cancellationToken)
    {
        drawingsByProject[projectId] = await queries.AskAsync(new ListDrawingsForProject(projectId), cancellationToken);
        drawingsRequested.Add(projectId);
        drawingsFailed.Remove(projectId);
        OnChanged?.Invoke();
    }

    public async Task RefreshRevisionsAsync(string drawingId, CancellationToken cancellationToken)
    {
        revisionsByDrawing[drawingId] = await queries.AskAsync(new ListRevisionsForDrawing(drawingId), cancellationToken);
        revisionsRequested.Add(drawingId);
        revisionsFailed.Remove(drawingId);
        OnChanged?.Invoke();
    }

    public async Task RefreshFoldersAsync(string projectId, CancellationToken cancellationToken)
    {
        foldersByProject[projectId] = await queries.AskAsync(new ListDrawingFoldersForProject(projectId), cancellationToken);
        foldersRequested.Add(projectId);
        OnChanged?.Invoke();
    }
}
