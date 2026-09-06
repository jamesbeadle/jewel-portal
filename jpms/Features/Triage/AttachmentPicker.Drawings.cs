using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Drawings;

namespace Jewel.JPMS.Features.Triage;

public partial class AttachmentPicker
{
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
                (_, 0) => $"{withoutFile} ticked {(withoutFile == 1 ? "document has" : "documents have")} no uploaded file yet — nothing to attach there.",
                (0, _) => $"{failedLoads.Count} {(failedLoads.Count == 1 ? "document" : "documents")} couldn't be checked for files — still ticked, press Attach again.",
                _ => $"{withoutFile + failedLoads.Count} ticked documents couldn't be attached — no file yet, or the file list couldn't be loaded (those stay ticked)."
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

    private Task AddThreadAttachmentAsync(string messageId, IntakeAttachment attachment) =>
        AddAsync(ComposeDraftAttachment.FromOriginal(messageId, attachment.Id, attachment.Name, attachment.Size));

    private Task AddRecordDocumentAsync(LinkableRecord record) =>
        AddAsync(ComposeDraftAttachment.FromRecordDocument(record));

    private async Task AddAsync(ComposeDraftAttachment attachment)
    {
        // The same file attached twice is a mistake, not a request for two copies.
        // (A mailbox attachment is identified by its message too — Graph attachment ids are scoped
        // to the message that carries them.)
        if (Attachments.Any(a => a.Source == attachment.Source && a.Source != ComposeAttachmentSource.Upload && a.Id == attachment.Id
                && (a.Source != ComposeAttachmentSource.OriginalMessage || a.SourceMessageId == attachment.SourceMessageId)))
            return;
        await AttachmentsChanged.InvokeAsync(Attachments.Append(attachment).ToList());
    }

    private Task RemoveAsync(ComposeDraftAttachment attachment) =>
        AttachmentsChanged.InvokeAsync(Attachments.Where(a => a.Key != attachment.Key).ToList());

    private string SourceButtonClass(Panel panel) =>
        "inline-flex items-center gap-1 rounded border px-2.5 py-1.5 "
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
