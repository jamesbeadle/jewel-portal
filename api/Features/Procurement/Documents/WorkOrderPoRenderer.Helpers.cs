using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    // ---- Helpers ------------------------------------------------------------------------------

    private static void SectionHeading(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 10.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        p.Format.Borders.Bottom.Width = 0.75;
        p.Format.Borders.Bottom.Color = Orange;
        p.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        p.Format.KeepWithNext = true;
        SpaceBefore(p, 4);
        SpaceAfter(p, 2.5);
    }

    private static void SubHeading(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        p.Format.KeepWithNext = true;
        SpaceBefore(p, 2.5);
        SpaceAfter(p, 1);
    }

    private static void BodyText(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 8.5;
        SpaceAfter(p, 1.2);
    }

    /// <summary>Free text typed with line breaks (Scope, ProgrammeNotes) keeps them — one
    /// paragraph per typed line, blanks collapsed, mirroring the sheet's pre-wrap rendering.</summary>
    private static void PrewrapText(Section section, string text)
    {
        foreach (var line in Prewrap(text))
            BodyText(section, line);
    }

    private static void PrewrapCell(Cell cell, string text)
    {
        var lines = Prewrap(text).ToList();
        if (lines.Count == 0)
        {
            var dash = cell.AddParagraph("—");
            dash.Format.LeftIndent = Unit.FromMillimeter(1.5);
            dash.Format.Font.Size = 8;
            dash.Format.Font.Color = Muted;
            return;
        }
        foreach (var line in lines)
        {
            var p = cell.AddParagraph(line);
            p.Format.LeftIndent = Unit.FromMillimeter(1.5);
            p.Format.Font.Size = 8;
        }
    }

    private static IEnumerable<string> Prewrap(string text) =>
        (text ?? "").Replace("\r\n", "\n").Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line));

    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 7.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(Money(amount));
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = Ink;
    }

    private static void AddGridRow(Table table, string l1, string v1, string l2, string v2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], l1);
        ValueCell(row.Cells[1], v1);
        LabelCell(row.Cells[2], l2);
        ValueCell(row.Cells[3], v2);
    }

    private static void LabelCell(Cell cell, string text)
    {
        cell.Shading.Color = Panel;
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void ValueCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        p.Format.Font.Size = 9;
        p.Format.Font.Color = Ink;
    }


    private static void SpaceBefore(Paragraph p, double mm) => p.Format.SpaceBefore = Unit.FromMillimeter(mm);
    private static void SpaceAfter(Paragraph p, double mm) => p.Format.SpaceAfter = Unit.FromMillimeter(mm);


    // U+2212 MINUS SIGN, not the hyphen-minus: MigraDoc breaks a line after a hyphen that is not
    // followed by a digit, so "-£1,000.00" could print as a bare "-" with the figure on the next
    // line (the valuation report did exactly that, 2026-09-02). Same rule as
    // ValuationReportSnapshotRenderer.Money.
    private static string Money(decimal value) => value.ToString("£#,##0.00;\u2212£#,##0.00", Uk);
    private static string Date(DateTimeOffset value) => value.LocalDateTime.ToString("d MMM yyyy", Uk);
    private static string DateTime(DateTimeOffset value) => value.LocalDateTime.ToString("d MMM yyyy, HH:mm", Uk);
}
