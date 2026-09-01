using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Features.Drawings;

namespace Jewel.JPMS.Pages;

public partial class ProjectDrawings
{
    [Parameter] public string ProjectId { get; set; } = "";

    private bool isLoaded;
    private bool isUploading;
    private bool confirmingExtractAll;
    private bool extractAllBusy;
    private string? extractAllError;
    private string? extractAllNote;
    private bool approvedOnly;

    // Set by a folder row's upload button: the upload panel opens with this folder preselected.
    private DrawingFolder? uploadIntoFolder;

    // Folder dialogs: one name dialog for create/rename, one confirm for delete.
    private bool folderDialogOpen;
    private DrawingFolder? renamingFolder;
    private DrawingFolder? parentFolder;
    private DrawingFolder? deletingFolder;
    private string folderName = "";
    private string? folderError;
    private bool folderBusy;

    private IReadOnlyList<Drawing> Drawings => DrawingStore.DrawingsFor(ProjectId);
    private IReadOnlyList<DrawingFolder> Folders => DrawingStore.FoldersFor(ProjectId);
    private DrawingFolderTree Tree => new(Folders);

    // Counted from the tree, not ParentDrawingFolderId: the tree promotes a folder whose parent
    // is missing from the list to the top level, so these two always sum to Folders.Count and
    // always match the rows on show.
    private int TopLevelFolderCount => Tree.Nodes.Count(node => node.Depth == 0);
    private int SubFolderCount => Folders.Count - TopLevelFolderCount;
    private IReadOnlyList<DrawingRevision> Ambiguous => DrawingStore.AmbiguousFor(ProjectId);

    private IReadOnlyList<Drawing> VisibleDrawings =>
        approvedOnly
            ? Drawings.Where(drawing => drawing.HasApprovedRevision).ToList()
            : Drawings;

    private string FolderDialogTitle
    {
        get
        {
            if (renamingFolder is not null) return $"Rename “{renamingFolder.Name}”";
            if (parentFolder is not null) return $"New sub-folder in “{parentFolder.Name}”";
            return "New folder";
        }
    }

    private bool CanManage =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    private ExcelWorkbook? BuildExportWorkbook(bool includeUnapproved)
    {
        // "Include unapproved" (offered while the approved-only toggle is on) exports the whole
        // register — the Status column separates approved from pending.
        var drawings = includeUnapproved ? Drawings : VisibleDrawings;
        if (drawings.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Drawings",
            new ExcelColumn("Folder"),
            new ExcelColumn("Code"),
            new ExcelColumn("Title"),
            new ExcelColumn("File"),
            new ExcelColumn("Latest approved"),
            new ExcelColumn("Status"),
            new ExcelColumn("Pipeline"),
            new ExcelColumn("Added", ExcelFormat.Date));

        // Rows in register order: each folder in tree order, then Ungrouped — the same shape as the table.
        var tree = Tree;
        foreach (var drawing in InRegisterOrder(drawings, tree))
        {
            sheet.AddRow(
                FolderPathFor(drawing, tree),
                drawing.DrawingCode,
                drawing.Title,
                drawing.LatestFileName,
                DrawingNaming.ApprovedRevisionText(drawing),
                StatusText(drawing),
                PipelineText(drawing),
                drawing.CreatedAt.LocalDateTime);
        }
        return workbook;
    }

    private IEnumerable<Drawing> InRegisterOrder(IReadOnlyList<Drawing> drawings, DrawingFolderTree tree)
    {
        foreach (var node in tree.Nodes)
            foreach (var drawing in drawings.Where(candidate => candidate.DrawingFolderId == node.Folder.DrawingFolderId))
                yield return drawing;
        foreach (var drawing in drawings.Where(candidate => candidate.DrawingFolderId is null
            || Folders.All(folder => folder.DrawingFolderId != candidate.DrawingFolderId)))
            yield return drawing;
    }

    // "Architect / Planning" — the full path, so a sub-folder's drawings say where they live.
    private static string? FolderPathFor(Drawing drawing, DrawingFolderTree tree)
    {
        var path = tree.PathOf(drawing.DrawingFolderId);
        return path.Length == 0 ? null : path;
    }

