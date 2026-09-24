using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;


namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // An ellipsis marks a pick that opens something before it moves: the approve panel when no
    // build-up is staged, and the confirms behind Rejected and the return to quoting.
    private string PillOptionLabel(VariationOrderStatus status)
    {
        var opensSomethingFirst = (status == VariationOrderStatus.Approved && !HasStagedBuildUp)
            || status == VariationOrderStatus.Rejected
            || (order?.Status == VariationOrderStatus.Approved && status == VariationOrderStatus.Quoting);
        return opensSomethingFirst ? $"{status.DisplayName()}…" : status.DisplayName();
    }

    private bool HasStagedBuildUp => order is not null && VariationApproval.FromStagedBuildUp(order) is not null;

    private string? PillOptionTitle(VariationOrderStatus status)
    {
        if (status == VariationOrderStatus.Approved && HasStagedBuildUp)
            return "Approves with its line items — mints the V-ref and writes the contract figures";
        if (status == VariationOrderStatus.Approved)
            return "Approving writes the contract figures — opens the approve panel to enter the lines";
        if (order?.Status == VariationOrderStatus.Approved && status == VariationOrderStatus.Quoting)
            return "Un-approves the variation order — reverses the approval's valuation / CVR / budget writes";
        if (status == VariationOrderStatus.Rejected && order?.Status == VariationOrderStatus.Approved)
            return "Rejecting an approved order reverses the valuation / CVR / budget writes";
        if (order?.Status == VariationOrderStatus.Approved && status == VariationOrderStatus.Issued)
            return "Not allowed directly from Approved — return to quoting first";
        if (status == VariationOrderStatus.AwaitingArchitectInstruction)
            return "Issued and waiting on a formal Architect's Instruction — no commercial effect yet";
        return null;
    }

    // Routes the pill's chosen target per the unified transition rules: Approved runs the approval
    // with the staged build-up, or opens the approve panel when there is none to run with; Rejected
    // reverses the approval's writes when leaving Approved (routed through the inline confirm) and
    // is a plain move otherwise; leaving Approved for Quoting is the "return to quoting" repair
    // (also routed through an inline confirm); Approved -> Issued is never allowed; everything else
    // between Quoting, Issued and Awaiting AI moves directly.
    private async Task ChangeStatus(VariationOrderStatus target)
    {
        if (busy || order is null || target == order.Status) return;
        error = null;

        if (order.Status == VariationOrderStatus.Approved && target == VariationOrderStatus.Issued)
            return; // not a valid transition

        if (target == VariationOrderStatus.Approved)
        {
            await ApproveFromStatusMove(order);
            return;
        }

        if (target == VariationOrderStatus.Rejected)
        {
            if (order.Status == VariationOrderStatus.Approved)
            {
                rejectingOrder = true;
                await FocusVariationOrderPanel();
            }
            else
            {
                // Quoting / Issued / Awaiting AI: a plain move, but a terminal one — confirm first.
                decliningOrder = true;
            }
            return;
        }

        if (order.Status == VariationOrderStatus.Approved && target == VariationOrderStatus.Quoting)
        {
            returningToQuoting = true;
            await FocusVariationOrderPanel();
            return;
        }

        // Direct moves between the side-effect-free stages (Quoting, Issued, Awaiting AI).
        try
        {
            busy = true;
            order = await Variations.SetStatusAsync(VariationOrderId, target);
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't change the status. Please try again."; }
        finally { busy = false; }
    }

    private async Task RejectOrder()
    {
        if (busy || order is null) return;
        error = null;
        try
        {
            busy = true;
            order = await Variations.RejectAsync(VariationOrderId);
            rejectingOrder = false;
            decliningOrder = false;
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't reject the variation order. Please try again."; }
        finally { busy = false; }
    }

    private async Task DeleteOrder()
    {
        if (busy || order is null) return;
        error = null;
        try
        {
            busy = true;
            await Variations.DeleteAsync(VariationOrderId);
            // The record is gone — return to the variations register rather than a dead page.
            Nav.NavigateTo($"/projects/{ProjectId}/variations");
        }
        catch (CommandFailedException ex) { error = ex.Message; busy = false; }
        catch { error = "Couldn't delete the variation order. Please try again."; busy = false; }
    }

    // A fresh attempt starts clean: last time's refusal must not read as this time's.
    private void OpenEditLines()
    {
        editLinesError = null;
        editLinesModalOpen = true;
    }

    private void CloseEditLines()
    {
        editLinesModalOpen = false;
        editLinesError = null;
    }

    private async Task SaveLinesEdit(VariationApproval request)
    {
        if (busy || order is null) return;
        editLinesError = null;
        try
        {
            busy = true;
            order = await Variations.ReviseVariationOrderLinesAsync(VariationOrderId, request.Lines);
            editLinesModalOpen = false;
            Valuation.Refresh(ProjectId); // pull the re-priced lines for the breakdown
            await ReloadAsync();
        }
        // The endpoint answers a refused revision with 400 and its own words (no toast, by
        // convention) — so they have to land in the dialog the user is still standing in.
        catch (CommandFailedException ex) { editLinesError = ex.Message; }
        catch { editLinesError = "Couldn't save the variation lines. Please try again."; }
        finally { busy = false; }
    }

    // Moving to Approved is the same approval the bar's button makes: with a staged build-up it is
    // what the panel would submit pre-seeded, so it runs at once — no confirm, no second press;
    // with none there is nothing to approve with, and the panel is where the lines are entered.
    private Task ApproveFromStatusMove(VariationOrder current)
    {
        var staged = VariationApproval.FromStagedBuildUp(current);
        return staged is null ? FocusApprovePanel() : ApproveWithLines(staged);
    }

    // Opens the approve modal — the build-up (cost centres, lines) needs the room a modal gives,
    // not the narrow sidebar.
    private Task FocusApprovePanel()
    {
        approveModalOpen = true;
        return Task.CompletedTask;
    }

    private async Task ReturnToQuoting()
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            order = await Variations.ReturnToQuotingAsync(VariationOrderId);
            returningToQuoting = false;
            await ReloadAsync(); // the approved section drops away; the approve panel reappears
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't return the variation order to quoting. Please try again."; }
        finally { busy = false; }
    }

    // The menu's "Revise value…": the editor is inline in the approved-figures panel, so scroll
    // there and flash it — the same landing as the Reject / Return-to-quoting confirms.
    private async Task OpenReviseValue()
    {
        if (busy || ApprovedOrder is null) return;
        revisingValue = true;
        await FocusVariationOrderPanel();
    }

    // Answers whether the revision took — the panel keeps its editor open on a refusal.
    private async Task<bool> ReviseVoValue(decimal value)
    {
        if (busy || order is null) return false;
        error = null;
        try
        {
            busy = true;
            order = await Variations.ReviseVariationOrderValueAsync(VariationOrderId, value);
            await ReloadAsync();
            return true;
        }
        catch { error = "Couldn't revise the variation order value. Please try again."; return false; }
        finally { busy = false; }
    }

    private string SelectedSubName()
    {
        if (order?.SelectedSubcontractorId is null) return "—";
        return Subcontractors.Find(order.SelectedSubcontractorId)?.CompanyName ?? order.SelectedSubcontractorId;
    }
}
