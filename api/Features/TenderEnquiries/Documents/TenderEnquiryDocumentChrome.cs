using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Documents;

/// <summary>The PQQ response's chrome — the navy header band (logo, document name, reference,
/// addressee and dates) and the branded footer — in the same house style as the request and
/// variation documents.</summary>
internal static class TenderEnquiryDocumentChrome
{
    private const string DocumentName = "PRE-QUALIFICATION RESPONSE";

    public static void AddHeaderBand(Section section, TenderEnquiryDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;

        var row = table.AddRow();
        row.Shading.Color = Navy;
        row.TopPadding = Unit.FromMillimeter(4);
        row.BottomPadding = Unit.FromMillimeter(4);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(4);
        row.Cells[1].Format.RightIndent = Unit.FromMillimeter(4);
        row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph(DocumentName);
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var reference = row.Cells[0].AddParagraph(model.Reference);
        reference.Format.Font.Size = 9.5;
        reference.Format.Font.Bold = true;
        reference.Format.Font.Color = Gold;

        var addressee = row.Cells[1].AddParagraph($"For {model.ArchitectPracticeName}".ToUpperInvariant());
        addressee.Format.Font.Size = 10;
        addressee.Format.Font.Bold = true;
        addressee.Format.Font.Color = White;
        SpaceAfter(addressee, 2);

        var received = row.Cells[1].AddParagraph($"Enquiry received  {Date(model.ReceivedAt)}");
        received.Format.Font.Size = 8;
        received.Format.Font.Color = White;

        if (model.PqqDueAt is { } dueAt)
        {
            SpaceAfter(received, 0.5);
            var due = row.Cells[1].AddParagraph($"Return by  {Date(dueAt)}");
            due.Format.Font.Size = 8;
            due.Format.Font.Color = Gold;
        }

        Hairline(section);
    }

    public static void AddFooter(Section section, TenderEnquiryDocumentModel model)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Borders.Top.Width = 0.75;
        footer.Format.Borders.Top.Color = Orange;
        footer.Format.Borders.Distance = Unit.FromMillimeter(2);
        footer.Format.Font.Size = 7.5;

        footer.AddFormattedText("◆ ", new Font { Color = Orange, Size = 7.5 });
        footer.AddFormattedText("JEWEL BESPOKE BUILD", new Font { Color = Navy, Bold = true, Size = 7.5 });
        footer.AddFormattedText("    WWW.JEWELBB.CO.UK", new Font { Color = Gold, Bold = true, Size = 7.5 });
        footer.AddTab();
        footer.AddFormattedText($"Generated {DateAndTime(model.GeneratedAt)} · from the JPMS register", new Font { Color = Muted, Size = 7 });
        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(18.3), TabAlignment.Right);
    }
}
