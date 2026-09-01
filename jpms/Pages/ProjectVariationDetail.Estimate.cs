using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // ---- Estimate (pre-approval only) ---------------------------------------
    private bool editingEstimate;
    private string estimateText = "";
    private string? estimateError;

    // A staged build-up owns the figure (its total is the estimate), and approval moves the
    // money story to the contract Value — the edit only exists while the estimate is free-standing.
    private bool CanEditEstimate =>
        CanManage && order is not null && order.Status.IsPreApproval()
        && (order.DraftLines is null || order.DraftLines.Count == 0);

    private void StartEditEstimate()
    {
        estimateText = order?.EstimatedValue?.ToString("0.##") ?? "";
        estimateError = null;
        editingEstimate = true;
    }

    private void CancelEditEstimate() => editingEstimate = false;

    private async Task SaveEstimate()
    {
        if (busy || order is null) return;
        estimateError = null;
        decimal? estimate = null;
        var text = estimateText.Trim().TrimStart('£').Replace(",", "");
        if (text.Length > 0)
        {
            if (!decimal.TryParse(text, out var parsed)) { estimateError = "Enter a number, or leave blank for unpriced."; return; }
            if (parsed < 0m) { estimateError = "An estimate cannot be negative."; return; }
            estimate = parsed;
        }
        try
        {
            busy = true;
            order = await Variations.SetEstimateAsync(VariationOrderId, estimate);
            editingEstimate = false;
        }
        catch (CommandFailedException ex) { estimateError = ex.Message; }
        catch { estimateError = "Couldn't save the estimate. Please try again."; }
        finally { busy = false; }
    }

    private async Task SaveTitle()
    {
        if (busy || order is null) return;
        error = null;
        var title = renameTitle.Trim();
        if (title.Length == 0) { error = "A title is required."; return; }
        // An unchanged title is a cancel by another name — no round-trip, no audit noise.
        if (string.Equals(title, order.Title, StringComparison.Ordinal)) { renamingOrder = false; return; }
        try
        {
            busy = true;
            order = await Variations.RenameAsync(VariationOrderId, title);
            renamingOrder = false;
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't save the new title. Please try again."; }
        finally { busy = false; }
    }

    private void StartEditNarratives()
    {
        narrativeCommercialBasis = order?.CommercialBasis ?? "";
        narrativeProgrammeImpact = order?.ProgrammeImpact ?? "";
        narrativeExclusions = order?.Exclusions ?? "";
        // Open on a clean slate — same rule as the retitle's banner.
        narrativesError = null;
        editingNarratives = true;
    }

    private async Task SaveNarratives()
    {
        if (busy || order is null) return;
        narrativesError = null;
        try
        {
            busy = true;
            order = await Variations.UpdateNarrativesAsync(
                VariationOrderId, narrativeCommercialBasis, narrativeProgrammeImpact, narrativeExclusions);
            editingNarratives = false;
        }
        catch (CommandFailedException ex) { narrativesError = ex.Message; }
        catch { narrativesError = "Couldn't save the document sections. Please try again."; }
        finally { busy = false; }
    }

    private void OnSubChanged(ChangeEventArgs e) => selSubId = e.Value?.ToString() ?? "";

    private async Task ApproveWithLines(VariationApprovePanel.ApproveRequest request)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            // The panel supplies the priced build-up; the value and primary cost code are derived
            // from it. The server writes one report line per entry under this variation's V-ref.
            order = await Variations.ApproveAsync(VariationOrderId, request.PrimaryCostCode, request.Total, request.Lines);
            approveModalOpen = false;
            Valuation.Refresh(ProjectId); // pull the freshly written variation lines for the breakdown
            await ReloadAsync();
        }
        catch (System.Exception ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message) ? "Couldn't approve the variation order. Please try again." : ex.Message;
        }
        finally { busy = false; }
    }

    private async Task IssueWorkOrder()
    {
        if (busy || ApprovedOrder is not { } approved) return;
        error = null;
        try
        {
            busy = true;
            await Variations.IssueWorkOrderForVariationOrderAsync(approved.VariationOrderId);
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't issue the work order. Please try again."; }
        finally { busy = false; }
    }

}
