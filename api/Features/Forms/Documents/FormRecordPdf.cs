using Jewel.JPMS.Api.Features.Forms.Answers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Forms.Documents;

/// <summary>
/// A completed form as a record to keep or print — the answers sheet the dashboard filed next to the
/// uploads (Jeremy, 26 Aug: a DSE "should be in SharePoint"), built from the record on every download
/// and never stored, in the house style every Jewel Bespoke Build document wears. Held-back answers stay
/// held back.
/// </summary>
public static class FormRecordPdf
{
    private const string HeldBack = "Held back — reveal it in the portal";

    public static byte[] Render(FormSubmissionView view, DateTimeOffset generatedAt)
    {
        EnsureFonts();
        var submission = view.Submission;
        var form = FormCatalogue.For(submission.FormSlug);
        var title = form?.Title ?? submission.FormSlug;
        var document = new Document();
        document.Info.Title = $"{title} - {submission.SubmitterName}";
        document.Info.Author = JewelBespokeBuild.LegalName;
        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;
        var section = A4Page(document);
        CleanHeader(section, title, submission.SubmitterName, HeaderFacts(submission));
        AddAnswers(section, form, view);
        AddFiles(section, view);
        HouseFooter(section, $"Form {submission.FormSubmissionId} · generated {DateAndTime(generatedAt)} · from the JPMS register (source of truth)");
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static HeaderFact[] HeaderFacts(FormSubmission submission) => new[]
    {
        new HeaderFact($"Submitted {DateAndTime(submission.SubmittedAt)}"),
        new HeaderFact(submission.IsVerifiedLink ? $"Sent through a one-time link to {submission.SentToEmail}" : "")
    };

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
}
