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
    private async Task OpenDrawingsModal()
    {
        showDrawingsModal = true;
        try { projectDrawings = await Queries.AskAsync(new ListDrawingsForProject(ProjectId), CancellationToken.None); }
        catch { projectDrawings = Array.Empty<Drawing>(); }
    }

    private void CloseDrawingsModal() => showDrawingsModal = false;

    private async Task ConfirmDrawings(IReadOnlyList<string> drawingIds)
    {
        if (busy || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            fetchedPackageDrawings = await Commands.SendAsync(
                new SetBidPackageDrawings(BidPackageId, drawingIds.ToList()), CancellationToken.None);
            showDrawingsModal = false;
        }
        catch { error = "Couldn't update the linked documents. Please try again."; }
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
}
