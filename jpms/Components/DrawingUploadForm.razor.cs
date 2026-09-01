

namespace Jewel.JPMS.Components;

public partial class DrawingUploadForm
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    [Parameter] public EventCallback OnUploaded { get; set; }

    /// <summary>Folder to preselect in the picker — set when the form was opened from a folder's
    /// own upload button, so the files land in that folder without another choice to make.</summary>
    [Parameter] public string? InitialFolderId { get; set; }

    // Well beyond any real drawing issue; the browser needs SOME ceiling for GetMultipleFiles.
    private const int MaxBulkFiles = 200;

    private DrawingFolderPicker? folderPicker;
    private List<IBrowserFile> selectedFiles = new();
    private bool isRevision;
    private int dragDepth;
    private string selectedDrawingId = "";
    private string drawingCode = "";
    private string title = "";
    private string revisionLabel = "";
    private string issuedBy = "";
    private bool busy;
    private string? error;

    // Bulk progress and recovery: which file is in flight, what failed (kept in selectedFiles so
    // Upload retries exactly the failures), and drawings already registered for a file whose
    // revision upload then failed — so a retry doesn't register a second, empty drawing.
    private int bulkDone;
    private int bulkTotal;
    private readonly List<(string Name, string Message)> failures = new();
    private readonly Dictionary<IBrowserFile, string> registeredIds = new();

    private bool IsBulk => selectedFiles.Count > 1;
    private bool IsDragOver => dragDepth > 0;

    private IReadOnlyList<Drawing> ExistingDrawings => DrawingStore.DrawingsFor(ProjectId);

    // The idle labels say WHERE the upload goes — "to the register" — so this button reads as the
    // save-to-the-portal step, distinct from the page button that merely opens this panel.
    private string SubmitLabel =>
        busy
            ? IsBulk ? $"Uploading {bulkDone} of {bulkTotal}…" : "Uploading…"
            : IsBulk ? $"Upload {selectedFiles.Count} drawings to the register"
            : isRevision ? "Upload revision to the register" : "Upload drawing to the register";

    private void SetMode(bool isRevision)
    {
        this.isRevision = isRevision;
        error = null;
    }

    private void OnFilesSelected(InputFileChangeEventArgs e)
    {
        if (e.FileCount > MaxBulkFiles)
        {
            error = $"That's {e.FileCount} files — upload at most {MaxBulkFiles} at a time.";
            return;
        }
        error = null;

        // A revision is one file into one drawing — a new pick REPLACES the old one.
        if (isRevision)
        {
            selectedFiles = e.GetMultipleFiles(1).ToList();
            failures.Clear();
            registeredIds.Clear();
            MaybeExtractRevisionLabel();
            return;
        }

        // New-drawing mode APPENDS: a second drag adds to the selection rather than wiping it,
        // so a register can be gathered from several drops (each file has its ✕ to take it back
        // out). A file already selected — same name and size — is skipped, so dropping the same
        // batch twice doesn't double anything up. Kept files keep their failure notes and
        // already-registered drawing ids, so a retry after adding more still works.
        var incoming = e.GetMultipleFiles(MaxBulkFiles)
            .Where(file => !selectedFiles.Any(existing => existing.Name == file.Name && existing.Size == file.Size))
            .ToList();
        if (selectedFiles.Count + incoming.Count > MaxBulkFiles)
        {
            error = $"That would make {selectedFiles.Count + incoming.Count} files — upload at most {MaxBulkFiles} at a time.";
            return;
        }
        selectedFiles.AddRange(incoming);
        MaybeExtractRevisionLabel();
    }

    private void MaybeExtractRevisionLabel()
    {
        if (selectedFiles.Count != 1 || !string.IsNullOrWhiteSpace(revisionLabel)) return;
        var extracted = ExtractRevisionLabel(selectedFiles[0].Name);
        if (extracted.Length > 0) revisionLabel = extracted;
    }

    private void RemoveFile(IBrowserFile file)
    {
        selectedFiles.Remove(file);
        registeredIds.Remove(file);
        error = null;
    }

    private void ClearSelection()
    {
        selectedFiles = new List<IBrowserFile>();
        failures.Clear();
        registeredIds.Clear();
        error = null;
    }

    private async Task HandleUpload()
    {
        if (selectedFiles.Count == 0) { error = "Choose a file to upload."; return; }

        if (IsBulk)
        {
            if (isRevision) { error = "A revision is one file into one drawing — switch to “New drawing” for a bulk upload."; return; }
            await HandleBulkUpload();
            return;
        }

        var selectedFile = selectedFiles[0];

        string drawingId;
        if (isRevision)
        {
            if (string.IsNullOrEmpty(selectedDrawingId)) { error = "Select the drawing this revision belongs to."; return; }
            drawingId = selectedDrawingId;
        }
        else
        {
            // A blank code can never clash — only a given code is checked against the register.
            var duplicate = DuplicateOfCode(drawingCode.Trim());
            if (duplicate is not null)
            {
                error = $"“{duplicate.DrawingCode}” is already in the register. Switch to “Revision of existing drawing” to add a new version of it.";
                return;
            }
            drawingId = "";
        }

        if (!isRevision && folderPicker?.Problem is { } folderProblem)
        {
            error = folderProblem;
            return;
        }

        busy = true;
        error = null;
        try
        {
            if (!isRevision)
            {
                // Resolve the folder first: the picker's "New folder…" path creates (or finds, by
                // name) the folder, then the drawing is registered straight into it.
                var folderId = folderPicker is null ? null : await folderPicker.ResolveFolderAsync(CancellationToken.None);

                // A file left over from a partly-failed bulk upload may already have its drawing
                // registered — upload into that rather than registering a second, empty one.
                if (registeredIds.TryGetValue(selectedFile, out var registeredId))
                {
                    drawingId = registeredId;
                }
                else
                {
                    drawingId = (await DrawingStore.RegisterDrawingAsync(ProjectId, drawingCode.Trim(), title.Trim(), folderId, CancellationToken.None)).DrawingId;
                    registeredIds[selectedFile] = drawingId;
                }
            }

            await DrawingStore.UploadRevisionAsync(
                ProjectId, drawingId, revisionLabel.Trim().ToUpperInvariant(), issuedBy.Trim(), selectedFile, CancellationToken.None);

            // Wait for the register reload to LAND before closing, so the new drawing is on
            // screen the moment the form goes — not after a page refresh.
            await DrawingStore.RefreshNowAsync(ProjectId, isRevision ? drawingId : null, CancellationToken.None);

            registeredIds.Remove(selectedFile);
            ResetForm();
            await OnUploaded.InvokeAsync();
        }
        catch (Exception ex)
        {
            error = $"Upload failed: {ex.Message}";
        }
        finally
        {
            busy = false;
        }
    }

    // One drawing per file, uploaded one after another so a slow connection isn't saturated.
    // A failure doesn't stop the rest: the file stays selected, its message is listed, and
    // pressing Upload again retries only what's left.
    private async Task HandleBulkUpload()
    {
        if (folderPicker?.Problem is { } folderProblem)
        {
            error = folderProblem;
            return;
        }

        busy = true;
        error = null;
        failures.Clear();
        bulkTotal = selectedFiles.Count;
        bulkDone = 0;
        try
        {
            // Resolved once for the whole batch; "New folder…" creates (or finds, by name) the
            // folder first, so a retry after failures files into the same folder.
            var folderId = folderPicker is null ? null : await folderPicker.ResolveFolderAsync(CancellationToken.None);

            var stillFailing = new List<IBrowserFile>();
            foreach (var file in selectedFiles.ToList())
            {
                bulkDone++;
                StateHasChanged();
                try
                {
                    if (!registeredIds.TryGetValue(file, out var drawingId))
                    {
                        drawingId = (await DrawingStore.RegisterDrawingAsync(ProjectId, "", "", folderId, CancellationToken.None)).DrawingId;
                        registeredIds[file] = drawingId;
                    }
                    await DrawingStore.UploadRevisionAsync(
                        ProjectId, drawingId, ExtractRevisionLabel(file.Name), "", file, CancellationToken.None);
                    registeredIds.Remove(file);
                }
                catch (Exception ex)
                {
                    failures.Add((file.Name, ex.Message));
                    stillFailing.Add(file);
                }
            }

            // Whatever failed, the successes are on the register — wait for the reload to land
            // so they are visible immediately, with no page refresh.
            await DrawingStore.RefreshNowAsync(ProjectId, null, CancellationToken.None);

            selectedFiles = stillFailing;
            if (stillFailing.Count == 0)
            {
                ResetForm();
                await OnUploaded.InvokeAsync();
            }
        }
        catch (Exception ex)
        {
            // Folder resolution is the only await outside the per-file try — nothing uploaded yet.
            error = $"Upload failed: {ex.Message}";
        }
        finally
        {
            busy = false;
        }
    }

    private void ResetForm()
    {
        selectedFiles = new List<IBrowserFile>();
        failures.Clear();
        registeredIds.Clear();
        drawingCode = ""; title = ""; revisionLabel = ""; issuedBy = ""; selectedDrawingId = "";
        folderPicker?.Reset();
    }

    private Drawing? DuplicateOfCode(string code) =>
        code.Length == 0
            ? null
            : ExistingDrawings.FirstOrDefault(drawing => string.Equals(drawing.DrawingCode, code, StringComparison.OrdinalIgnoreCase));

    private static string ExtractRevisionLabel(string fileName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            fileName, @"[_-]Rev[_-]?([A-Za-z0-9]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
    }

}
