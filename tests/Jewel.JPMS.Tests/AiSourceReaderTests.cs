using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Jewel.JPMS.Api.Features.Ai.Sources;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the source reader (docs/ai/06-context-retrieval.md): a file opens into parts and units,
// reads page through it under a budget without ever losing a later sheet, and search finds where
// a reference lives. The live failure this guards (2026-08-25): a valuation workbook's first
// sheet ate the whole 25,000-character extraction budget and the "V01 - Levelling compound" tab
// was never extracted, so the assistant told the user the file was cut off.
public sealed class AiSourceReaderTests
{
    private static AiSourceDocument Workbook(params (string Sheet, string[] Rows)[] sheets) =>
        new(AiSourceDocument.Workbook,
            sheets.Select(sheet => new AiSourcePart(sheet.Sheet, sheet.Sheet, "row", sheet.Rows)).ToList());

    private static string[] Rows(int count, string prefix = "row") =>
        Enumerable.Range(1, count).Select(i => $"{prefix} {i}\tvalue {i}").ToArray();

    // ---- the live failure: a big first sheet must not hide the later tabs ----

    [Fact]
    public void TheV01Tab_isInTheManifest_andReadable_whateverTheFirstSheetWeighs()
    {
        var document = Workbook(
            ("Valuation No.14", Rows(2_000, "Valuation line")),
            ("V01 - Levelling compound", new[] { "Item\tQty\tRate\tTotal", "Levelling compound\t150\t7.00\t1,050.00" }),
            ("V02 - Additional steel works", Rows(22)));

        var manifest = document.Manifest();
        Assert.Equal(3, manifest.Parts.Count);
        Assert.Equal("V01 - Levelling compound", manifest.Parts[1].Key);
        Assert.Equal(2, manifest.Parts[1].Units);
        Assert.Equal("3 sheets · 2,024 rows", manifest.Summary());

        var read = AiSourceReader.Read(document, "V01 - Levelling compound", 1, AiSourceReader.DefaultReadChars);
        Assert.Contains("[Sheet: V01 - Levelling compound]", read.Text);
        Assert.Contains("2\tLevelling compound\t150\t7.00\t1,050.00", read.Text);
        Assert.True(read.ReachedEnd);
        // Stays inside the named part; the next part is offered, not appended.
        Assert.DoesNotContain("row 1\tvalue 1", read.Text);
        Assert.NotNull(read.Next);
        Assert.Equal("V02 - Additional steel works", read.Next!.Part);
        Assert.Equal(1, read.Next.From);
    }

    [Fact]
    public void Search_findsTheTabNamedForTheReference_andTheRowsThatMentionIt()
    {
        var document = Workbook(
            ("Valuation No.14", new[] { "Ref\tDescription\tSum", "V01\tLevelling compound removal\t1,050.00", "V02\tAdditional steel\t2,400.00" }),
            ("V01 - Levelling compound", new[] { "Item\tQty", "Levelling compound\t150" }));

        var found = AiSourceReader.Search(document, "v01");

        Assert.Single(found.PartsByName);
        Assert.Equal("V01 - Levelling compound", found.PartsByName[0].Key);
        Assert.Single(found.Hits);
        Assert.Equal("Valuation No.14", found.Hits[0].Part);
        Assert.Equal(2, found.Hits[0].Unit);
        Assert.Contains("1,050.00", found.Hits[0].Text);
        Assert.Equal(1, found.TotalHits);
    }

    [Fact]
    public void Search_fallsBackToEveryWordPresent_whenThePhraseMatchesNothing()
    {
        var document = Workbook(("Sheet1", new[] { "Levelling of the compound floor", "Steel works" }));

        var found = AiSourceReader.Search(document, "compound levelling");

        Assert.Single(found.Hits);
        Assert.Equal(1, found.Hits[0].Unit);
    }

    [Fact]
    public void Search_isCaseAndWhitespaceForgiving_andCapsHitsButCountsAll()
    {
        var document = Workbook(("Sheet1", Enumerable.Range(1, 50).Select(i => $"Levelling   Compound {i}").ToArray()));

        var found = AiSourceReader.Search(document, "levelling compound", maxHits: 5);

        Assert.Equal(5, found.Hits.Count);
        Assert.Equal(50, found.TotalHits);
    }

    // ---- paging ----

