using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // ---- Detail edit: the Detail panel's text ---------------------------------------------------

    private void OpenDetailEdit()
    {
        if (record is null) return;
        editDescription = record.Description ?? "";
        editError = null;
        editingDetail = true;
    }

    private void CancelDetailEdit() => editingDetail = false;

    private async Task SaveDetailEdit()
    {
        if (record is null || busy || !CanEditDetails) return;
        editError = null;

        var command = new UpdateRequestDetails(
            record.RequestId,
            record.Reference,
            record.Title,
            editDescription?.Trim() ?? "",
            record.Status,
            record.Value,
            record.ResponseText,
            record.RespondedByEmail,
            record.ImpliesVariation,
            record.DrawingRef,
            record.ResponseDue,
            record.RelatedDrawingSpec,
            record.InternalNotes,
            record.ClientNotes);

        if (await SendEdit(command)) editingDetail = false;
    }

    /// <summary>Shared send for the three edit modals: true on success (the caller closes its
    /// modal), false leaves the modal open with the error shown inside it.</summary>
    private async Task<bool> SendEdit(UpdateRequestDetails command)
    {
        try
        {
            busy = true;
            record = await RequestRegister.UpdateAsync(command);
            responseDraft = record.ResponseText ?? "";
            return true;
        }
        catch (CommandFailedException ex)
        {
            editError = ex.Message;
            return false;
        }
        catch
        {
            editError = "Couldn't save the changes. Please try again.";
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    private void CancelDelete() => confirmingDelete = false;

    private async Task PerformDelete()
    {
        if (record is null || busy || !IsAdmin) return;
        actionError = null;
        try
        {
            busy = true;
            await RequestRegister.DeleteAsync(record.RequestId, ProjectId);
            Nav.NavigateTo(RegisterHref);
        }
        catch
        {
            actionError = "Couldn't delete the request. Please try again.";
            confirmingDelete = false;
            busy = false;
        }
    }

    private void CancelReturn() => confirmingReturn = false;

    private async Task PerformReturn()
    {
        if (record is null || busy || !CanTriage) return;
        actionError = null;
        try
        {
            busy = true;
            await RequestRegister.ReturnToTriageAsync(record.RequestId, ProjectId);
            // The request survives a return to triage (only its emails re-enter the queue),
            // so stay on the page rather than navigating back to the register.
            confirmingReturn = false;
            busy = false;
        }
        catch
        {
            actionError = "Couldn't return the request to the Control Centre. Please try again.";
            confirmingReturn = false;
            busy = false;
        }
    }

}
