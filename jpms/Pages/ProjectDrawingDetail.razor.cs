using Jewel.JPMS.Features.Drawings;

namespace Jewel.JPMS.Pages;

public partial class ProjectDrawingDetail
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string DrawingId { get; set; } = "";

    // Session checked and the user is signed in — not "the drawing is here". The tab chrome shows
    // straight away; the register and the revisions each hold their own panel.
    private bool isUploading;
    private bool confirmingDelete;
    private bool deleteBusy;
    private string? deleteError;
    private bool extractBusy;
    private string? extractError;
    private DrawingExtractionPanel? extractionPanel;
    private bool moveBusy;
    private string? moveError;
    private string? moveConfirmation;
    private int moveConfirmationToken;
    private Drawing? drawing;

    private bool CanManage =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    // The register landing says nothing about this drawing's revisions — they are a separate fetch.
    private bool RevisionsReady => DrawingStore.RevisionsLoadedFor(DrawingId);

    private bool RevisionsFailed => DrawingStore.RevisionsFailedFor(DrawingId);

    private bool FoldersReady => DrawingStore.FoldersLoadedFor(ProjectId);
    private IReadOnlyList<DrawingFolder> Folders => DrawingStore.FoldersFor(ProjectId);

    // "Architect / Planning" — the full path, since the drawing may sit in a sub-folder.
    private string? FolderPath
    {
        get
        {
            var path = new DrawingFolderTree(Folders).PathOf(drawing?.DrawingFolderId);
            return path.Length == 0 ? null : path;
        }
    }

    private async Task MoveToFolderAsync(ChangeEventArgs args)
    {
        if (drawing is null || moveBusy) return;
        var folderId = string.IsNullOrEmpty(args.Value?.ToString()) ? null : args.Value.ToString();
        if (folderId == drawing.DrawingFolderId) return;
        moveBusy = true;
        moveError = null;
        moveConfirmation = null;
        try
        {
            await DrawingStore.MoveToFolderAsync(ProjectId, drawing.DrawingId, folderId, CancellationToken.None);
            // Wait for the register reload to land, so "Moving…" only clears once the select is
            // showing the SAVED state — then confirm what happened, since there is no dialog or
            // Save button to do the confirming.
            await DrawingStore.RefreshNowAsync(ProjectId, null, CancellationToken.None);
            var path = new DrawingFolderTree(Folders).PathOf(folderId);
            moveConfirmation = folderId is null
                ? "Moved out of its folder — now ungrouped"
                : $"Moved to “{(path.Length == 0 ? "the folder" : path)}”";
            _ = ClearMoveConfirmationLaterAsync(++moveConfirmationToken);
        }
        catch (Exception ex)
        {
            moveError = $"Move failed: {ex.Message}";
        }
        finally
        {
            moveBusy = false;
        }
    }

    // The confirmation fades on its own; the token keeps a slow old timer from wiping the
    // message of a newer move.
    private async Task ClearMoveConfirmationLaterAsync(int token)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        if (token != moveConfirmationToken || moveConfirmation is null) return;
        moveConfirmation = null;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        DrawingStore.OnChange += HandleChange;
        Bluebeam.OnChange += HandleChange;
        Reload();
        // Fetched once per session; until it lands the Extract button sits disabled with its tooltip.
        _ = Bluebeam.EnsureLoadedAsync();
    }

    protected override void OnParametersSet() => Reload();

    // Read from the cached register rather than a blocking query. A synchronous
    // wait on an async HTTP call (the old DrawingStore.Find) deadlocks on WebAssembly.
    private void Reload()
    {
        drawing = Siblings.FirstOrDefault(candidate => candidate.DrawingId == DrawingId);
        // The revision fetch is started by reading it, and every read now sits behind a closed
        // gate — so start it here or the gate waits on a load nothing ever asked for.
        _ = DrawingStore.RevisionsFor(DrawingId);
    }

    private IReadOnlyList<Drawing> Siblings => DrawingStore.DrawingsFor(ProjectId);

    private int CurrentIndex
    {
        get
        {
            var list = Siblings;
            for (var i = 0; i < list.Count; i++)
                if (list[i].DrawingId == DrawingId) return i;
            return -1;
        }
    }

    private Drawing? PreviousDrawing => CurrentIndex > 0 ? Siblings[CurrentIndex - 1] : null;

    private Drawing? NextDrawing =>
        CurrentIndex >= 0 && CurrentIndex < Siblings.Count - 1 ? Siblings[CurrentIndex + 1] : null;

    private (int index, int total)? Position =>
        CurrentIndex >= 0 ? (CurrentIndex + 1, Siblings.Count) : null;

    // The version to preview: the approved revision if there is one, otherwise the
    // most recently received revision that has a stored file.
    private DrawingRevision? PreviewRevision =>
        DrawingStore.RevisionsFor(DrawingId)
            .Where(revision => !string.IsNullOrEmpty(revision.BlobRef))
            .OrderByDescending(revision => revision.ApprovalStatus == DrawingApprovalStatus.Approved)
            .ThenByDescending(revision => revision.ReceivedAt)
            .FirstOrDefault();

    private static bool IsPdf(DrawingRevision revision) =>
        (revision.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase);

    // Extraction needs a previewable PDF revision and the shared Bluebeam connection.
    private bool CanExtract =>
        CanManage && Bluebeam.IsConnected && PreviewRevision is { } revision && IsPdf(revision);

    private string ExtractDisabledReason =>
        !Bluebeam.IsConnected
            ? "Extraction needs the Bluebeam connection — connect it under Admin → Integrations"
            : PreviewRevision is null
                ? "No stored revision to extract from"
                : "Only PDF revisions can be extracted";

    private async Task DoExtractData()
    {
        if (extractBusy || PreviewRevision is not { } revision) return;
        extractError = null;
        try
        {
            extractBusy = true;
            await DrawingStore.QueueExtractionAsync(
                ProjectId, DrawingId, revision.DrawingRevisionId, CancellationToken.None);
            if (extractionPanel is { } panel) await panel.RefreshAsync();
        }
        catch (Jewel.JPMS.Cqrs.CommandFailedException ex) { extractError = ex.Message; }
        catch { extractError = "Queueing the extraction didn't complete. Please try again."; }
        finally { extractBusy = false; }
    }

    private static bool IsImage(DrawingRevision revision) =>
        (revision.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private string FileUrl(DrawingRevision revision, bool inline) =>
        $"/api/drawings/revisions/{revision.DrawingRevisionId}/file{(inline ? "?inline=1" : "")}";

    private string DrawingHref(Drawing target) => $"/projects/{ProjectId}/drawings/{target.DrawingId}";

    private async Task DeleteDrawingAsync()
    {
        if (drawing is null || deleteBusy) return;
        deleteBusy = true;
        deleteError = null;
        try
        {
            await DrawingStore.DeleteDrawingAsync(ProjectId, drawing.DrawingId, CancellationToken.None);
            confirmingDelete = false;
            Nav.NavigateTo($"/projects/{ProjectId}/drawings");
        }
        catch (Exception ex)
        {
            confirmingDelete = false;
            deleteError = $"Delete failed: {ex.Message}";
        }
        finally
        {
            deleteBusy = false;
        }
    }

    private void HandleChange() { Reload(); StateHasChanged(); }

    public void Dispose()
    {
        DrawingStore.OnChange -= HandleChange;
        Bluebeam.OnChange -= HandleChange;
    }
}
