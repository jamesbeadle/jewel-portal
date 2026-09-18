using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Xunit;

namespace Jewel.JPMS.Tests;

// Two sanitising rules, because a body a person typed and a body the portal wrote are not the same
// risk (Nigel's decision, 2026-09-18). The one-line version of this — run every outbound body
// through the typed rule inside the dispatcher — would have stripped the branding off every RFI,
// purchase order and statement the portal sends, because that rule allows exactly one CSS property.
// These are the cases that say so.
public sealed class ComposeHtmlPipelineTests
{
    private const string CoverNote =
        "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5\">"
        + "<p style=\"margin:0 0 12px\">Please find attached <strong>RFI-052</strong>.</p>"
        + "<p style=\"margin:16px 0 0;color:#C09A51;font-weight:bold\">Jewel Bespoke Build</p></div>";

    private const string LinesTable =
        "<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">"
        + "<tr><th align=\"left\">Item</th></tr><tr><td align=\"right\">£1,250.00</td></tr></table>";

    [Fact]
    public void ThePortalsOwnMail_keepsTheStylingItIsBrandedWith()
    {
        var cleaned = ComposeHtmlPipeline.FromPortalDocument(CoverNote);

        Assert.Contains("font-family", cleaned);
        Assert.Contains("font-size", cleaned);
        Assert.Contains("line-height", cleaned);
        Assert.Contains("margin", cleaned);
        Assert.Contains("font-weight", cleaned);
        Assert.Contains("#C09A51", cleaned);
        Assert.Contains("Jewel Bespoke Build", cleaned);
    }

    [Fact]
    public void APurchaseOrderTable_keepsTheLayoutEmailClientsNeed()
    {
        var cleaned = ComposeHtmlPipeline.FromPortalDocument(LinesTable);

        Assert.Contains("cellpadding", cleaned);
        Assert.Contains("border-collapse", cleaned);
        Assert.Contains("align", cleaned);
        Assert.Contains("£1,250.00", cleaned);
    }

    [Theory]
    [InlineData("<p>Hello</p><script>steal()</script>", "script")]
    [InlineData("<p onclick=\"steal()\">Hello</p>", "onclick")]
    [InlineData("<a href=\"javascript:steal()\">Hello</a>", "javascript:")]
    [InlineData("<p style=\"font-family:Arial;behavior:url(x.htc)\">Hello</p>", "behavior")]
    public void ThePortalsOwnMail_stillLosesAnythingThatCouldRun(string body, string forbidden)
    {
        var cleaned = ComposeHtmlPipeline.FromPortalDocument(body);

        Assert.DoesNotContain(forbidden, cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello", cleaned);
    }

    [Fact]
    public void ATypedBody_keepsItsColourAndLosesEverythingElseAboutHowItLooks()
    {
        var typed = new ComposeHtmlPipeline().FromTypedHtml(
            "<p style=\"color:#C09A51;font-family:Comic Sans MS;font-size:48px\">Shouting</p>");

        Assert.Contains("color", typed.Html);
        Assert.DoesNotContain("font-family", typed.Html);
        Assert.DoesNotContain("font-size", typed.Html);
        Assert.Contains("Shouting", typed.Html);
    }

    [Fact]
    public void AnEmptyBody_isLeftAsItArrived_soNoCoverNoteStaysNoCoverNote()
    {
        Assert.Equal("", ComposeHtmlPipeline.FromPortalDocument(""));
        Assert.Equal("", ComposeHtmlPipeline.FromPortalDocument(null));
        Assert.Equal("   ", ComposeHtmlPipeline.FromPortalDocument("   "));
    }
}
