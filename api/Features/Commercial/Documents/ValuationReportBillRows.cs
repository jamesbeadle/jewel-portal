using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// One printed row of a bill section on the valuation report PDF. Contract, PC and contingency
/// lines print one row each under their area title; variations print one row per variation
/// order per cost centre (<see cref="VariationRollUps"/>) — a consolidated row carries the
/// lines' summed money and their weighted % complete, and no single quantity or rate.
/// </summary>
internal sealed record ValuationReportBillRow(
    string Code,
    string ClientReference,
    string Title,
    string Comments,
    string KindLabel,        // "" for a priced row; "Omit", "Declined", "TBC"… otherwise
    string AreaTitle,        // "" when the row carries no area (variations, untitled lines)
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

internal static class ValuationReportBillRows
{
    public static IReadOnlyList<ValuationReportBillRow> For(
        IEnumerable<ValuationReportSnapshotLine> lines, ValuationElementType elementType, Func<string, string?> costCentreNameFor)
    {
        var ofType = lines.Where(line => line.ElementType == elementType).ToList();
        if (elementType == ValuationElementType.Variation)
            return VariationRollUps.Build(ofType)
                .Select(rollUp => rollUp.IsRolledUp ? Consolidated(rollUp, costCentreNameFor) : FromLine(rollUp.Lines[0], ""))
                .ToList();
        return ofType
            .OrderBy(line => line.DisplayOrder)
            .Select(line => FromLine(line, ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, costCentreNameFor)))
            .ToList();
    }

    private static ValuationReportBillRow FromLine(ValuationReportSnapshotLine line, string areaTitle) =>
        new(CodeFor(line), line.ClientReference, TitleFor(line), line.Comments,
            line.CountsTowardTotals ? "" : LineTypeLabel(line.LineType), areaTitle,
            line.Quantity, line.Rate, line.LineAmount, line.PercentComplete,
            line.CumulativeClaimed - line.PeriodIncrement, line.PeriodIncrement, line.CumulativeClaimed,
            line.CountsTowardTotals);

    private static ValuationReportBillRow Consolidated(
        VariationRollUp<ValuationReportSnapshotLine> rollUp, Func<string, string?> costCentreNameFor)
    {
        var claimed = rollUp.CountingLines.Sum(line => line.CumulativeClaimed);
        var period = rollUp.CountingLines.Sum(line => line.PeriodIncrement);
        var centre = costCentreNameFor(rollUp.CostCode) ?? rollUp.CostCode;
        return new(rollUp.VariationRef, ClientReferenceFor(rollUp), rollUp.VariationTitle,
            $"{rollUp.Lines.Count} items — {centre}", rollUp.CountsTowardTotals ? "" : "Not priced", "",
            null, null, rollUp.Amount, VariationRollUps.WeightedPercent(claimed, rollUp.Amount),
            claimed - period, period, claimed, rollUp.CountsTowardTotals);
    }

    // Every line of a roll-up shares a cost centre, so they share the client's reference too.
    private static string ClientReferenceFor(VariationRollUp<ValuationReportSnapshotLine> rollUp) =>
        rollUp.Lines.Select(line => line.ClientReference).FirstOrDefault(reference => !string.IsNullOrWhiteSpace(reference)) ?? "";

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
