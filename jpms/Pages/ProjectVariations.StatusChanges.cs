using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    // ---- Issue work order for an approved variation order ----

    private IReadOnlyList<WorkOrder> IssuedWorkOrdersFor(VariationOrder order) =>
        Procurement.WorkOrdersFor(ProjectId)
            .Where(wo => string.Equals(wo.VariationOrderId, order.VariationOrderId, StringComparison.OrdinalIgnoreCase))
            .ToList();

    // A work order is only instructed after approval — the client's instruction to proceed.
    private bool CanIssueWorkOrder(VariationOrder order) =>
        CanIssueWorkOrders
        && order.Status == VariationOrderStatus.Approved
        && !string.IsNullOrWhiteSpace(order.SelectedSubcontractorId);

    private async Task IssueWorkOrder(string variationOrderId)
    {
        if (requestBusy) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.IssueWorkOrderForVariationOrderAsync(variationOrderId);
            Procurement.Refresh(ProjectId); // Show the new order in the issued-WO column.
            await LoadVariationsAsync();    // VO may have moved Approved → Issued.
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't issue the work order. Please try again."; }
        finally { requestBusy = false; }
    }

    // ---- In-row variation status changes (the chip's dropdown) ----------------------------------

    // The row whose status menu is open. The menu dismisses itself (DropdownMenu); this is only
    // so the table can lift its scroll clip while a panel is showing.
    private string? variationStatusBusyId;
    private string? variationStatusError;


    private List<DropdownMenu.Item> StatusMenuItems(IReadOnlyList<VariationStatusChoice> choices) =>
        choices.Select(StatusMenuItem).ToList();

    // The current status renders as a disabled, ticked row — it names where the variation is
    // without offering a move to where it already is.
    private DropdownMenu.Item StatusMenuItem(VariationStatusChoice choice) => new(
        Label: choice.Label,
        OnSelect: choice.Action is null ? null : EventCallback.Factory.Create(this, choice.Action),
        Href: choice.Href,
        Hint: choice.Hint,
        Disabled: choice.IsCurrent,
        Selected: choice.IsCurrent);

    // One dropdown entry: a direct move (Action) or a link through to the variation (Href) for the
    // transitions whose real flows — cost code, confirms, reversals — live on the record itself.
    private sealed record VariationStatusChoice(string Label, string? Hint, bool IsCurrent, Func<Task>? Action = null, string? Href = null);

    private List<VariationStatusChoice> VariationStatusChoices(VariationOrder order)
    {
        var choices = new List<VariationStatusChoice>();
        var variationHref = $"/projects/{ProjectId}/variations/{order.VariationOrderId}";

        // Rejected is a terminal audit record — the pill doesn't offer reactivation.
        if (order.Status == VariationOrderStatus.Rejected) return choices;

        if (order.Status == VariationOrderStatus.Approved)
        {
            // An approved order can only move back to Quoting (data repair, un-approve) or to
            // Rejected (a real commercial event) — never straight across to Issued.
            choices.Add(new("Approved", null, true));
            choices.Add(new("Quoting (return to quoting)…",
                "Un-approves — reverses the approval's writes and frees the V-ref; a record correction",
                false,
                Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Quoting)));
            choices.Add(new("Rejected…",
                "A real commercial event — reverses the approval's valuation / CVR / budget writes",
                false,
                Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Rejected)));
            return choices;
        }

        // Quoting / Issued / Awaiting AI: move directly between the side-effect-free stages, approve
        // (in place with the staged build-up, else through the variation's approve panel) or reject.
        choices.Add(new("Quoting", null, order.Status == VariationOrderStatus.Quoting,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Quoting)));
        choices.Add(new("Issued",
            "Marks the variation as sent to the client, awaiting their decision",
            order.Status == VariationOrderStatus.Issued,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.Issued)));
        choices.Add(new("Awaiting AI",
            "Issued and waiting on a formal Architect's Instruction — no commercial effect yet",
            order.Status == VariationOrderStatus.AwaitingArchitectInstruction,
            Action: () => ChangeVariationStatusInline(order, VariationOrderStatus.AwaitingArchitectInstruction)));
        choices.Add(ApprovedChoice(order, variationHref));
        choices.Add(new("Rejected…",
            "Declined by the client or withdrawn — terminal, and confirmed before it is applied",
            false,
            Action: () => { decliningVariation = order; return Task.CompletedTask; }));
        return choices;
    }

    // Approved is the same approval the record page's button makes. With a staged build-up it is
    // exactly what the approve panel would submit pre-seeded, so it runs here, in one press — no
    // confirm, no navigation. With none there is nothing to approve with, so the pick opens the
    // variation on its approve panel to enter the lines.
    private VariationStatusChoice ApprovedChoice(VariationOrder order, string variationHref)
    {
        var staged = VariationApproval.FromStagedBuildUp(order);
        if (staged is null)
            return new("Approved…",
                "Approving mints the V-ref and writes the contract figures — opens the variation's approve panel to enter the lines",
                false, Href: $"{variationHref}?approve=true");
        return new("Approved",
            "Approves with the staged build-up — mints the V-ref and writes the contract figures",
            false,
            Action: () => ApproveVariationInline(order, staged));
    }

    private Task ApproveVariationInline(VariationOrder order, VariationApproval approval) =>
        RunInlineStatusMove(order,
            () => Variations.ApproveAsync(order.VariationOrderId, approval.PrimaryCostCode, approval.Total, approval.Lines),
            $"Couldn't approve {RowReference(order)}. Please try again.");

    // The variation the decline modal is asking about; null when the modal is closed.
    private VariationOrder? decliningVariation;

    private async Task ConfirmDeclineVariation()
    {
        if (decliningVariation is not { } order) return;
        await ChangeVariationStatusInline(order, VariationOrderStatus.Rejected);
        // Close only on success — a failure leaves the modal up with the error visible behind it,
        // rather than silently swallowing the attempt.
        if (variationStatusError is null) decliningVariation = null;
    }

    private Task ChangeVariationStatusInline(VariationOrder order, VariationOrderStatus status) =>
        RunInlineStatusMove(order,
            () => StatusMove(order, status),
            $"Couldn't change the status of {RowReference(order)}. Please try again.");

    private Task StatusMove(VariationOrder order, VariationOrderStatus status)
    {
        if (status == VariationOrderStatus.Rejected) return Variations.RejectAsync(order.VariationOrderId);
        var isUnapproving = status == VariationOrderStatus.Quoting && order.Status == VariationOrderStatus.Approved;
        if (isUnapproving) return Variations.ReturnToQuotingAsync(order.VariationOrderId);
        return Variations.SetStatusAsync(order.VariationOrderId, status);
    }

    // One row moves at a time; the row's own reference leads any refusal so the list says which.
    private async Task RunInlineStatusMove(VariationOrder order, Func<Task> move, string failureMessage)
    {
        if (variationStatusBusyId is not null) return;
        variationStatusError = null;
        try
        {
            variationStatusBusyId = order.VariationOrderId;
            await move();
            await LoadVariationsAsync();
        }
        catch (CommandFailedException ex) { variationStatusError = $"{RowReference(order)}: {ex.Message}"; }
        catch { variationStatusError = failureMessage; }
        finally { variationStatusBusyId = null; }
    }

    private static string VariationStatusLabel(VariationOrder order) => order.Status switch
    {
        VariationOrderStatus.Quoting => "Quoting",
        VariationOrderStatus.Issued => "Issued",
        VariationOrderStatus.AwaitingArchitectInstruction => "Awaiting AI",
        VariationOrderStatus.Approved => "Approved",
        VariationOrderStatus.Rejected => "Rejected",
        _ => "Variation"
    };



}
