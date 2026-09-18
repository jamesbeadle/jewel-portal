using System.Globalization;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial.Export;

/// <summary>
/// A valuation statement — a locked valuation's frozen rows, or a Draft's freshly computed
/// working copy — as the workbook's lines, summary footer and identity strip. This is the ONE
/// mapping the statement viewer's Export button and the connector's export_valuation_report share (2026-09-02, the
/// accountant's ask: pull the portal's own file rather than rebuild it), so a spreadsheet
/// pulled through Claude is the spreadsheet the portal's button produces, cell for cell.
/// Sections come out in statement order (Contract Works, Provisional Sums, Contingency Sums,
/// Variations); each line's Previous is its cumulative claimed less the period increment
/// frozen at lock; the summary rows are the valuation's own footer. The code, title
/// and kind-label fallbacks are the report table's and the PDF renderer's, so every surface
/// reads the same line the same way.
/// </summary>
public static class ValuationStatementExport
{
    private static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");
    private const string StampFormat = "dd MMM yyyy HH:mm";

    /// <summary>The statement's sections, in the order every surface prints them.</summary>
    public static readonly IReadOnlyList<(string Title, ValuationElementType Type)> Sections = new[]
    {
        ("Contract Works", ValuationElementType.ContractWorks),
        ("Provisional Sums", ValuationElementType.PcSum),
        ("Contingency Sums", ValuationElementType.Contingency),
        ("Variations", ValuationElementType.Variation),
    };

    /// <summary>Every frozen line as an export line, section by section in statement order.</summary>
    /// <param name="costCentreName">Cost code → master name, for the area sub-heading of a line
    /// that carries no estimate section (the ValuationReportAreas rule); null falls back to the code.</param>
    public static IReadOnlyList<ValuationExportLine> Lines(
        IReadOnlyList<ValuationStatementLine> lines, Func<string, string?> costCentreName)
    {
        var result = new List<ValuationExportLine>();
        foreach (var (title, type) in Sections)
        {
            foreach (var line in lines.Where(line => line.ElementType == type).OrderBy(line => line.DisplayOrder))
                result.Add(LineFor(title, line, costCentreName));
        }
        return result;
    }

    public static ValuationExportLine LineFor(
        string sectionTitle, ValuationStatementLine line, Func<string, string?> costCentreName) =>
        new(sectionTitle,
            line.ElementType,
            AreaFor(line, costCentreName),
            CodeFor(line),
            TitleFor(line),
            LineTypeLabel(line.LineType),
            line.CountsTowardTotals,
            line.Unit,
            line.Quantity,
            line.Rate,
            line.LineAmount,
            line.PercentComplete,
            line.CumulativeClaimed - line.PeriodIncrement,
            line.PeriodIncrement,
            line.CumulativeClaimed,
            line.Comments,
            line.VariationRef,
            line.VariationTitle,
            line.CostCode,
            line.DisplayOrder,
            line.ClientReference);

    /// <summary>The summary footer as the snapshot froze it — the same rows, in the same order,
    /// as the snapshot viewer's footer and the PDF's summary block.</summary>
    public static IReadOnlyList<ValuationExportSummaryRow> Summary(
        ValuationClaim snapshot, IReadOnlyList<ValuationStatementLine> lines)
    {
        var periodTotal = lines.Where(line => line.CountsTowardTotals).Sum(line => line.PeriodIncrement);

        var summary = new List<ValuationExportSummaryRow>
        {
            new("Original contract sum", snapshot.ContractSum),
            new("Net variations", snapshot.NetVariations),
            new("Revised contract sum", snapshot.RevisedContractSum, Strong: true),
            new("Total works complete", snapshot.TotalWorksComplete),
            new("Works claimed this period", periodTotal),
            new($"Retention held ({Pct(snapshot.RetentionPercent)})", snapshot.RetentionHeld),
            new($"Retention released ({Pct(snapshot.RetentionReleasePercent)})", snapshot.RetentionReleased),
            new("Certified to date", snapshot.CertifiedToDate),
        };
        if (snapshot.DepositPercent > 0m || snapshot.DepositReleased != 0m)
        {
            summary.Add(new("Payment due before deposit (ex VAT)", snapshot.PaymentDueExVat + snapshot.DepositReleased));
            summary.Add(new($"Less deposit released ({Pct(snapshot.DepositPercent)})", snapshot.DepositReleased));
        }
        summary.Add(new("Payment due (ex VAT)", snapshot.PaymentDueExVat, Strong: true));
        return summary;
    }

    /// <summary>The identity strip: a locked valuation says when it was locked and that it is the
    /// issued record; a working copy says when it was prepared and that the live report is what
    /// it reads (its label already carries the "— working copy" wording).</summary>
    public static ValuationExportMeta Meta(ValuationStatement statement) =>
        statement.IsDraft
            ? new ValuationExportMeta(
                statement.Label,
                $"Prepared {Stamp(statement.AsAt)} · working copy of the live report",
                IsDraft: true)
            : new ValuationExportMeta(
                statement.Label,
                $"Locked {Stamp(statement.AsAt)} · issued statement from the JPMS register",
                IsDraft: false);

    // Invariant, not en-GB: ICU on Linux (the Functions host, the CI runner) abbreviates September
    // as "Sept" under en-GB while macOS gives "Sep", so the stamp differed by platform.
    private static string Stamp(DateTimeOffset takenAt) =>
        takenAt.ToString(StampFormat, CultureInfo.InvariantCulture);

    // The area sub-heading the line falls under — the estimate section frozen on the line, else
    // the cost centre's master name; variations never group by area.
    public static string AreaFor(ValuationStatementLine line, Func<string, string?> costCentreName) =>
        ValuationReportAreas.GroupsByArea(line.ElementType)
            ? ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, costCentreName)
            : "";

    public static string CodeFor(ValuationStatementLine line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : VariationRefs.Padded(line.VariationRef))
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    // Variation lines lead with their own description; the order's title is the fallback.
    public static string TitleFor(ValuationStatementLine line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    // Same wording as the PDF renderer, so the workbook and the statement agree.
    public static string LineTypeLabel(ValuationLineType type) => type switch
    {
        ValuationLineType.Priced => "Priced",
        ValuationLineType.ProvisionalSum => "Provisional sum",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Declined => "Declined",
        ValuationLineType.Tbc => "TBC",
        _ => type.ToString()
    };

    private static string Pct(decimal value) => value.ToString("0.##", Gb) + "%";
}
