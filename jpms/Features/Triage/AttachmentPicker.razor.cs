using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Drawings;

namespace Jewel.JPMS.Features.Triage;

public partial class AttachmentPicker
{
    /// <summary>The attachments currently on the composed email. Owned by the parent; this
    /// component only proposes additions/removals through <see cref="AttachmentsChanged"/>.</summary>
    [Parameter] public IReadOnlyList<ComposeDraftAttachment> Attachments { get; set; } = Array.Empty<ComposeDraftAttachment>();
    [Parameter] public EventCallback<IReadOnlyList<ComposeDraftAttachment>> AttachmentsChanged { get; set; }

    /// <summary>Projects offered by the drawings/photos panels' project select.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    /// <summary>Pre-seeds the panels' project select (e.g. the reply form's chosen project).</summary>
    [Parameter] public string? ProjectId { get; set; }

    /// <summary>The opened email's own attachments, offered for forwarding; null hides the tab.</summary>
    [Parameter] public IReadOnlyList<IntakeAttachment>? OriginalAttachments { get; set; }
    [Parameter] public string? OriginalMessageId { get; set; }

    private const long MaxFileBytes = 25_000_000;

    private enum Panel { None, Drawings, Photos, Records, Original }
    private Panel openPanel = Panel.None;
    private string panelProjectId = "";
    private string? expandedDrawingId;
    private string? oversizeNote;

    // Drawings panel state: the register rendered as its folder tree (reusing DrawingFolderTree,
    // the same nesting the register table draws), a search box that flattens matches, and a
    // multi-select basket. Ticking a drawing means "its best file" (approved, else latest upload),
    // resolved when Attach is pressed; expanding a drawing lets a specific revision be ticked.
    private string drawingSearch = "";
    private readonly HashSet<string> collapsedFolderIds = new();
    private readonly HashSet<string> selectedDrawingIds = new();
    private readonly Dictionary<string, DrawingRevision> selectedRevisionsById = new();
    private bool attachingSelected;
    private string? attachNote;
    private DrawingFolderTree drawingTree = new(Array.Empty<DrawingFolder>());
    private const int DrawingResultCap = 100;
    private const string UngroupedFolderKey = "";
    private const double DrawingIndentRem = 1.25;

    private sealed record DrawingGroup(string Key, DrawingFolder? Folder, int Depth, IReadOnlyList<Drawing> Drawings);

    // System-documents panel state. Records are fetched once per project for the composer's life
    // (a picker list, not a live view); absence from the dictionary is the honest "not fetched
    // yet", so the panel shows Loading rather than an empty answer it hasn't got.
    private readonly Dictionary<string, IReadOnlyList<LinkableRecord>> recordsByProject = new();
    private string? recordsFailedFor;
    private string recordSearch = "";
    private bool recordsIncludeInactive;
    private const int RecordResultCap = 50;

    private IReadOnlyList<LinkableRecord> FilteredRecords
    {
        get
        {
            if (!recordsByProject.TryGetValue(panelProjectId, out var pool)) return Array.Empty<LinkableRecord>();
            var live = recordsIncludeInactive ? pool : pool.Where(r => r.IsActive);
            var needle = recordSearch.Trim();
            var matches = needle.Length == 0
                ? live
                : live.Where(r => r.Reference.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || r.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || (r.Summary?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
            return matches.Take(RecordResultCap).ToList();
        }
    }

    protected override void OnInitialized()
    {
        Drawings.OnChange += StoreChanged;
        Progress.OnChange += StoreChanged;
    }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(panelProjectId) && !string.IsNullOrEmpty(ProjectId))
            panelProjectId = ProjectId!;
    }

    private void StoreChanged() => InvokeAsync(() =>
    {
        // Folders may have just landed (or changed elsewhere) — rebuild the nesting they define.
        RebuildDrawingTree();
        StateHasChanged();
    });

    private async Task TogglePanel(Panel panel)
    {
        openPanel = openPanel == panel ? Panel.None : panel;
        oversizeNote = null;
        if (panel is Panel.Drawings) ResetDrawingPanelState(); // opening starts clean; closing drops a stale basket
        if (openPanel is Panel.Records) recordsFailedFor = null; // reopening retries a failed load
        if (string.IsNullOrEmpty(panelProjectId)) return;
        if (openPanel is Panel.Drawings) Drawings.Refresh(panelProjectId);
        if (openPanel is Panel.Photos) Progress.Refresh(panelProjectId);
        if (openPanel is Panel.Records) await LoadRecordsAsync(panelProjectId);
    }

