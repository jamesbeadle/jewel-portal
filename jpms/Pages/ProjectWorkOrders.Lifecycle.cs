using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    private string? pendingCancelId;
    private bool cancelBusy;
    private string? cancelError;

    private bool CancelPendingFor(ProjectWorkOrderDetail detail) =>
        pendingCancelId == detail.Order.WorkOrderId;

    private void SetCancelPending(ProjectWorkOrderDetail detail)
    {
        pendingCancelId = detail.Order.WorkOrderId;
        cancelError = null;
    }

    private void ClearCancelPending() => pendingCancelId = null;

    private async Task CancelAsync(ProjectWorkOrderDetail detail)
    {
        if (cancelBusy) return;
        cancelBusy = true;
        cancelError = null;
        try
        {
            await Commands.SendAsync(new CancelWorkOrder(ProjectId, detail.Order.WorkOrderId), CancellationToken.None);
            pendingCancelId = null;
            // Post-write reload: never let a failed re-query take the page down after a write
            // that succeeded (JPMS-668D10) — the query client has already toasted it, and the
            // next stale-while-revalidate refresh fixes the stale list.
            try { await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None); }
            catch (OperationCanceledException) { throw; }
            catch { }
        }
        catch (CommandFailedException ex)
        {
            // 400s are answers for the caller to show, not toasts — bills still linked,
            // money already paid, or the role isn't allowed to.
            cancelError = $"Couldn't cancel {Reference(detail.Order)}: {ex.Message}";
        }
        finally
        {
            cancelBusy = false;
        }
    }

    // What happened to the automatic purchase-order email after a release (modal creation or
    // draft approval) — shown at the top of the page until dismissed.
    private string? poEmailNote;

    private void ShowPoEmailNote(string note) => poEmailNote = note;
}
