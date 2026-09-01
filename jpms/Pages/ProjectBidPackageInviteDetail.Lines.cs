using System.Text.Json;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Line-item coverage (link to a cost centre or a variation order) ----

    private bool showCoverageModal;
    private BidPackageLineItem? linkingLine;
    private BidPackageLineCoverage coverageChoice = BidPackageLineCoverage.Unassigned;
    private string? coverageCostCode;
    private string? coverageVariationId;

    private string CoverageLabel(BidPackageLineItem item)
    {
        switch (item.Coverage)
        {
            case BidPackageLineCoverage.ContractLine:
                // Legacy BoQ links (retired 2026-08-16) keep displaying what they meant.
                if (!string.IsNullOrWhiteSpace(item.BoqLineItemId))
                {
                    var boq = boqLines.FirstOrDefault(b => b.BoqLineItemId == item.BoqLineItemId);
                    return boq is null ? "Contract line" : $"BoQ · {boq.Description}";
                }
                return string.IsNullOrWhiteSpace(item.CostCode) ? "Cost centre" : $"CC · {item.CostCode}";
            case BidPackageLineCoverage.Variation:
                var variation = variations.FirstOrDefault(v => v.VariationOrderId == item.VariationOrderId);
                return variation is null ? "Variation" : $"{variation.DisplayNumber} · {variation.Title}";
            default:
                return "";
        }
    }

    private void OpenCoverageModal(BidPackageLineItem item)
    {
        linkingLine = item;
        coverageChoice = item.Coverage;
        coverageCostCode = item.CostCode;
        coverageVariationId = item.VariationOrderId;
        showCoverageModal = true;
    }

    private void CloseCoverageModal()
    {
        showCoverageModal = false;
        linkingLine = null;
    }

    private async Task ConfirmCoverage()
    {
        if (busy || linkingLine is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var costCode = coverageChoice == BidPackageLineCoverage.ContractLine && !string.IsNullOrWhiteSpace(coverageCostCode) ? coverageCostCode : null;
            var variationId = coverageChoice == BidPackageLineCoverage.Variation && !string.IsNullOrWhiteSpace(coverageVariationId) ? coverageVariationId : null;
            fetchedLineItems = await Commands.SendAsync(
                new SetBidPackageLineItemCoverage(linkingLine.LineItemId, coverageChoice,
                    BoqLineItemId: null, VariationOrderId: variationId, CostCode: costCode), CancellationToken.None);
            showCoverageModal = false;
            linkingLine = null;
        }
        catch { error = "Couldn't update coverage. Make sure a cost centre or variation order is selected, then try again."; }
        finally { busy = false; }
    }

    private static string Append(string? existing, string line) =>
        string.IsNullOrEmpty(existing) ? line : existing + "\n" + line;

    // "00006-12 — Plastering" for a known code; the bare code when the master list hasn't loaded.
    private string CostCentreLabel(string code)
    {
        var centre = CostCenters.Alphabetical.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        return centre is null ? code : $"{centre.Code} — {centre.Name}";
    }

    // Flip the package's materials flag: when on, the drafted tender invite asks each
    // subcontractor to state whether they will supply their own materials or price labour-only.
    private async Task ToggleMaterialsApplicable(bool applicable)
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(
                new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status, package.OwnerEmail, applicable),
                CancellationToken.None);
        }
        catch { error = "Couldn't update the materials setting. Please try again."; }
        finally { busy = false; }
    }

    // ---- Select line items from the valuation report (the fast path onto the package) ----
    // The picker (ValuationLinePickerModal) owns the report read and the selection; the page
    // appends what it confirms through AddBidPackageLineItems, which never touches existing
    // lines' ids, coverage links or quote references.

    private bool showValuationPicker;

    private void OpenValuationPicker() => showValuationPicker = true;

    private void CloseValuationPicker() => showValuationPicker = false;

    private async Task AppendValuationLinesAsync(IReadOnlyList<BidPackageLineItemInput> inputs)
    {
        if (busy || package is null || !CanEdit || inputs.Count == 0) return;
        error = null;
        try
        {
            busy = true;
            fetchedLineItems = await Commands.SendAsync(new AddBidPackageLineItems(BidPackageId, inputs), CancellationToken.None);
            showValuationPicker = false;
        }
        catch { error = "Couldn't add the selected line items — check the package's line list before retrying."; }
        finally { busy = false; }
    }
}
