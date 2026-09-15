using Jewel.JPMS.Api.Features.Sales.Documents;
using Jewel.JPMS.Models;
using UglyToad.PdfPig;
using Xunit;

namespace Jewel.JPMS.Tests;

// The estimate document (2026-09-15, the tender's shape): renders from the record alone, names
// the file after the property, and prints every part — cover, project page, executive summary
// with build time and exclusions, the breakdown chart, one table per section with its total, the
// total project estimate value, and the contact page. The internal notes never print.
public sealed class EstimateDocumentRendererTests
{
    static EstimateDocumentRendererTests()
    {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fonts");
        if (Directory.Exists(fonts))
            Environment.SetEnvironmentVariable("RequestDocuments__FontPath", fonts);
    }

    [Fact]
    public void RendersEveryPart_inTheTendersShape()
    {
        var pdf = EstimateDocumentRenderer.Render(new EstimateDocumentRenderer.Model(Estimate(), Lead(), new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)));

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        if (Environment.GetEnvironmentVariable("ESTIMATE_PDF_OUT") is { Length: > 0 } outPath) File.WriteAllBytes(outPath, pdf);
        using var document = PdfDocument.Open(pdf);
        var pages = document.GetPages().Select(page => string.Join(" ", page.GetWords().Select(word => word.Text))).ToList();
        var text = string.Join("\n", pages);

        // Cover, then the project page with Jewel's own block.
        Assert.True(pages.Count >= 5, $"expected cover, project, summary, chart, sections and contact pages — got {pages.Count}");
        Assert.Contains("ESTIMATE FOR PROJECT", pages[0]);
        Assert.Contains("Estimate for:", pages[1]);
        Assert.Contains("Julia Nagornaya", pages[1]);
        Assert.Contains("RESI", pages[1]);
        Assert.Contains("Argent House", pages[1]);

        // The narrative and the figure.
        Assert.Contains("Executive summary", text);
        Assert.Contains("rear dormer", text);
        Assert.Contains("Build time", text);
        Assert.Contains("12–14 weeks", text);
        Assert.Contains("Exclusions", text);
        Assert.Contains("VAT", text);
        Assert.Contains("£58,500", text);

        // The sections, their lines, the section totals in pence, and the provisional flag.
        Assert.Contains("Project breakdown", text);
        Assert.Contains("Preliminaries & preambles", text);
        Assert.Contains("Structural steelwork", text);
        Assert.Contains("Provisional allowance", text);
        Assert.Contains("PRELIMS-SMG", text);
        Assert.Contains("18,480.00", text);   // 420 m² × £44.00
        Assert.Contains("38,480.00", text);   // Preliminaries total
        Assert.Contains("Total project estimate value", text);
        Assert.Contains("58,500.00", text);
        Assert.Contains("Contact us", text);

        // The internal notes stay internal.
        Assert.DoesNotContain("INTERNAL NOTE", text);
    }

    [Fact]
    public void WithoutABreakdown_theDocumentStillStands_withoutTheChartOrTables()
    {
        var pdf = EstimateDocumentRenderer.Render(new EstimateDocumentRenderer.Model(Estimate() with { Lines = null, Total = null }, Lead(), DateTimeOffset.UtcNow));
        using var document = PdfDocument.Open(pdf);
        var text = string.Join("\n", document.GetPages().Select(page => string.Join(" ", page.GetWords().Select(word => word.Text))));
        Assert.Contains("ESTIMATE FOR PROJECT", text);
        Assert.Contains("Executive summary", text);
        Assert.DoesNotContain("Project breakdown", text);
        Assert.DoesNotContain("Total project estimate value", text);
        Assert.Contains("Contact us", text);
    }

    [Fact]
    public void TheFileName_isTheReferenceAndTheProperty()
    {
        Assert.Equal("EST-0001 - 16 Ravens Dene - estimate.pdf", EstimateDocumentRenderer.FileName(Estimate(), Lead()));
        Assert.Equal("EST-0001 - Julia Nagornaya - estimate.pdf", EstimateDocumentRenderer.FileName(Estimate(), Lead() with { PropertyAddress = "" }));
    }

    [Fact]
    public void TheSections_groupTheLinesInPrintOrder_andTotalThem()
    {
        var sections = Estimate().Sections;
        Assert.Equal(new[] { "Preliminaries & preambles", "Structural steelwork", "Drainage" }, sections.Select(section => section.Name));
        Assert.Equal(38_480m, sections[0].Total);
        Assert.True(sections[2].Provisional);
        Assert.Equal(58_500m, sections.Sum(section => section.Total));
    }

    private static LeadEstimate Estimate()
    {
        var lines = new List<EstimateLine>
        {
            new("l1", "est-1", "Preliminaries & preambles", 0, false, "SCAFF-STD", "Scaffolding including temporary roof", 420m, "m²", 44m, 18_480m, 0),
            new("l2", "est-1", "Preliminaries & preambles", 0, false, "PRELIMS-SMG", "Site manager", 20m, "week", 1_000m, 20_000m, 1),
            new("l3", "est-1", "Structural steelwork", 1, false, "STR-STL", "203 x 203 x 46 kg steel beam - B1", 800m, "kg", 7m, 5_600m, 0),
            new("l4", "est-1", "Structural steelwork", 1, false, "STR-STL", "Cut out & cast concrete padstones", 12m, "nr", 95m, 1_140m, 1),
            new("l5", "est-1", "Structural steelwork", 1, false, "", "Fireline protection to steels", 1m, "item", 1_200m, 1_200m, 2),
            new("l6", "est-1", "Drainage", 2, true, "SUB-DRN", "Excavate & lay new underground drainage runs", 1m, "item", 5_000m, 5_000m, 0),
            new("l7", "est-1", "Drainage", 2, true, "SUB-DRN", "New inspection chambers", 3m, "nr", 645m, 1_935m, 1),
            new("l8", "est-1", "Drainage", 2, true, "SUB-DRN", "Aco slot drains", 20m, "m", 155m, 3_100m, 2),
            new("l9", "est-1", "Drainage", 2, true, "", "Make good damaged areas", 1m, "item", 2_045m, 2_045m, 3),
        };
        return new LeadEstimate(
            "est-1", "lead-1", "EST-0001",
            "Loft dormer conversion and ground floor internal works only.",
            "RESI — Mina Ghabrial", null, 150_000m, 58_500m,
            "INTERNAL NOTE: priced on benchmark rates.",
            EstimateStatus.Pricing, new DateTimeOffset(2026, 9, 15, 11, 50, 0, TimeSpan.Zero), null,
            "james.beadle@jewelbb.co.uk", new DateTimeOffset(2026, 9, 15, 11, 50, 0, TimeSpan.Zero),
            ExecutiveSummary: "We understand the project to be a rear dormer loft conversion with two bedrooms and an en-suite, and the ground floor garage converted.\n\n- Loft: rear dormer, new staircase, eleven roof windows.\n- Ground floor: garage conversion, utility and WC.",
            BuildTime: "12–14 weeks on site from a December 2026 start, completing late March 2027.",
            Exclusions: "VAT at 20%; kitchen units and appliances; structural engineer's design.",
            Lines: lines);
    }

    private static Lead Lead() => new(
        "lead-1", "LD-0001", "Julia Nagornaya", "jlnagornaya@icloud.com", "", "", LeadProspectKind.Homeowner,
        "16 Ravens Dene", "BR7 5FP", "Extension", "", LeadSource.Inbound, null, null,
        LeadStage.Engaged, DateTimeOffset.UtcNow, null, "james.beadle@jewelbb.co.uk", DateTimeOffset.UtcNow, null, null, null);
}