    [Fact]
    public void Read_pagesUnderTheBudget_andSaysWhereToContinue()
    {
        var document = Workbook(("Big", Rows(5_000)));

        var first = AiSourceReader.Read(document, "Big", 1, AiSourceReader.MinReadChars);
        Assert.False(first.ReachedEnd);
        Assert.NotNull(first.Next);
        Assert.Equal("Big", first.Next!.Part);
        Assert.True(first.Next.From > 1);
        Assert.Equal(first.ToUnit + 1, first.Next.From);
        Assert.Contains($"continues at row {first.Next.From}", first.Text);
        Assert.True(first.Text.Length <= AiSourceReader.MinReadChars + 200); // header + continuation line

        var second = AiSourceReader.Read(document, first.Next.Part, first.Next.From, AiSourceReader.MinReadChars);
        Assert.Equal(first.Next.From, second.FromUnit);
        Assert.Contains($"{first.Next.From}\trow {first.Next.From}", second.Text);
        Assert.Contains($"from row {first.Next.From}", second.Text);
    }

    [Fact]
    public void Read_withNoPart_flowsAcrossParts_untilTheBudgetIsSpent()
    {
        var document = Workbook(("A", Rows(3, "a")), ("B", Rows(3, "b")), ("C", Rows(3, "c")));

        var read = AiSourceReader.Read(document, null, 1, AiSourceReader.DefaultReadChars);

        Assert.Contains("[Sheet: A]", read.Text);
        Assert.Contains("[Sheet: B]", read.Text);
        Assert.Contains("[Sheet: C]", read.Text);
        Assert.True(read.ReachedEnd);
        Assert.Null(read.Next);
    }

    [Fact]
    public void Read_alwaysReturnsAtLeastOneUnit_evenWhenItExceedsTheBudget()
    {
        var document = Workbook(("Wide", new[] { new string('x', 10_000), "second" }));

        var read = AiSourceReader.Read(document, "Wide", 1, AiSourceReader.MinReadChars);

        Assert.Contains(new string('x', 10_000), read.Text);
        Assert.Equal(1, read.ToUnit);
        Assert.NotNull(read.Next);
        Assert.Equal(2, read.Next!.From);
    }

    [Fact]
    public void Read_pastTheEndOfAPart_saysSoInsteadOfReturningNothing()
    {
        var document = Workbook(("A", Rows(3)), ("B", Rows(2)));

        var read = AiSourceReader.Read(document, "A", 10, AiSourceReader.DefaultReadChars);

        Assert.Contains("has 3 rows", read.Text);
        Assert.True(read.ReachedEnd);
        Assert.Equal("B", read.Next!.Part);
    }

    [Fact]
    public void Read_ofAnUnknownPart_throws_soTheToolCanListTheRealOnes()
    {
        var document = Workbook(("A", Rows(1)));
        Assert.Throws<ArgumentException>(() => AiSourceReader.Read(document, "Z", 1, AiSourceReader.DefaultReadChars));
    }

    [Fact]
    public void Part_lookupIsCaseInsensitive_andAcceptsTheLabel()
    {
        var document = new AiSourceDocument(AiSourceDocument.Pdf,
            new[] { new AiSourcePart("p1", "Page 1", "line", new[] { "hello" }), new AiSourcePart("p2", "Page 2", "line", new[] { "world" }) });

        Assert.Equal("p2", document.Part("P2")!.Key);
        Assert.Equal("p2", document.Part("page 2")!.Key);
        Assert.Null(document.Part("p3"));
    }

    // ---- preview and manifest text ----

    [Fact]
    public void Preview_isTheOpeningOfTheFirstPart_underItsOwnSmallBudget()
    {
        var document = Workbook(("First", Rows(500)), ("Second", Rows(5)));

        var preview = AiSourceReader.Preview(document);

        Assert.StartsWith("[Sheet: First — opening rows]", preview);
        Assert.Contains("1\trow 1\tvalue 1", preview);
        Assert.DoesNotContain("Second", preview);
        Assert.True(preview.Length <= AiSourceReader.PreviewChars);
    }

    [Fact]
    public void PartsLine_fencesNames_andAbbreviatesLongWorkbooks()
    {
        var many = Enumerable.Range(1, 50).Select(i => ($"Tab {i}", Rows(1))).ToArray();
        var manifest = Workbook(many).Manifest();

        var line = manifest.PartsLine(maxParts: 12);

        Assert.StartsWith("«Tab 1» · 1 row", line);
        Assert.EndsWith("… and 38 more", line);
    }

