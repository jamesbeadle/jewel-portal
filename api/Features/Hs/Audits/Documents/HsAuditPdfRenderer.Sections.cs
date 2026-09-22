using Jewel.JPMS.Contracts.Hs;
using MigraDoc.DocumentObjectModel;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Documents;

public static partial class HsAuditPdfRenderer
{
    private static void AddSections(Section section, HsAuditView view)
    {
        foreach (var templateSection in HsAuditTemplate.Sections)
        {
            var rows = view.Items
                .Where(item => item.Section == templateSection.Number)
                .Where(HsAuditReportText.IsWorthPrinting)
                .ToList();
            AddSection(section, templateSection, rows);
        }
    }

    private static void AddSection(Section section, HsAuditTemplateSection templateSection, IReadOnlyList<HsAuditItem> rows)
    {
        SectionHeading(section, $"{templateSection.Number}  {templateSection.Name}");
        if (rows.Count == 0) { MutedLine(section, HsAuditReportText.NothingRecorded); return; }

        var table = RegisterTable(section,
            ("Code", 1.2, false), ("Item", 3.4, false), ("Comment", 1.3, false), ("Rate", 0.9, true), ("Class", 0.9, false),
            ("Time", 0.9, false), ("Findings / advice given", 5.6, false), ("Owner", 2.2, false), ("Rectified", 1.4, false));
        foreach (var item in rows) BodyRow(table, RowValues(item));
        SpaceAfterTable(section);
    }

    private static string[] RowValues(HsAuditItem item) => new[]
    {
        item.Code,
        item.Name,
        item.Comment is { } comment ? comment.Code() : HsAuditReportText.Dash,
        item.Rate is { } rate ? ((int)rate).ToString() : HsAuditReportText.Dash,
        item.Class is { } hsAuditClass ? hsAuditClass.Letter() : HsAuditReportText.Dash,
        item.TimeScale is { } timeScale ? timeScale.Code() : HsAuditReportText.Dash,
        HsAuditReportText.OrDash(item.Findings),
        HsAuditReportText.OrDash(item.OwnerName),
        item.DateRectified is { } rectified ? Date(rectified) : HsAuditReportText.Dash
    };
}
