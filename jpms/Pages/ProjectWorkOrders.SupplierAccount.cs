using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectWorkOrders
{
    // ── The supplier's account on this project (2026-09-14, the accountant's ask) ──
    // Orders, invoices received (linked or not), paid, left to invoice and any over-invoice, in
    // one sheet with a PDF to send on. Opened from the Account link on a supplier row in the
    // supplier view, or from any order's Actions menu in either view — the order carries its
    // supplier, so the cost-centre view reaches it too. The order held here is the one whose
    // supplier the modal shows; null means closed.
    private ProjectWorkOrderDetail? supplierAccountFor;

    private void OpenSupplierAccount(ProjectWorkOrderDetail detail) => supplierAccountFor = detail;

    private void CloseSupplierAccount() => supplierAccountFor = null;

    private DropdownMenu.Item SupplierAccountMenuItem(ProjectWorkOrderDetail detail) =>
        new("Supplier account…",
            OnSelect: EventCallback.Factory.Create(this, () => OpenSupplierAccount(detail)),
            Hint: $"{detail.SubcontractorName}'s account on this project — orders, invoices received, paid, left to invoice and any over-invoice, with a PDF to send on",
            Group: 1);
}