    [Fact]
    public void Manifest_roundTripsThroughJson_asStoredOnTheAttachmentRow()
    {
        var manifest = Workbook(("V01 - Levelling compound", Rows(18))).Manifest();

        var json = JsonSerializer.Serialize(manifest);
        var back = JsonSerializer.Deserialize<AiSourceManifest>(json);

        Assert.NotNull(back);
        Assert.Equal(manifest.Kind, back!.Kind);
        Assert.Single(back.Parts);
        Assert.Equal("V01 - Levelling compound", back.Parts[0].Key);
        Assert.Equal(18, back.Parts[0].Units);
        Assert.Equal("row", back.Parts[0].UnitName);
        Assert.Equal(manifest.TotalChars, back.TotalChars);
    }

    // ---- real files through Load ----

    [Fact]
    public void Load_opensAMultiSheetWorkbook_everySheet_columnsAligned()
    {
        byte[] bytes;
        using (var workbook = new XLWorkbook())
        {
            var summary = workbook.Worksheets.Add("Valuation No.14");
            summary.Cell(1, 1).Value = "Ref";
            summary.Cell(1, 3).Value = "Sum";
            summary.Cell(2, 1).Value = "V01";
            summary.Cell(2, 3).Value = 1050m;
            summary.Cell(2, 3).Style.NumberFormat.Format = "#,##0.00";

            var v01 = workbook.Worksheets.Add("V01 - Levelling compound");
            v01.Cell(1, 1).Value = "Levelling compound";
            v01.Cell(1, 2).Value = 150;

            workbook.Worksheets.Add("Empty");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            bytes = stream.ToArray();
        }

        var document = AiSourceReader.Load("Valuation-No.14.xlsx", null, bytes);

        Assert.Equal(AiSourceDocument.Workbook, document.Kind);
        // The empty sheet is not a part; the two with content are, in workbook order.
        Assert.Equal(new[] { "Valuation No.14", "V01 - Levelling compound" }, document.Parts.Select(part => part.Key));
        // Column B is blank on both rows: it stays as an empty field so C lines up under "Sum".
        Assert.Equal("Ref\t\tSum", document.Parts[0].Units[0]);
        Assert.Equal("V01\t\t1,050.00", document.Parts[0].Units[1]);
        Assert.Equal("Levelling compound\t150", document.Parts[1].Units[0]);
    }

    [Fact]
    public void Load_readsCsvAsLines_withABom()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("a,b\r\n1,2\r\n\r\n")).ToArray();

        var document = AiSourceReader.Load("figures.csv", "text/csv", bytes);

        Assert.Equal(AiSourceDocument.Text, document.Kind);
        Assert.Equal(new[] { "a,b", "1,2" }, document.Parts[0].Units);
        Assert.Equal("2 lines", document.Manifest().Summary());
    }

    [Fact]
    public void Load_readsADocx_paragraphsAndTableRows()
    {
        const string documentXml =
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>"
            + "<w:p><w:r><w:t>Commercial basis</w:t></w:r></w:p>"
            + "<w:tbl><w:tr><w:tc><w:p><w:r><w:t>Item</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>£450</w:t></w:r></w:p></w:tc></w:tr></w:tbl>"
            + "</w:body></w:document>";
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry("word/document.xml");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(documentXml);
            }
            bytes = stream.ToArray();
        }

        var document = AiSourceReader.Load("vo.docx", null, bytes);

        Assert.Equal(AiSourceDocument.WordDocument, document.Kind);
        Assert.Equal(new[] { "Commercial basis", "Item\t£450" }, document.Parts[0].Units);
        Assert.Equal("2 paragraphs", document.Manifest().Summary());
    }

    [Fact]
    public void Load_routesOnContentType_whenTheNameIsOdd()
    {
        var bytes = Encoding.UTF8.GetBytes("x\ny");
        var document = AiSourceReader.Load("attachment.bin", "text/plain", bytes);
        Assert.Equal(AiSourceDocument.Text, document.Kind);
    }

    [Fact]
    public void Load_refusesWhatItCannotRead_withTheSentenceToRelay()
    {
        Assert.Throws<NotSupportedException>(() => AiSourceReader.Load("old.doc", null, new byte[] { 1, 2, 3 }));
        var invalid = Assert.Throws<InvalidDataException>(() => AiSourceReader.Load("broken.xlsx", null, new byte[] { 1, 2, 3 }));
        Assert.Contains("could not be opened as a spreadsheet", invalid.Message);
    }

    [Fact]
    public void Load_ofAnImage_carriesTheBytes_andHasNothingToSearch()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

        var document = AiSourceReader.Load("photo.png", null, png);

        Assert.True(document.IsImage);
        Assert.Equal("image/png", document.ImageMediaType);
        Assert.Same(png, document.ImageBytes);
        Assert.Empty(AiSourceReader.Search(document, "anything").Hits);
        Assert.Equal("image", document.Manifest().Summary());
    }
}
