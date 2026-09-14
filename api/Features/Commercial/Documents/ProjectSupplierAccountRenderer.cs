using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Renders one supplier's account on one project into a branded PDF using PDFsharp/MigraDoc —
/// the sheet the finance director sends the managing director when a supplier has invoiced past
/// their orders (2026-09-14): the orders with their lines and the invoices linked to each, then
/// every invoice received with its CIS labour / materials split, its Xero status and what it is
/// matched to, and the position the two halves add up to. Pure function of the account, so the
/// download and any future email attachment render identically. One partial per section.
/// </summary>
public static partial class ProjectSupplierAccountRenderer
{
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);

    public static byte[] Render(ProjectSupplierAccount account)
    {
        EnsureFonts();
        var pdf = NewDocument(account);
        var section = A4Page(pdf);

        AddHeaderBand(section, account);
        AddDetailsGrid(section, account);
        AddOrders(section, account);
        AddInvoices(section, account);
        AddPosition(section, account);
        AddClosingNote(section);
        HouseFooter(section, $"Generated {DateAndTime(account.GeneratedAt)} · from the JPMS register (source of truth)");

        return ToPdfBytes(pdf);
    }

    private static Document NewDocument(ProjectSupplierAccount account)
    {
        var pdf = new Document();
        pdf.Info.Title = $"{account.SupplierName} — supplier account — {account.ProjectName}".Trim();
        pdf.Info.Author = "Jewel Bespoke Build";
        pdf.Info.Subject = "Supplier account";

        var normal = pdf.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;
        return pdf;
    }

    private static byte[] ToPdfBytes(Document pdf)
    {
        var renderer = new PdfDocumentRenderer { Document = pdf };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddClosingNote(Section section)
    {
        var note = section.AddParagraph(
            "All figures are net of VAT; credit notes deduct. Labour is the net each bill posts to the CIS "
            + "labour account and materials everything else, exactly as the supplier raised it. Invoiced and "
            + "linked, paid and left to invoice are read from the bills linked to each order on the WO "
            + "Allocation tab; an invoice awaiting approval counts as received, but nothing is owed on it "
            + "until it is approved in Xero.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }
}