    // Mirrors DrawingsTable's Status badges: pending/archived counts, or "Approved" when neither
    // applies and a current approval exists — blank when none of those hold.
    private static string? StatusText(Drawing drawing)
    {
        var parts = new List<string>();
        if (drawing.UnapprovedCount > 0) parts.Add($"{drawing.UnapprovedCount} pending");
        if (drawing.ArchivedCount > 0) parts.Add($"{drawing.ArchivedCount} archived");
        if (drawing.UnapprovedCount == 0 && drawing.ArchivedCount == 0 && drawing.HasApprovedRevision)
            parts.Add("Approved");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    // Mirrors DrawingsTable's Pipeline badges: metadata-extraction and change-analysis status of
    // the latest revision.
    private static string PipelineText(Drawing drawing) =>
        $"{(drawing.LatestMetadataExtractedAt is not null ? "Metadata ✓" : "Not extracted")} · "
        + $"{(drawing.LatestAnalysedAt is not null ? "Analysed ✓" : "Not analysed")}";

    private void ToggleUploadPanel()
    {
        isUploading = !isUploading;
        if (!isUploading) uploadIntoFolder = null;
    }

    private void CloseUploadPanel()
    {
        isUploading = false;
        uploadIntoFolder = null;
    }

    // A folder row's upload button: open (or re-aim) the panel with that folder already chosen.
    private void OpenUploadTo(DrawingFolder folder)
    {
        uploadIntoFolder = folder;
        isUploading = true;
    }

    private void OpenNewFolder() => OpenNewFolderIn(parent: null);

    private void OpenNewSubFolder(DrawingFolder parent) => OpenNewFolderIn(parent);

    private void OpenNewFolderIn(DrawingFolder? parent)
    {
        renamingFolder = null;
        parentFolder = parent;
        folderName = "";
        folderError = null;
        folderDialogOpen = true;
    }

    private void OpenRenameFolder(DrawingFolder folder)
    {
        renamingFolder = folder;
        parentFolder = null;
        folderName = folder.Name;
        folderError = null;
        folderDialogOpen = true;
    }

    private void OpenDeleteFolder(DrawingFolder folder)
    {
        folderError = null;
        deletingFolder = folder;
    }

    private void CloseFolderDialog()
    {
        folderDialogOpen = false;
        renamingFolder = null;
        parentFolder = null;
        folderError = null;
    }

    private async Task SaveFolderAsync()
    {
        if (folderBusy || string.IsNullOrWhiteSpace(folderName)) return;
        folderBusy = true;
        folderError = null;
        try
        {
            if (renamingFolder is null)
                await DrawingStore.CreateFolderAsync(ProjectId, folderName.Trim(), parentFolder?.DrawingFolderId, CancellationToken.None);
            else
                await DrawingStore.RenameFolderAsync(ProjectId, renamingFolder.DrawingFolderId, folderName.Trim(), CancellationToken.None);
            CloseFolderDialog();
        }
        catch (Exception ex)
        {
            folderError = ex.Message;
        }
        finally
        {
            folderBusy = false;
        }
    }

    private async Task DeleteFolderAsync()
    {
        if (folderBusy || deletingFolder is null) return;
        folderBusy = true;
        folderError = null;
        try
        {
            await DrawingStore.DeleteFolderAsync(ProjectId, deletingFolder.DrawingFolderId, CancellationToken.None);
            deletingFolder = null;
        }
        catch (Exception ex)
        {
            folderError = ex.Message;
        }
        finally
        {
            folderBusy = false;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        DrawingStore.OnChange += StateHasChanged;
        Bluebeam.OnChange += StateHasChanged;
        // Refresh on entry: cached drawings render immediately, then update when the
        // background reload lands — so navigating back to this tab never shows stale data.
        DrawingStore.Refresh(ProjectId);
        isLoaded = true;
        // Fetched once per session; the Extract all button only appears once it reports connected.
        _ = Bluebeam.EnsureLoadedAsync();
    }

    private async Task ExtractAllAsync()
    {
        if (extractAllBusy) return;
        extractAllError = null;
        try
        {
            extractAllBusy = true;
            var queued = await DrawingStore.QueueAllExtractionsAsync(ProjectId, CancellationToken.None);
            confirmingExtractAll = false;
            extractAllNote = queued == 0
                ? "Nothing to queue — every drawing's latest PDF revision is already extracted (or already in the queue)."
                : $"Queued {queued} drawing(s) for extraction. Each drawing's page shows its progress.";
        }
        catch (Jewel.JPMS.Cqrs.CommandFailedException ex) { extractAllError = ex.Message; }
        catch { extractAllError = "Queueing didn't complete. Please try again."; }
        finally { extractAllBusy = false; }
    }

    public void Dispose()
    {
        DrawingStore.OnChange -= StateHasChanged;
        Bluebeam.OnChange -= StateHasChanged;
    }
}