    private async Task OnPanelProjectChanged(ChangeEventArgs e)
    {
        panelProjectId = e.Value?.ToString() ?? "";
        ResetDrawingPanelState(); // a basket of one project's drawings must not survive into another
        if (string.IsNullOrEmpty(panelProjectId)) return;
        if (openPanel == Panel.Drawings) Drawings.Refresh(panelProjectId);
        if (openPanel == Panel.Photos) Progress.Refresh(panelProjectId);
        if (openPanel == Panel.Records) await LoadRecordsAsync(panelProjectId);
    }

    private async Task LoadRecordsAsync(string projectId)
    {
        if (recordsByProject.ContainsKey(projectId)) return;
        try
        {
            // Both record families that carry an official PDF: requests (RFI, NOD, EOT…) and
            // variation orders. The variation provider lists every stage of a project's
            // variations — a still-quoting order is offered under its VOQ identity, an approved
            // one under its V-ref — so a VO can be attached and sent at any point in its life.
            var requests = await Intake.ListLinkableRecordsAsync(projectId, RecordType.Request);
            var variations = await Intake.ListLinkableRecordsAsync(projectId, RecordType.Variation);
            // Tender enquiries carry the PQQ response — the third official document.
            var tenderEnquiries = await Intake.ListLinkableRecordsAsync(projectId, RecordType.TenderEnquiry);
            recordsByProject[projectId] = requests.Concat(variations).Concat(tenderEnquiries).ToList();
        }
        catch
        {
            // The query client has reported the failure; the panel says so and reopening retries.
            recordsFailedFor = projectId;
        }
    }

    private void ToggleDrawing(string drawingId)
    {
        expandedDrawingId = expandedDrawingId == drawingId ? null : drawingId;
        // Expanding is the ask: the "Loading revisions…" row never reads RevisionsFor (only the
        // loaded branch does), so without this kick the fetch never started and the row said
        // "Loading revisions…" forever (reported 2026-08-28). The read fires the one-time
        // background load; the store's OnChange re-renders when it lands.
        if (expandedDrawingId is not null) _ = Drawings.RevisionsFor(expandedDrawingId);
    }

    // ---- Drawings panel: tree, search and the multi-select basket ----

    private void ResetDrawingPanelState()
    {
        drawingSearch = "";
        collapsedFolderIds.Clear();
        expandedDrawingId = null;
        ClearDrawingSelection();
        RebuildDrawingTree();
    }

    private void RebuildDrawingTree() =>
        drawingTree = new DrawingFolderTree(
            string.IsNullOrEmpty(panelProjectId) ? Array.Empty<DrawingFolder>() : Drawings.FoldersFor(panelProjectId));

    private bool HasDrawingFolders => drawingTree.Nodes.Count > 0;

    // Mirrors the register table's grouping: folders in tree order, each drawing under its folder,
    // unknown-folder drawings safely in Ungrouped (last). Folders with nothing anywhere beneath
    // them are noise in a picker and are skipped — unlike the register, nothing is managed here.
    private IEnumerable<DrawingGroup> DrawingGroups
    {
        get
        {
            var knownFolderIds = drawingTree.Nodes.Select(node => node.Folder.DrawingFolderId).ToHashSet();
            var byFolder = Drawings.DrawingsFor(panelProjectId)
                .GroupBy(drawing => drawing.DrawingFolderId is { } folderId && knownFolderIds.Contains(folderId) ? folderId : UngroupedFolderKey)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<Drawing>)group.ToList());

            foreach (var node in drawingTree.Nodes)
            {
                if (!drawingTree.SubtreeIds(node.Folder.DrawingFolderId).Any(byFolder.ContainsKey)) continue;
                byFolder.TryGetValue(node.Folder.DrawingFolderId, out var drawings);
                yield return new DrawingGroup(node.Folder.DrawingFolderId, node.Folder, node.Depth, drawings ?? Array.Empty<Drawing>());
            }

