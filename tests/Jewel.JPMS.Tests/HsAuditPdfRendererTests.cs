using Jewel.JPMS.Api.Features.Hs.Audits.Documents;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using UglyToad.PdfPig;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The officer's inspection report as she sends it on: the front sheet, the score in its band,
/// every section in order with only the rows she wrote on, and the two declarations — read back
/// from the rendered PDF in the host's face.
/// </summary>
public sealed class HsAuditPdfRendererTests
{
    static HsAuditPdfRendererTests()
    {
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fonts");
        if (Directory.Exists(fonts))
            Environment.SetEnvironmentVariable("RequestDocuments__FontPath", fonts);
    }

    [Fact]
    public void RendersTheFrontSheet_theSections_andTheDeclarations()
    {
        var pdf = HsAuditPdfRenderer.Render(View(), "By France", new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));

        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        using var document = PdfDocument.Open(pdf);
        var text = string.Join("\n", document.GetPages().Select(page => string.Join(" ", page.GetWords().Select(word => word.Text))));

        Assert.Contains("H&S Inspection Report", text);
        Assert.Contains("By France", text);
        Assert.Contains("HSA-0001", text);
        Assert.Contains("Katy-Louise Hicks", text);
        Assert.Contains("James Everitt", text);
        Assert.Contains("84% · Fair", text);
        Assert.Contains("1 Documentation", text);
        Assert.Contains("Toolbox Talks", text);
        Assert.Contains("one needs to be done", text);
        Assert.Contains("11 Offices and Welfare", text);
        Assert.Contains("Nothing recorded in this section", text);
        Assert.DoesNotContain("Lifting Plans", text);
        Assert.Contains("Safety officer declaration", text);
        Assert.Contains("22 Sep 2026", text);
        Assert.Contains("Not yet signed", text);
    }

    [Fact]
    public void FileName_readsSiteReferenceAndDate() =>
        Assert.Equal("By-France-HSA-0001-HS-inspection-2026-09-01.pdf", HsAuditFileNames.Pdf(View().Audit, "By France"));

    private static HsAuditView View()
    {
        var audit = new HsAudit("audit", "3490f944b29545c4b8d5a04130f42ab8", 1, "HSA-0001", HsAuditStatus.Issued, HsAuditType.Routine,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), "James Everitt", "Katy-Louise Hicks", "", 6,
            "A few things that need to be rectified but nothing too serious.", 0.8428m, null, HsAuditTemplate.Version, "",
            new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero), null, "katy-louise.hicks@jewelbb.co.uk", DateTimeOffset.UtcNow);
        var items = new[]
        {
            Item("1.01", 1, "Health & Safety Plan", HsAuditRate.UpToDate, HsAuditClass.E, "Up to date", ""),
            Item("1.07", 1, "Lifting Plans", null, null, "", ""),
            Item("1.14", 1, "Toolbox Talks", HsAuditRate.OneWeekOutOfDate, HsAuditClass.D, "No recent - one needs to be done in September", "James Everitt"),
            Item("11.02", 11, "Toilets & Washing Facilities", null, null, "", "")
        };
        return new HsAuditView(audit, items);
    }

    private static HsAuditItem Item(string code, int section, string name, HsAuditRate? rate, HsAuditClass? hsAuditClass, string findings, string owner) =>
        new(code, "audit", code, section, name, null, rate, hsAuditClass, 0, null, findings, owner, null, null);
}
