using System.Text.Json;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Ai;
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

    // ---- Line-item editing ----

    private sealed class LineDraft
    {
        public string Trade { get; set; } = "";
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public string CostCode { get; set; } = "";
    }

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
    //
    // The picker reads the LIVE report (ListValuationLinesForProject) — the same rows the
    // Valuation tab edits — so the cost codes on offer are exactly the ones with a sale-side
    // home, variations included. Nothing of the sale figures is persisted here: ticked rows
    // become plain BidPackageLineItemInputs and land through AddBidPackageLineItems, which
    // appends without touching existing lines' ids, coverage links or quote references.

    private bool showValuationPicker;
    private IReadOnlyList<ValuationLineItem>? valuationLines;   // null = no fetch has landed
    private bool valuationLinesFailed;
    private readonly HashSet<string> valuationSelection = new();
    private string valuationSearch = "";

    private void OnValuationSearchInput(ChangeEventArgs e) => valuationSearch = e.Value?.ToString() ?? "";

    // The search narrows what's LISTED, never what's SELECTED: ticks made under one search
    // survive the next, and the confirm reads valuationSelection against the full report.
    // Matches description (as the package line will carry it), cost code and section header,
    // so "SUB-PIL", "standing charge" and "PC sums" all narrow the way you'd expect.
    private IReadOnlyList<ValuationLineItem> FilteredValuationLines
    {
        get
        {
            var q = valuationSearch.Trim();
            if (q.Length == 0) return SelectableValuationLines;
            return SelectableValuationLines
                .Where(line => ValuationLineDescription(line).Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (line.CostCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || ValuationGroupLabel(line).Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    // Declined rows are recorded on the report but priced into nothing — not scope to tender.
    // Order mirrors the report: element blocks in bill order, DisplayOrder within.
    private IReadOnlyList<ValuationLineItem> SelectableValuationLines => (valuationLines ?? Array.Empty<ValuationLineItem>())
        .Where(line => line.LineType != ValuationLineType.Declined)
        .OrderBy(line => line.ElementType)
        .ThenBy(line => line.DisplayOrder)
        .ToList();

    private async Task OpenValuationPicker()
    {
        if (busy || package is null || !CanEdit) return;
        valuationSelection.Clear();
        valuationSearch = "";
        error = null;
        showValuationPicker = true;
        // Stale-while-revalidate: rows already fetched keep showing while the fresh fetch lands,
        // so reopening the picker after editing the report picks up the change.
        try
        {
            valuationLines = await Queries.AskAsync(new ListValuationLinesForProject(ProjectId), CancellationToken.None);
            valuationLinesFailed = false;
        }
        catch { valuationLinesFailed = valuationLines is null; }
    }

    private void CloseValuationPicker()
    {
        if (busy) return;
        showValuationPicker = false;
        valuationSelection.Clear();
        valuationSearch = "";
    }

    private void ToggleValuationLine(ValuationLineItem line)
    {
        if (string.IsNullOrWhiteSpace(line.CostCode)) return;
        if (!valuationSelection.Remove(line.ValuationLineItemId)) valuationSelection.Add(line.ValuationLineItemId);
    }

    private void ToggleValuationGroup(IEnumerable<ValuationLineItem> group)
    {
        var selectable = group.Where(l => !string.IsNullOrWhiteSpace(l.CostCode)).Select(l => l.ValuationLineItemId).ToList();
        if (selectable.Count == 0) return;
        if (selectable.All(valuationSelection.Contains)) valuationSelection.ExceptWith(selectable);
        else valuationSelection.UnionWith(selectable);
    }

    private async Task ConfirmValuationPicker()
    {
        if (busy || package is null || !CanEdit || valuationSelection.Count == 0) return;
        error = null;
        try
        {
            busy = true;
            var inputs = SelectableValuationLines
                .Where(line => valuationSelection.Contains(line.ValuationLineItemId) && !string.IsNullOrWhiteSpace(line.CostCode))
                .Select(line => new BidPackageLineItemInput(
                    ValuationLineDescription(line),
                    line.Unit.Trim(),
                    line.Quantity,
                    package.Trade,
                    line.CostCode.Trim()))
                .ToList();
            fetchedLineItems = await Commands.SendAsync(new AddBidPackageLineItems(BidPackageId, inputs), CancellationToken.None);
            showValuationPicker = false;
            valuationSelection.Clear();
        }
        catch { error = "Couldn't add the selected line items — check the package's line list before retrying."; }
        finally { busy = false; }
    }

    // Section headers mirroring the report's blocks; contract works keep their section identity.
    private static string ValuationGroupLabel(ValuationLineItem line) => line.ElementType switch
    {
        ValuationElementType.ContractWorks =>
            string.IsNullOrWhiteSpace(line.SectionCode) && string.IsNullOrWhiteSpace(line.SectionName)
                ? "Contract works"
                : $"{line.SectionCode} — {line.SectionName}".Trim(' ', '—'),
        ValuationElementType.PcSum => "PC sums",
        ValuationElementType.Contingency => "Contingency",
        _ => "Variations",
    };

    // The description the package line will carry. Variation lines lead with their V-number so
    // the tenderer's schedule says which change the scope belongs to; blank descriptions fall
    // back to the variation title or the section name rather than arriving empty.
    private static string ValuationLineDescription(ValuationLineItem line)
    {
        var description = line.Description.Trim();
        if (line.ElementType == ValuationElementType.Variation)
        {
            if (description.Length == 0) description = line.VariationTitle.Trim();
            var reference = line.VariationRef.Trim();
            if (reference.Length > 0 && description.Length > 0) description = $"{reference} — {description}";
            else if (reference.Length > 0) description = reference;
        }
        return description.Length == 0 ? line.SectionName.Trim() : description;
    }

    private static string? ValuationLineTypeBadge(ValuationLineItem line) => line.LineType switch
    {
        ValuationLineType.ProvisionalSum => "PS",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Tbc => "TBC",
        _ => null,
    };

}