            if (byFolder.TryGetValue(UngroupedFolderKey, out var ungrouped) && ungrouped.Count > 0)
                yield return new DrawingGroup(UngroupedFolderKey, null, 0, ungrouped);
        }
    }

    // A collapsed folder hides its whole sub-tree, not just its own drawings.
    private IEnumerable<DrawingGroup> VisibleDrawingSections =>
        DrawingGroups.Where(group => !drawingTree.HasAncestor(group.Folder?.DrawingFolderId, collapsedFolderIds));

    private bool HasDrawingSearch => drawingSearch.Trim().Length > 0;

    private IReadOnlyList<Drawing> DrawingSearchResults
    {
        get
        {
            var needle = drawingSearch.Trim();
            return Drawings.DrawingsFor(panelProjectId)
                .Where(drawing => MatchesDrawingSearch(drawing, needle))
                .Take(DrawingResultCap)
                .ToList();
        }
    }

    // Folder path counts as a match, so searching "electrical" finds everything filed under an
    // Electrical folder even when the file names don't say so.
    private bool MatchesDrawingSearch(Drawing drawing, string needle) =>
        DrawingNaming.Code(drawing).Contains(needle, StringComparison.OrdinalIgnoreCase)
        || DrawingNaming.Name(drawing).Contains(needle, StringComparison.OrdinalIgnoreCase)
        || (drawing.LatestFileName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || FolderPathFor(drawing).Contains(needle, StringComparison.OrdinalIgnoreCase);

    private string FolderPathFor(Drawing drawing) => drawingTree.PathOf(drawing.DrawingFolderId);

    private IReadOnlyList<Drawing> SubtreeDrawingsOf(DrawingGroup group)
    {
        if (group.Folder is null) return group.Drawings;
        var ids = drawingTree.SubtreeIds(group.Folder.DrawingFolderId).ToHashSet();
        return Drawings.DrawingsFor(panelProjectId)
            .Where(drawing => drawing.DrawingFolderId is { } folderId && ids.Contains(folderId))
            .ToList();
    }

    private int FolderSubtreeCount(DrawingGroup group) => SubtreeDrawingsOf(group).Count;

    private bool FolderFullySelected(DrawingGroup group)
    {
        var subtree = SubtreeDrawingsOf(group);
        return subtree.Count > 0 && subtree.All(drawing => selectedDrawingIds.Contains(drawing.DrawingId));
    }

    private void ToggleFolderSelection(DrawingGroup group)
    {
        var subtree = SubtreeDrawingsOf(group);
        if (subtree.Count == 0) return;
        if (subtree.All(drawing => selectedDrawingIds.Contains(drawing.DrawingId)))
            foreach (var drawing in subtree) selectedDrawingIds.Remove(drawing.DrawingId);
        else
            foreach (var drawing in subtree) selectedDrawingIds.Add(drawing.DrawingId);
    }

    private bool IsFolderCollapsed(string key) => collapsedFolderIds.Contains(key);

    private void ToggleFolderCollapse(string key)
    {
        if (!collapsedFolderIds.Add(key)) collapsedFolderIds.Remove(key);
    }

    private static string IndentStyle(int depth) =>
        depth <= 0 ? "" : $"padding-left: {depth * DrawingIndentRem}rem";

    private void ToggleDrawingSelection(string drawingId)
    {
        if (!selectedDrawingIds.Add(drawingId)) selectedDrawingIds.Remove(drawingId);
        attachNote = null;
    }

    private void ToggleRevisionSelection(DrawingRevision revision)
    {
        if (!selectedRevisionsById.Remove(revision.DrawingRevisionId))
            selectedRevisionsById[revision.DrawingRevisionId] = revision;
        attachNote = null;
    }

    private int SelectedDrawingCount => selectedDrawingIds.Count + selectedRevisionsById.Count;

    private void ClearDrawingSelection()
    {
        selectedDrawingIds.Clear();
        selectedRevisionsById.Clear();
        attachNote = null;
    }

    private async Task AttachSelectedDrawingsAsync()
    {
        if (attachingSelected || SelectedDrawingCount == 0) return;
        attachingSelected = true;
        attachNote = null;
        try
        {
            // One file per revision however it was ticked — via its drawing, its folder, or the
            // revision itself — and never one already on the email.
            var seenRevisionIds = Attachments
                .Where(a => a.Source == ComposeAttachmentSource.Drawing)
                .Select(a => a.Id)
                .ToHashSet();
            var additions = new List<ComposeDraftAttachment>();
            var withoutFile = 0;
            var failedLoads = new HashSet<string>();

            foreach (var drawingId in selectedDrawingIds.ToList())
            {
                await Drawings.EnsureRevisionsNowAsync(drawingId, CancellationToken.None);
                if (!Drawings.RevisionsLoadedFor(drawingId)) { failedLoads.Add(drawingId); continue; }
                var best = BestRevision(Drawings.RevisionsFor(drawingId));
                if (best is null) { withoutFile++; continue; }
                if (seenRevisionIds.Add(best.DrawingRevisionId))
                    additions.Add(ComposeDraftAttachment.FromDrawing(best.DrawingRevisionId, best.FileName, best.FileSizeBytes ?? 0));
            }

            foreach (var revision in selectedRevisionsById.Values)
            {
                if (seenRevisionIds.Add(revision.DrawingRevisionId))
                    additions.Add(ComposeDraftAttachment.FromDrawing(revision.DrawingRevisionId, revision.FileName, revision.FileSizeBytes ?? 0));
            }

            if (additions.Count > 0)
                await AttachmentsChanged.InvokeAsync(Attachments.Concat(additions).ToList());

            // Attached and file-less drawings leave the basket; failed loads STAY ticked so
            // "press Attach again" retries exactly them.
            selectedDrawingIds.Clear();
            foreach (var drawingId in failedLoads) selectedDrawingIds.Add(drawingId);
            selectedRevisionsById.Clear();

            attachNote = (withoutFile, failedLoads.Count) switch
            {
                (0, 0) => null,
                (_, 0) => $"{withoutFile} ticked {(withoutFile == 1 ? "drawing has" : "drawings have")} no uploaded file yet — nothing to attach there.",
                (0, _) => $"{failedLoads.Count} {(failedLoads.Count == 1 ? "drawing" : "drawings")} couldn't be checked for files — still ticked, press Attach again.",
                _ => $"{withoutFile + failedLoads.Count} ticked drawings couldn't be attached — no file yet, or the file list couldn't be loaded (those stay ticked)."
            };
        }
        finally
        {
            attachingSelected = false;
        }
    }

    /// <summary>The file a ticked drawing means: its approved revision, else its latest
    /// non-archived upload, else its latest upload of any kind; null when nothing is uploaded.</summary>
    private static DrawingRevision? BestRevision(IReadOnlyList<DrawingRevision> revisions) =>
        revisions.Where(r => r.ApprovalStatus == DrawingApprovalStatus.Approved).OrderByDescending(r => r.ReceivedAt).FirstOrDefault()
        ?? revisions.Where(r => r.ApprovalStatus != DrawingApprovalStatus.Archived).OrderByDescending(r => r.ReceivedAt).FirstOrDefault()
        ?? revisions.OrderByDescending(r => r.ReceivedAt).FirstOrDefault();


    private async Task OnFilesPicked(InputFileChangeEventArgs e)
    {
        oversizeNote = null;
        var added = new List<ComposeDraftAttachment>(Attachments);
        foreach (var file in e.GetMultipleFiles(20))
        {
            if (file.Size > MaxFileBytes)
            {
                oversizeNote = $"{file.Name} is over 25 MB — attach a smaller file.";
                continue;
            }
            added.Add(ComposeDraftAttachment.FromUpload(file));
        }
        await AttachmentsChanged.InvokeAsync(added);
    }

    private Task AddPhotoAsync(ProgressPhoto photo) =>
        AddAsync(ComposeDraftAttachment.FromProgressPhoto(photo.ProgressPhotoId, photo.FileName, photo.FileSizeBytes));

    private Task AddOriginalAsync(IntakeAttachment attachment) =>
        AddAsync(ComposeDraftAttachment.FromOriginal(OriginalMessageId ?? "", attachment.Id, attachment.Name, attachment.Size));

    private Task AddRecordDocumentAsync(LinkableRecord record) =>
        AddAsync(ComposeDraftAttachment.FromRecordDocument(record));

    private async Task AddAsync(ComposeDraftAttachment attachment)
    {
        // The same file attached twice is a mistake, not a request for two copies.
        if (Attachments.Any(a => a.Source == attachment.Source && a.Source != ComposeAttachmentSource.Upload && a.Id == attachment.Id))
            return;
        await AttachmentsChanged.InvokeAsync(Attachments.Append(attachment).ToList());
    }

    private Task RemoveAsync(ComposeDraftAttachment attachment) =>
        AttachmentsChanged.InvokeAsync(Attachments.Where(a => a.Key != attachment.Key).ToList());

    private string SourceButtonClass(Panel panel) =>
        "inline-flex items-center gap-1 rounded-lg border px-2.5 py-1.5 "
        + (openPanel == panel
            ? "border-accent text-accent bg-accent/5"
            : "border-line text-content-muted hover:text-content hover:border-line-strong");

    private static string AttachmentGlyph(ComposeAttachmentSource source) => source switch
    {
        ComposeAttachmentSource.Drawing => "📐",
        ComposeAttachmentSource.ProgressPhoto => "📷",
        ComposeAttachmentSource.OriginalMessage => "✉️",
        ComposeAttachmentSource.RecordDocument => "📄",
        _ => "📎"
    };

    private static string SizeLabel(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:0.#} MB"
        : bytes >= 1024 ? $"{bytes / 1024.0:0.#} KB"
        : bytes > 0 ? $"{bytes} B" : "";

    public void Dispose()
    {
        Drawings.OnChange -= StoreChanged;
        Progress.OnChange -= StoreChanged;
    }
}
