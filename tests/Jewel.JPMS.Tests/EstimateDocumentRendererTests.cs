using Jewel.JPMS.Api.Features.Sales.Documents;
using Jewel.JPMS.Models;
using UglyToad.PdfPig;
using Xunit;

namespace Jewel.JPMS.Tests;

// The estimate sheet (2026-09-15): renders from the record alone, names the file after the
// property, and the notes' breakdown lines come through as text — the figures print as typed.
public sealed class EstimateDocumentRendererTests
{
    static EstimateDocumentRendererTests()
    {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fonts");
        if (Directory.Exists(fonts))
            Environment.SetEnvironmentVariable("RequestDocuments__FontPath", fonts);
    }

    [Fact]
    public void RendersTheSheet_withTheFiguresAndBreakdownAsTyped()
    {
        var pdf = EstimateDocumentRenderer.Render(new EstimateDocumentRenderer.Model(Estimate(), Lead(), new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)));

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        if (Environment.GetEnvironmentVariable("ESTIMATE_PDF_OUT") is { Length: > 0 } outPath) File.WriteAllBytes(outPath, pdf);
        var text = Text(pdf);
        Assert.Contains("EST-0001", text);
        Assert.Contains("LD-0001", text);
        Assert.Contains("16 Ravens Dene", text);
        Assert.Contains("142,500", text);
        Assert.Contains("150,000", text);
        Assert.Contains("Rear dormer", text);
        Assert.Contains("16,000", text);
        Assert.Contains("LOFT CONVERSION", text);
        Assert.Contains("internal estimate", text);
    }

    [Fact]
    public void AnUnpricedEstimate_saysSo_andStillRenders()
    {
        var pdf = EstimateDocumentRenderer.Render(new EstimateDocumentRenderer.Model(Estimate() with { Total = null, BudgetMentioned = null, Notes = "" }, Lead(), DateTimeOffset.UtcNow));
        var text = Text(pdf);
        Assert.Contains("Not yet priced", text);
        Assert.DoesNotContain("Notes and breakdown", text);
    }

    [Fact]
    public void TheFileName_isTheReferenceAndTheProperty()
    {
        Assert.Equal("EST-0001 - 16 Ravens Dene - estimate.pdf", EstimateDocumentRenderer.FileName(Estimate(), Lead()));
        Assert.Equal("EST-0001 - Julia Nagornaya - estimate.pdf", EstimateDocumentRenderer.FileName(Estimate(), Lead() with { PropertyAddress = "" }));
    }

    // PdfPig's page.Text runs words together; the words, spaced, are what a reader sees.
    private static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join("\n", document.GetPages().Select(page => string.Join(" ", page.GetWords().Select(word => word.Text))));
    }

    private static LeadEstimate Estimate() => new(
        "est-1", "lead-1", "EST-0001",
        "Loft dormer conversion and ground floor internal works only.",
        "RESI — Mina Ghabrial", null, 150000m, 142500m,
        "INDICATIVE BUDGET ESTIMATE\n\nLOFT CONVERSION — £89,500\n- Preliminaries, scaffold: £9,500\n- Rear dormer — framing, flat roof: £16,000\n\nTOTAL £142,500 ex VAT (range £135k–£155k).\n\nEXCLUDED: VAT at 20%.",
        EstimateStatus.Received, new DateTimeOffset(2026, 9, 15, 11, 50, 0, TimeSpan.Zero), null,
        "james.beadle@jewelbb.co.uk", new DateTimeOffset(2026, 9, 15, 11, 50, 0, TimeSpan.Zero));

    private static Lead Lead() => new(
        "lead-1", "LD-0001", "Julia Nagornaya", "jlnagornaya@icloud.com", "", "", LeadProspectKind.Homeowner,
        "16 Ravens Dene", "BR7 5FP", "Extension", "", LeadSource.Inbound, null, null,
        LeadStage.Engaged, DateTimeOffset.UtcNow, null, "james.beadle@jewelbb.co.uk", DateTimeOffset.UtcNow, null, null, null);
}
