using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using static Jewel.JPMS.Features.Procurement.WorkOrderDisplay;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    // ── Cancelling an issued order: terminal, directors and the FD only ──
    // Same two-click shape as the draft decisions: cancelling voids an order the supplier
    // has already been sent, and there is no undo. The API is the real gate (CancelWorkOrder
    // is Admin/Director/FD only); CanCancelOrders just keeps the action out of everyone
    // else's way. Admin expands to every role at sign-in, so checking the two roles is enough.
    private bool CanCancelOrders =>
        Auth.CurrentRoles.Contains(Role.ManagingDirector) || Auth.CurrentRoles.Contains(Role.FinanceDirector);

    // ── Editing an order: manual orders as before; ANY order for the directors ──
    // The accountant's flow (2026-08-21): open WO-0045, add the £80 line from the email, save,
    // download the updated PO and send it back by hand. The API is the real gate (the endpoint
    // stamps the director flag onto UpdateManualWorkOrder); this just keeps Edit out of everyone
    // else's menus. Admin expands to every role at sign-in, so checking the two roles is enough.
    private bool CanEditAllOrders =>
        Auth.CurrentRoles.Contains(Role.ManagingDirector) || Auth.CurrentRoles.Contains(Role.FinanceDirector);

    private bool CanEditOrder(WorkOrder order) => order.IsManual || CanEditAllOrders;

    private IReadOnlyList<DropdownMenu.Item> LineMenuItems(WorkOrderLineEntry line)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("View PO",
                Href: PurchaseOrderPath(ProjectId, line.Detail.Order.WorkOrderId),
                Hint: "View / print the purchase order sent to the supplier")
        };
        if (CanEditOrder(line.Detail.Order))
        {
            items.Add(new DropdownMenu.Item("Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(line.Detail)),
                Hint: line.Detail.Order.IsManual
                    ? "This order was raised manually — edit its supplier, title, scope and priced lines"
                    : "Correct this order's supplier, title, scope and priced lines — directors only; the updated PO is downloaded and sent by hand"));
        }
        items.Add(new DropdownMenu.Item("Re-code this line…",
            OnSelect: EventCallback.Factory.Create(this, () => OpenRecode(line)),
            Hint: "Move this line to another cost centre, or split its amount across several — the order's value never changes",
            Group: 1));
        if (CanCancelOrders)
        {
            items.Add(new DropdownMenu.Item("Cancel order…",
                OnSelect: EventCallback.Factory.Create(this, () => SetCancelPending(line.Detail)),
                Hint: "Cancel this issued order — void the whole order (not just this line); it keeps its number as a record but stops counting everywhere. Refused while bills are linked or money is paid against it.",
                Destructive: true,
                Group: 2));
        }
        return items;
    }

    private IReadOnlyList<DropdownMenu.Item> OrderMenuItems(ProjectWorkOrderDetail detail)
    {
        var items = new List<DropdownMenu.Item>
        {
            new("View PO",
                Href: PurchaseOrderPath(ProjectId, detail.Order.WorkOrderId),
                Hint: "View / print the purchase order sent to the supplier")
        };
        if (CanEditOrder(detail.Order))
        {
            items.Add(new DropdownMenu.Item("Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(detail)),
                Hint: detail.Order.IsManual
                    ? "This order was raised manually — edit its supplier, title, scope and priced lines"
                    : "Correct this order's supplier, title, scope and priced lines — directors only; the updated PO is downloaded and sent by hand"));
        }
        if (CanCancelOrders)
        {
            items.Add(new DropdownMenu.Item("Cancel order…",
                OnSelect: EventCallback.Factory.Create(this, () => SetCancelPending(detail)),
                Hint: "Cancel this issued order — void it; it keeps its number as a record but stops counting everywhere. Refused while bills are linked or money is paid against it.",
                Destructive: true,
                Group: 1));
        }
        return items;
    }
}
