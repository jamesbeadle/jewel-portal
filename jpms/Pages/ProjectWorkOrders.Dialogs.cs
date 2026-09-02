using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    // The line being re-coded across cost centres, if the modal is open.
    private WorkOrderLineEntry? recoding;

    private void OpenRecode(WorkOrderLineEntry line) => recoding = line;

    private void CloseRecode() => recoding = null;

    private async Task HandleRecodedAsync()
    {
        recoding = null;
        await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // The manual "Add work order" flow, which can also package the fresh order
    // against the valuation lines that priced its scope. The same modal edits a
    // manually raised order when editingOrder is set.
    private bool manualOrderOpen;
    private ProjectWorkOrderDetail? editingOrder;

    private void OpenAdd()
    {
        editingOrder = null;
        manualOrderOpen = true;
    }

    private void OpenEdit(ProjectWorkOrderDetail detail)
    {
        editingOrder = detail;
        manualOrderOpen = true;
    }

    private void CloseManualOrder()
    {
        manualOrderOpen = false;
        editingOrder = null;
    }

    private async Task HandleManualOrderSavedAsync()
    {
        manualOrderOpen = false;
        editingOrder = null;
        await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
    }

    private DeleteWorkOrderModal deleteModal = default!;
    // A delete is in flight — the rejected list's menus stand down on it.
    private bool deleteBusy;
}
