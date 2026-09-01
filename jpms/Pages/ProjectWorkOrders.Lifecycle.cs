using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
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

    // An assistant open_modal request this page refused (unknown order, role gate, terminal
    // status) — shown at the top of the page until dismissed.

    private void ShowPoEmailNote(string note) => poEmailNote = note;

    /// <summary>The supplier's directory email for an order, or null when the directory has
    /// none — which is exactly the case the approve warning must call out.</summary>
    private string? SupplierEmailFor(ProjectWorkOrderDetail detail)
    {
        var supplier = (Subcontractors.Current ?? Array.Empty<Subcontractor>()).FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, detail.Order.SubcontractorId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(supplier?.ContactEmail) ? null : supplier!.ContactEmail;
    }

    /// <summary>Sends the purchase-order email a release promised (draft approval path).
    /// Never throws — the order is released either way; the returned note says what happened.</summary>
    private async Task<string> TrySendPoEmailAsync(WorkOrder order, ProjectWorkOrderDetail detail)
    {
        if (SupplierEmailFor(detail) is null)
            return $"{order.Reference} was approved, but the supplier has no email address in the directory "
                + "so the purchase order wasn't emailed — add one, then send it from the PO page.";
        var projectName = Projects.Find(ProjectId)?.Name ?? "";
        var emailLines = detail.Lines.OrderBy(l => l.SortOrder).Select(WorkOrderPoEmail.ToLine).ToList();
        try
        {
            var outcome = await Commands.SendAsync(new SendWorkOrderPoEmail(
                order.WorkOrderId,
                WorkOrderPoEmail.Subject(order, string.IsNullOrWhiteSpace(projectName) ? ProjectId : projectName),
                WorkOrderPoEmail.Body(order, detail.SubcontractorName, emailLines, projectName, Nav.BaseUri)),
                CancellationToken.None);
            return outcome.Sent
                ? $"{order.Reference} was approved and the purchase order was emailed to {outcome.RecipientEmail}."
                : $"{order.Reference} was approved. {outcome.FailureNote}";
        }
        catch (CommandFailedException ex)
        {
            return $"{order.Reference} was approved, but the purchase-order email couldn't be sent: "
                + $"{ex.Message} You can send it from the PO page.";
        }
        catch
        {
            return $"{order.Reference} was approved, but the purchase-order email couldn't be sent "
                + "— you can send it from the PO page.";
        }
    }

    private void SetPending(ProjectWorkOrderDetail detail, DraftDecision decision)
    {
        pendingDraftId = detail.Order.WorkOrderId;
        pendingDecision = decision;
        decisionError = null;
    }

    private void ClearPending() => pendingDraftId = null;

    // The draft row's Actions menu. Approve and Reject only ARM the row's inline confirm step
    // (SetPending) — the decision itself still takes the second, spelled-out click. Delete opens
    // the confirm modal instead, because it alone leaves no record behind.
    private IReadOnlyList<DropdownMenu.Item> DraftMenuItems(ProjectWorkOrderDetail detail)
    {
        var items = new List<DropdownMenu.Item>
        {
            new(Label: "Preview PO",
                Href: PurchaseOrderPathFor(detail),
                Hint: "Open the purchase order exactly as it will read once approved — the sheet carries a Draft mark until the number is minted; print or save it as a PDF from there"),
            new(Label: "Edit…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenEdit(detail)),
                Hint: "Edit the draft's supplier, title, scope and priced lines"),
            new(Label: "Approve…",
                OnSelect: EventCallback.Factory.Create(this, () => SetPending(detail, DraftDecision.Approve)),
                Hint: "Approve this draft — it takes the next order number and becomes a live order",
                Group: 1),
            new(Label: "Reject…",
                OnSelect: EventCallback.Factory.Create(this, () => SetPending(detail, DraftDecision.Reject)),
                Hint: "Reject this draft — terminal; its value stops counting everywhere, and it stays listed under Rejected",
                Destructive: true, Group: 1),
            new(Label: "Delete…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenDelete(detail)),
                Hint: "Delete this draft outright — for drafts raised in error; nothing stays on this page",
                Destructive: true, Group: 2)
        };
        return items;
    }

    // A rejected order's menu is short: the record is already decided, so all that is left is
    // reading the PO it would have been, and removing records that shouldn't even be a quiet note.
    private IReadOnlyList<DropdownMenu.Item> RejectedMenuItems(ProjectWorkOrderDetail detail)
    {
        var items = new List<DropdownMenu.Item>
        {
            new(Label: "Preview PO",
                Href: PurchaseOrderPathFor(detail),
                Hint: "Open the purchase order as it would have read — it keeps its Draft mark; no number was ever minted"),
            new(Label: "Delete…",
                OnSelect: EventCallback.Factory.Create(this, () => OpenDelete(detail)),
                Hint: "Delete this rejected order outright — it counts nowhere already; this removes the record itself",
                Destructive: true, Group: 1)
        };
        return items;
    }

    private void OpenDelete(ProjectWorkOrderDetail detail)
    {
        deletingOrder = detail;
        deleteError = null;
    }

    private void CloseDelete()
    {
        if (deleteBusy) return;
        deletingOrder = null;
        deleteError = null;
    }

    private async Task DeleteOrderAsync()
    {
        if (deleteBusy || deletingOrder is not { } orderToDelete) return;
        deleteBusy = true;
        deleteError = null;
        try
        {
            await Commands.SendAsync(new DeleteDraftWorkOrder(
                ProjectId, orderToDelete.Order.WorkOrderId), CancellationToken.None);
            deletingOrder = null;
            await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
        }
        catch (CommandFailedException ex)
        {
            // 400s are answers for the caller to show, not toasts — say what refused and why.
            deleteError = $"Couldn't delete \"{orderToDelete.Order.Title}\": {ex.Message}";
        }
        finally
        {
            deleteBusy = false;
        }
    }

    private async Task DecideAsync(ProjectWorkOrderDetail detail, DraftDecision decision)
    {
        if (decisionBusy) return;
        decisionBusy = true;
        decisionError = null;
        try
        {
            if (decision == DraftDecision.Approve)
            {
                var approved = await Commands.SendAsync(new ApproveWorkOrder(
                    ProjectId, detail.Order.WorkOrderId, Auth.CurrentUser?.Email ?? ""), CancellationToken.None);
                pendingDraftId = null;
                await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
                // The email the approve warning promised — non-fatal by design: the order is
                // released either way, and the note says what happened to its email.
                poEmailNote = await TrySendPoEmailAsync(approved, detail);
            }
            else
            {
                await Commands.SendAsync(new RejectWorkOrder(
                    ProjectId, detail.Order.WorkOrderId), CancellationToken.None);
                pendingDraftId = null;
                await WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
            }
        }
        catch (CommandFailedException ex)
        {
            // 400s are answers for the caller to show, not toasts — say what refused and why.
            decisionError = decision == DraftDecision.Approve
                ? $"Couldn't approve \"{detail.Order.Title}\": {ex.Message}"
                : $"Couldn't reject \"{detail.Order.Title}\": {ex.Message}";
        }
        finally
        {
            decisionBusy = false;
        }
    }
}
