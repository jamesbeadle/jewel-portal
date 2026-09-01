using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Documents;

/// <summary>
/// The PQQ response's sections: the subject line, the details grid, the scope the architect
/// described, and the questionnaire itself — each question as a heading with Jewel's answer in a
/// panel beneath, in the order the architect asked.
/// </summary>
internal static class TenderEnquiryDocumentSections
{
    private const string NoAnswerYet = "—";

    public static void AddTitleBlock(Section section, TenderEnquiryDocumentModel model)
    {
        var label = section.AddParagraph("PROJECT");
        label.Format.Font.Size = 7.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Muted;
        SpaceBefore(label, 3);
        SpaceAfter(label, 1);

        var title = section.AddParagraph(model.Title);
        title.Format.Font.Size = 13;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Navy;
        SpaceAfter(title, 2);
    }

    public static void AddDetailsGrid(Section section, TenderEnquiryDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelWidth = Unit.FromCentimeter(3.3);
        var valueWidth = Unit.FromCentimeter(5.6);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);

        AddGridRow(table, "Architect", model.ArchitectPracticeName, "Contact", model.ArchitectContactName);
        AddGridRow(table, "Site address", model.SiteAddress, "Contract form", model.ContractForm);
        AddGridRow(table, "Our reference", $"{model.ProjectReference} · {model.Reference}", "Prepared by", model.OwnerEmail);
        SpaceAfterTable(section);
    }

    public static void AddScopeOfWorks(Section section, TenderEnquiryDocumentModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ScopeSummary)) return;
        SectionHeading(section, "The works, as described in the enquiry");
        Panelled(section, model.ScopeSummary);
        SpaceAfterTable(section);
    }

    public static void AddAnswers(Section section, TenderEnquiryDocumentModel model)
    {
        SectionHeading(section, "Our responses to the questionnaire");
        if (model.Answers.Count == 0)
        {
            Panelled(section, "No answers have been entered yet.");
            SpaceAfterTable(section);
            return;
        }
        foreach (var answer in model.Answers)
        {
            var question = section.AddParagraph($"{answer.Position}.  {answer.Question}");
            question.Format.Font.Size = 9.5;
            question.Format.Font.Bold = true;
            question.Format.Font.Color = Navy;
            question.Format.KeepWithNext = true;
            SpaceBefore(question, 3);
            SpaceAfter(question, 1.5);
            Panelled(section, string.IsNullOrWhiteSpace(answer.Answer) ? NoAnswerYet : answer.Answer);
            SpaceAfterTable(section);
        }
    }

    private static void AddGridRow(Table table, string label1, string value1, string label2, string value2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], label1);
        ValueCell(row.Cells[1], value1);
        LabelCell(row.Cells[2], label2);
        ValueCell(row.Cells[3], value2);
    }
}
