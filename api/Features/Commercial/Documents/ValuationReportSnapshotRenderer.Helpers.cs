using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Commercial;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ValuationReportSnapshotRenderer
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

    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 7.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false, Color? colour = null)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(Money(amount));
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = colour ?? Ink;
    }

    // Quantity / rate: a consolidated variation row has neither, and prints a dash instead.
    private static void NumberCell(Cell cell, decimal? value)
    {
        var p = cell.AddParagraph(value is { } number ? Num(number) : "—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
    }

    private static void DashCell(Cell cell)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph("—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
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


    private static string Money(decimal value) => value.ToString("£#,##0.00;-£#,##0.00", Uk);
    private static string Num(decimal value) => value.ToString("0.##", Uk);
    private static string Pct(decimal value) => value.ToString("0.##", Uk) + "%";
    private static string DateTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);
}
