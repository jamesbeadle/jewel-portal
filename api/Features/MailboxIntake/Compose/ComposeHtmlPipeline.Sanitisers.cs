using Ganss.Xss;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class ComposeHtmlPipeline
{
    /// <summary>The structure any outbound mail may carry. Both rules start here; they differ on
    /// what a tag is allowed to say about how it looks.</summary>
    private static readonly string[] MailTags =
    {
        "p", "div", "br", "b", "strong", "i", "em", "u", "s",
        "ul", "ol", "li", "a", "blockquote", "span", "pre", "code", "img",
        "h1", "h2", "h3", "h4", "table", "thead", "tbody", "tfoot", "tr", "th", "td", "hr"
    };

    /// <summary>What the portal's own mail is styled with, read off the bodies it composes rather
    /// than guessed: the request cover note (font-family, font-size, line-height, margin,
    /// font-weight, color), the purchase-order table (border-collapse), and the Azure
    /// Communication Services templates' call-to-action button (display, padding, background,
    /// border-radius, text-decoration, max-width, white-space).</summary>
    private static readonly string[] MailStyleProperties =
    {
        "background", "background-color", "border", "border-collapse", "border-radius",
        "color", "display", "font-family", "font-size", "font-style", "font-weight",
        "line-height", "margin", "max-width", "padding", "text-align", "text-decoration",
        "vertical-align", "white-space", "width"
    };

    /// <summary>The layout attributes a mail table still uses, because email clients have never
    /// agreed on CSS for tables.</summary>
    private static readonly string[] MailLayoutAttributes =
    {
        "align", "bgcolor", "border", "cellpadding", "cellspacing",
        "colspan", "height", "rowspan", "target", "title", "valign", "width"
    };

    /// <summary>A body a person typed. Style pass-through is a single property — color, for the
    /// composer's text-colour button — because a style attribute is the classic sanitiser escape
    /// hatch and a typed body needs nothing else.</summary>
    private static HtmlSanitizer TypedBodySanitiser()
    {
        var sanitiser = MailSanitiser();
        sanitiser.AllowedCssProperties.Add("color");
        return sanitiser;
    }

    /// <summary>A body this codebase composed. It keeps the inline styles and table layout branded
    /// mail lives by; scripts, event handlers, and every scheme but the four below are still gone,
    /// which is the part that matters when the body reaches a client's inbox.</summary>
    private static HtmlSanitizer PortalDocumentSanitiser()
    {
        var sanitiser = MailSanitiser();
        foreach (var property in MailStyleProperties) sanitiser.AllowedCssProperties.Add(property);
        foreach (var attribute in MailLayoutAttributes) sanitiser.AllowedAttributes.Add(attribute);
        return sanitiser;
    }

    private static HtmlSanitizer MailSanitiser()
    {
        var sanitiser = new HtmlSanitizer();
        sanitiser.AllowedTags.Clear();
        foreach (var tag in MailTags) sanitiser.AllowedTags.Add(tag);

        sanitiser.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "href", "src", "alt", "style" })
            sanitiser.AllowedAttributes.Add(attribute);

        sanitiser.AllowedCssProperties.Clear();
        sanitiser.AllowedAtRules.Clear();

        sanitiser.AllowedSchemes.Clear();
        foreach (var scheme in new[] { "http", "https", "mailto", "data", "cid" })
            sanitiser.AllowedSchemes.Add(scheme);
        return sanitiser;
    }
}
