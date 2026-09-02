using Jewel.JPMS.Commercial;
using static Jewel.JPMS.Features.Commercial.ValuationReportDisplay;

namespace Jewel.JPMS.Components;

public partial class ValuationReportTable
{
    // ---- Variation value revise (Admin) ------------------------------------
    // A variation line's value mirrors its approved VO, so the inline edit goes through
    // the VO revise pipeline (ReviseVariationOrderValue): it re-prices this line, records
    // the delta as a QS accrual on the CVR and moves the committed cost-centre budget in
    // one transaction — the same write-through as "Revise value" on the variation page, so every
    // view of the VO stays in step. UI is Admin-only for now (the API accepts the wider
    // variation-manager set: Admin, Director, PM, Estimator).
    private IReadOnlyList<VariationOrder> variationOrders = Array.Empty<VariationOrder>();
    private bool CanReviseVariationValue => Session.ActiveRole is Role.Admin;

    private string? revisingVoLineId;
    private string reviseValue = "";
    private bool reviseError;         // parse failure on the typed value
    private string? reviseFailure;    // API failure on save
    private bool reviseSaving;

    // Only an approved variation carries live figures, so only its line is revisable; a line whose
    // ref matches no approved VO (e.g. a declined placeholder) stays locked.
    private VariationOrder? RevisableVoFor(ValuationLineItem line) =>
        string.IsNullOrWhiteSpace(line.VariationRef)
            ? null
            : variationOrders.FirstOrDefault(vo =>
                vo.Status == VariationOrderStatus.Approved
                && string.Equals(vo.VariationRef, line.VariationRef, StringComparison.OrdinalIgnoreCase));

    private void StartVoRevise(ValuationLineItem line, VariationOrder vo)
    {
        CancelEdit(); // one cell editor at a time — close any open % editor first
        revisingVoLineId = line.ValuationLineItemId;
        reviseValue = vo.Value.ToString("0.##", Gb);
        reviseError = false;
        reviseFailure = null;
    }

    private void CancelVoRevise()
    {
        revisingVoLineId = null;
        reviseError = false;
        reviseFailure = null;
    }

    private async Task OnVoReviseKeyDownAsync(KeyboardEventArgs e, ValuationLineItem line)
    {
        if (e.Key == "Enter") await CommitVoReviseAsync(line);
        else if (e.Key == "Escape") CancelVoRevise();
    }

    private async Task CommitVoReviseAsync(ValuationLineItem line)
    {
        if (reviseSaving) return;
        var vo = RevisableVoFor(line);
        if (vo is null) { CancelVoRevise(); return; }
        // Negative values are legitimate — omission variations (e.g. supply credits) carry them.
        if (!decimal.TryParse(reviseValue.Trim().TrimStart('£'), System.Globalization.NumberStyles.Number, Gb, out var value))
        {
            reviseError = true;
            return;
        }
        reviseSaving = true;
        reviseFailure = null;
        try
        {
            var revised = await Variations.ReviseVariationOrderValueAsync(vo.VariationOrderId, value);
            variationOrders = variationOrders
                .Select(existing => existing.VariationOrderId == revised.VariationOrderId ? revised : existing)
                .ToList();
            revisingVoLineId = null;
            // The API re-priced the line server-side; refetch so the row (and totals) catch up.
            Store.Refresh(ProjectId);
        }
        catch
        {
            reviseFailure = "Couldn't revise the variation value. Please try again.";
        }
        finally
        {
            reviseSaving = false;
        }
    }

    // ---- Variation cost-centre recode --------------------------------------
    // Mirrors the API gate (ValuationReportAuthorisation.RolesThatMayRecodeCostCentres):
    // admins, the MD, the FD and project managers may move a variation's value between
    // cost centres; everyone else sees the allocation read-only.
    private bool CanRecodeCostCentres => Session.ActiveRole
        is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager;

    private string? recodingLineId;
    private string? costCentreError;
    private string? costCentreErrorLineId;

    private async Task SetCostCentreAsync(ValuationLineItem line, string code)
    {
        // SearchSelect's leading blank entry clears — a variation's value always sits
        // somewhere, so ignore it rather than send an update the API would reject.
        if (string.IsNullOrWhiteSpace(code) || code == line.CostCode) return;
        recodingLineId = line.ValuationLineItemId;
        costCentreError = null;
        costCentreErrorLineId = null;
        try
        {
            await Store.SetLineCostCentreAsync(new SetValuationLineCostCentre(line.ValuationLineItemId, code));
            OnParametersSet();
        }
        catch (CommandFailedException failure)
        {
            costCentreError = failure.Message;
            costCentreErrorLineId = line.ValuationLineItemId;
        }
        catch
        {
            // Transport/API failure (e.g. the API mid-deploy answering 503) must not kill
            // the whole app — surface it on the row and let the user simply retry.
            costCentreError = "The server couldn't be reached — please try again in a moment.";
            costCentreErrorLineId = line.ValuationLineItemId;
        }
        finally
        {
            recodingLineId = null;
        }
    }

    // The label carries code + name so typing matches either (same as XeroAllocation).
    private IReadOnlyList<SearchSelect.Option>? costCentreOptionsCache;
    private object? costCentreOptionsCacheKey;

    private IReadOnlyList<SearchSelect.Option> CostCentreOptions
    {
        get
        {
            var centres = CostCenters.ActiveAlphabetical();
            if (costCentreOptionsCache is null || !ReferenceEquals(costCentreOptionsCacheKey, centres))
            {
                costCentreOptionsCache = centres
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}")).ToList();
                costCentreOptionsCacheKey = centres;
            }
            return costCentreOptionsCache;
        }
    }

    // A line coded to a retired centre still shows (and can be moved off) it.
    private IReadOnlyList<SearchSelect.Option> CostCentreOptionsFor(ValuationLineItem line)
    {
        var options = CostCentreOptions;
        if (string.IsNullOrWhiteSpace(line.CostCode) || options.Any(option => option.Value == line.CostCode))
            return options;
        var withCurrent = new List<SearchSelect.Option>(options.Count + 1)
        {
            new(line.CostCode, $"{line.CostCode} (retired)")
        };
        withCurrent.AddRange(options);
        return withCurrent;
    }

    private string CostCentreLabel(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "—";
        var centre = CostCenters.All().FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        return centre is null ? code : $"{code} — {centre.Name}";
    }
}
