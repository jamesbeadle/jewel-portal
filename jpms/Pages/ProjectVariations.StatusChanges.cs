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

    private string? variationStatusMenuId;
    private string? variationStatusBusyId;
    private string? variationStatusError;

    private void ToggleVariationStatusMenu(string variationOrderId) =>
        variationStatusMenuId = variationStatusMenuId == variationOrderId ? null : variationOrderId;

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
        // (through the variation, where the cost code and value are collected) or reject.
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
        choices.Add(new("Approved…",
            "Approving mints the V-ref and writes the contract figures — runs on the variation itself",
            false, Href: variationHref));
        choices.Add(new("Rejected…",
            "Declined by the client or withdrawn — terminal, and confirmed before it is applied",
            false,
            Action: () => { decliningVariation = order; return Task.CompletedTask; }));
        return choices;
    }

    private async Task PickVariationStatus(VariationStatusChoice choice)
    {
        variationStatusMenuId = null;
        if (choice.IsCurrent) return;
        if (choice.Href is not null) { Nav.NavigateTo(choice.Href); return; }
        if (choice.Action is not null) await choice.Action();
    }

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

    private async Task ChangeVariationStatusInline(VariationOrder order, VariationOrderStatus status)
    {
        if (variationStatusBusyId is not null) return;
        variationStatusError = null;
        try
        {
            variationStatusBusyId = order.VariationOrderId;
            if (status == VariationOrderStatus.Rejected)
                await Variations.RejectAsync(order.VariationOrderId);
            else if (status == VariationOrderStatus.Quoting && order.Status == VariationOrderStatus.Approved)
                await Variations.ReturnToQuotingAsync(order.VariationOrderId);
            else
                await Variations.SetStatusAsync(order.VariationOrderId, status);
            await LoadVariationsAsync();
        }
        catch (CommandFailedException ex) { variationStatusError = $"{RowReference(order)}: {ex.Message}"; }
        catch { variationStatusError = $"Couldn't change the status of {RowReference(order)}. Please try again."; }
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

    private static string BadgeClass(VariationOrder order)
    {
        const string baseClass = "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium ";
        return order.Status switch
        {
            VariationOrderStatus.Approved => baseClass + "bg-accent/10 border-accent/30 text-accent",
            VariationOrderStatus.Rejected => baseClass + "bg-negative/10 border-negative/30 text-negative",
            _ => baseClass + "bg-surface-raised border-line text-content-muted"
        };
    }


}
