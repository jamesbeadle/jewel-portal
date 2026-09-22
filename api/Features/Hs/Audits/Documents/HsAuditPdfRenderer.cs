using Jewel.JPMS.Api.Features.Documents;
using Jewel.JPMS.Contracts.Hs;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Documents;

/// <summary>
/// The officer's inspection report as a PDF in the house style, laid out as her sheet is: the
/// front sheet, the score, the keys, the eleven sections with the rows she wrote on, the
/// further comments, and the two declarations. Built from the audit view alone, in any status,
/// so what she downloads is what the record says.
/// </summary>
public static partial class HsAuditPdfRenderer
{
    public static byte[] Render(HsAuditView view, string projectName, DateTimeOffset generatedAt)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{view.Audit.Reference} {HsAuditReportText.DocumentTitle}";
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = projectName;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);
        AddHeader(section, view.Audit, projectName);
        AddFrontSheet(section, view.Audit, projectName);
        AddKeys(section);
        AddSections(section, view);
        AddFurtherComments(section, view.Audit);
        AddDeclarations(section, view.Audit);
        HouseFooter(section, HsAuditReportText.Provenance(view.Audit, generatedAt));

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddHeader(Section section, HsAudit audit, string projectName)
    {
        CleanHeader(section, HsAuditReportText.DocumentTitle, projectName,
            new HeaderFact(audit.Reference),
            new HeaderFact(audit.Status.DisplayName()),
            new HeaderFact($"Inspected  {Date(audit.InspectionDate)}"));
    }

    private static void AddFrontSheet(Section section, HsAudit audit, string projectName)
    {
        var table = GridTable(section);
        GridRow(table, "Site", projectName, "Type of report", audit.Type.DisplayName());
        GridRow(table, "Site manager", HsAuditReportText.OrDash(audit.SiteManagerName), "Safety officer", HsAuditReportText.OrDash(audit.SafetyOfficerName));
        GridRow(table, "Audit score", HsAuditReportText.Score(audit), "Previous audit score", HsAuditReportText.PreviousScore(audit));
        GridRow(table, "No. site operatives", HsAuditReportText.Count(audit.SiteOperativeCount), "Framework version", audit.TemplateVersion);
        SpaceAfterTable(section);
        if (string.IsNullOrWhiteSpace(audit.SummaryOfWorkActivities)) return;
        SectionHeading(section, "Summary of work activities");
        Panelled(section, audit.SummaryOfWorkActivities);
        SpaceAfterTable(section);
    }

    private static void AddKeys(Section section)
    {
        MutedLine(section, HsAuditReportText.RateKey);
        MutedLine(section, HsAuditReportText.ClassKey);
        MutedLine(section, HsAuditReportText.TimeScaleKey);
    }

    private static void AddFurtherComments(Section section, HsAudit audit)
    {
        SectionHeading(section, "Any further comments");
        Panelled(section, HsAuditReportText.OrDash(audit.FurtherComments));
        SpaceAfterTable(section);
    }

    private static void AddDeclarations(Section section, HsAudit audit)
    {
        SectionHeading(section, "Safety officer declaration");
        Panelled(section, HsAuditReportText.OfficerDeclaration);
        MutedLine(section, HsAuditReportText.DeclaredBy(audit.SafetyOfficerName, audit.IssuedAt));
        SectionHeading(section, "Manager / supervisor declaration");
        Panelled(section, HsAuditReportText.ManagerDeclaration);
        MutedLine(section, HsAuditReportText.DeclaredBy(audit.ManagerName, audit.ClosedAt));
    }
}
