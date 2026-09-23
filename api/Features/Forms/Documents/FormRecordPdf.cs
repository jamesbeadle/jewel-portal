using Jewel.JPMS.Api.Features.Forms.Answers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Forms.Documents;

/// <summary>
/// A completed form as a record to keep or print — the answers sheet the dashboard filed next to the
/// uploads (Jeremy, 26 Aug: a DSE "should be in SharePoint"), built from the record on every download
/// and never stored. It carries the company the form spoke for rather than the portal's own branding,
/// because a Jewel Property Serve form is Jewel Property Serve's record. Held-back answers stay held back.
/// </summary>
public static class FormRecordPdf
{
    private const string HeldBack = "Held back — reveal it in the portal";
    private const double HeadingSize = 17;
    private const double FooterSize = 7;

    public static byte[] Render(FormSubmissionView view, DateTimeOffset generatedAt)
    {
        EnsureFonts();
        var submission = view.Submission;
        var form = FormCatalogue.For(submission.FormSlug);
        var company = JewelCompanies.For(submission.Company);
        var title = form?.TitleFor(submission.Company) ?? submission.FormSlug;
        var document = new Document();
        document.Info.Title = $"{title} - {submission.SubmitterName}";
        document.Info.Author = company.LegalName;
        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;
        var section = A4Page(document);
        AddHeading(section, title, view, company);
        AddAnswers(section, form, view);
        AddFiles(section, view);
        AddFooter(section, company, submission, generatedAt);
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddHeading(Section section, string title, FormSubmissionView view, JewelCompanyParticulars company)
    {
        var heading = section.AddParagraph(title);
        var headingFont = heading.Format.Font;
        headingFont.Size = HeadingSize;
        headingFont.Color = Navy;
        var submission = view.Submission;
        var link = submission.IsVerifiedLink ? $" · sent through a one-time link to {submission.SentToEmail}" : "";
        var subtitle = section.AddParagraph($"{company.LegalName} · {submission.SubmitterName} · submitted {DateAndTime(submission.SubmittedAt)}{link}");
        var subtitleFont = subtitle.Format.Font;
        subtitleFont.Color = Muted;
        SpaceAfter(subtitle, 4);
    }

    private static void AddAnswers(Section section, FormDefinition? form, FormSubmissionView view)
    {
        SectionHeading(section, "Answers");
        var table = RegisterTable(section, ("Question", 7.0, false), ("Answer", 10.8, false));
        var lines = form is null ? Array.Empty<FormAnswerLine>() : FormAnswerLines.AllOf(form, view.Answers);
        foreach (var line in lines) BodyRow(table, line.Label, line.Answer);
        foreach (var key in view.WithheldKeys) BodyRow(table, form?.QuestionFor(key)?.Label ?? key, HeldBack);
        SpaceAfterTable(section);
    }

    private static void AddFiles(Section section, FormSubmissionView view)
    {
        if (view.Files.Count == 0) return;
        SectionHeading(section, "Files");
        var table = RegisterTable(section, ("File", 10.8, false), ("Question", 7.0, false));
        foreach (var file in view.Files) BodyRow(table, FileLine(file), file.QuestionKey);
        SpaceAfterTable(section);
    }

    private static string FileLine(FormUploadedFile file) =>
        file.DeletedAt is { } deletedAt ? $"{file.FileName} — deleted {Date(deletedAt)}: {file.DeletionReason}" : file.FileName;

    private static void AddFooter(Section section, JewelCompanyParticulars company, FormSubmission submission, DateTimeOffset generatedAt)
    {
        var footer = section.Footers.Primary.AddParagraph(
            $"{company.LegalName} · form {submission.FormSubmissionId} · generated {DateAndTime(generatedAt)} from the Jewel portal");
        var footerFont = footer.Format.Font;
        footerFont.Size = FooterSize;
        footerFont.Color = Muted;
    }
}
