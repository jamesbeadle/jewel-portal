using System.Text.Json;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Link project drawings to the package ----

    private bool showDrawingsModal;
    private IReadOnlyList<Drawing> projectDrawings = Array.Empty<Drawing>();
    private readonly HashSet<string> selectedDrawingIds = new(StringComparer.OrdinalIgnoreCase);

    private async Task OpenDrawingsModal()
    {
        selectedDrawingIds.Clear();
        foreach (var drawing in packageDrawings) selectedDrawingIds.Add(drawing.DrawingId);
        showDrawingsModal = true;
        try { projectDrawings = await Queries.AskAsync(new ListDrawingsForProject(ProjectId), CancellationToken.None); }
        catch { projectDrawings = Array.Empty<Drawing>(); }
    }

    private void CloseDrawingsModal() => showDrawingsModal = false;

    private void ToggleDrawing(string drawingId, ChangeEventArgs e)
    {
        if (e.Value is true) selectedDrawingIds.Add(drawingId);
        else selectedDrawingIds.Remove(drawingId);
    }

    private async Task ConfirmDrawings()
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedPackageDrawings = await Commands.SendAsync(
                new SetBidPackageDrawings(BidPackageId, selectedDrawingIds.ToList()), CancellationToken.None);
            showDrawingsModal = false;
        }
        catch { error = "Couldn't update the linked drawings. Please try again."; }
        finally { busy = false; }
    }

    // ---- Tender-document attachments: uploaded files that travel with the invite. ----

    private async Task OnAttachmentFilesSelected(InputFileChangeEventArgs e)
    {
        if (busy || !CanEdit) return;
        var files = e.GetMultipleFiles(20);
        if (files.Count == 0) return;
        error = null;
        try
        {
            busy = true;
            fetchedAttachments = await PackageAttachments.UploadFilesAsync(BidPackageId, files);
        }
        catch (Exception ex) { error = $"Couldn't upload: {ex.Message}"; }
        finally { busy = false; }
    }

    private async Task RemoveAttachment(BidPackageAttachment attachment)
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedAttachments = await PackageAttachments.RemoveAsync(BidPackageId, attachment.BidPackageAttachmentId);
        }
        catch { error = "Couldn't remove the attachment. Please try again."; }
        finally { busy = false; }
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024d * 1024d):0.#} MB",
        >= 1024 => $"{bytes / 1024d:0.#} KB",
        _ => $"{bytes} B"
    };

}
