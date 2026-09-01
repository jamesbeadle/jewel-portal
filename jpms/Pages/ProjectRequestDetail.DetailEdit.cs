using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    private void OpenDetailEdit() { if (record is not null) editingDetail = true; }

    // ---- The three edit dialogs (header, facts, detail) build their own commands; this is the
    // one send they share. The page swaps in the saved record; a refusal goes back to the dialog
    // the user is still standing in.
    private async Task<(bool Saved, string? Error)> SendEdit(UpdateRequestDetails command)
    {
        if (record is null || busy || !CanEditDetails) return (false, null);
        try
        {
            busy = true;
            record = await RequestRegister.UpdateAsync(command);
            responseDraft = record.ResponseText ?? "";
            return (true, null);
        }
        catch (CommandFailedException ex)
        {
            return (false, ex.Message);
        }
        catch
        {
            return (false, "Couldn't save the changes. Please try again.");
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
