using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// One printed row of a bill section on the valuation report PDF. Contract, PC and contingency
/// work prints EVERY line, grouped under its area heading — the accountant reconciles item by
/// item, so nothing there consolidates (accountant's request 2026-08-26; area roll-ups were the
/// old behaviour). Only variations consolidate: one row per variation order
/// (<see cref="VariationOrderRollUps"/>), every cost centre the order touches folded into it —
/// the consolidated row carries the lines' summed money and their weighted % complete, priced
/// as one item at the order's total, with the itemised detail on the workbook's per-order tabs.
/// </summary>
internal sealed record ValuationReportBillRow(
    string Code,
    string ClientReference,
    string Title,
    string Comments,
    string KindLabel,        // "" for a priced row; "Omit", "Declined", "TBC"… otherwise
    decimal? Quantity,
    decimal? Rate,
    decimal Amount,
    decimal PercentComplete,
    decimal PreviousClaimed,
    decimal PeriodIncrement,
    decimal CumulativeClaimed,
    bool CountsTowardTotals)
{
    public bool MovedThisPeriod => CountsTowardTotals && PeriodIncrement != 0m;
}

/// <summary>A run of bill rows under one area sub-heading; a blank title prints no heading row
/// (a section whose lines carry no area at all reads as one flat run).</summary>
internal sealed record ValuationReportBillGroup(string AreaTitle, IReadOnlyList<ValuationReportBillRow> Rows);

internal static class ValuationReportBillRows
{
    private const decimal OneItem = 1m;

    public static IReadOnlyList<ValuationReportBillGroup> GroupsFor(
        IEnumerable<ValuationReportSnapshotLine> lines, ValuationElementType elementType, Func<string, string?> costCentreNameFor)
    {
        var ofType = lines.Where(line => line.ElementType == elementType).ToList();
        if (ofType.Count == 0)
            return Array.Empty<ValuationReportBillGroup>();
        if (elementType == ValuationElementType.Variation)
        {
            // Variations never sub-head: the one-row-per-order consolidation IS the grouping.
            // Only approved orders print — an order with nothing priced into the totals (TBC
            // placeholders left by a return to quoting, declined work) is not on the statement
            // at all; the register holds its story (accountant 2026-08-26).
            var rows = VariationOrderRollUps.Build(ofType)
                .Where(rollUp => rollUp.CountsTowardTotals)
                .Select(rollUp => rollUp.IsRolledUp ? Consolidated(rollUp) : FromLine(rollUp.Lines[0]))
                .ToList();
            return rows.Count == 0
                ? Array.Empty<ValuationReportBillGroup>()
                : new[] { new ValuationReportBillGroup("", rows) };
        }
        return AreaGroups(ofType, costCentreNameFor);
    }

    // The same consecutive-run rule as the screen and the workbook (ValuationReportAreas): a
    // line whose title matches the run above continues it, a blank title continues it too, so
    // the PDF's area headings sit exactly where the other surfaces put theirs.
    private static IReadOnlyList<ValuationReportBillGroup> AreaGroups(
        IEnumerable<ValuationReportSnapshotLine> linesOfOneType, Func<string, string?> costCentreNameFor)
    {
        var groups = new List<(string Title, List<ValuationReportBillRow> Rows)>();
        var currentArea = "";
        foreach (var line in linesOfOneType.OrderBy(line => line.DisplayOrder))
        {
            var title = ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, costCentreNameFor);
            if (groups.Count == 0 || ValuationReportAreas.StartsNewArea(title, currentArea))
            {
                currentArea = title;
                groups.Add((title, new List<ValuationReportBillRow>()));
            }
            groups[^1].Rows.Add(FromLine(line));
        }
        return groups
            .Select(group => new ValuationReportBillGroup(group.Title, group.Rows))
            .ToList();
    }

    private static ValuationReportBillRow FromLine(ValuationReportSnapshotLine line) =>
        new(CodeFor(line), line.ClientReference, TitleFor(line), line.Comments,
            line.CountsTowardTotals ? "" : LineTypeLabel(line.LineType),
            line.Quantity, line.Rate, line.LineAmount, line.PercentComplete,
            line.CumulativeClaimed - line.PeriodIncrement, line.PeriodIncrement, line.CumulativeClaimed,
            line.CountsTowardTotals);

    private static ValuationReportBillRow Consolidated(VariationRollUp<ValuationReportSnapshotLine> rollUp)
    {
        var claimed = rollUp.CountingLines.Sum(line => line.CumulativeClaimed);
        var period = rollUp.CountingLines.Sum(line => line.PeriodIncrement);
        return new(rollUp.VariationRef, SharedClientReference(rollUp), rollUp.VariationTitle,
            $"{rollUp.Lines.Count} items", rollUp.CountsTowardTotals ? "" : "Not priced",
            OneItem, rollUp.Amount, rollUp.Amount, VariationRollUps.WeightedPercent(claimed, rollUp.Amount),
            claimed - period, period, claimed, rollUp.CountsTowardTotals);
    }

    // The client's reference prints on the order's row only when every line under it agrees;
    // an order spread across differently-referenced cost centres shows none rather than one
    // line's reference posing as the order's.
    private static string SharedClientReference(VariationRollUp<ValuationReportSnapshotLine> rollUp)
    {
        var distinct = rollUp.Lines
            .Select(line => line.ClientReference.Trim())
            .Where(reference => reference.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return distinct.Count == 1 ? distinct[0] : "";
    }

    // Same code/title fallbacks as the on-screen snapshot viewer, so PDF and screen always agree.
    private static string CodeFor(ValuationReportSnapshotLine line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string TitleFor(ValuationReportSnapshotLine line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    private static string LineTypeLabel(ValuationLineType type) => type switch
    {
        ValuationLineType.ProvisionalSum => "Provisional sum",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Declined => "Declined",
        ValuationLineType.Tbc => "TBC",
        _ => type.ToString()
    };
}
