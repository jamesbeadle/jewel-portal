using System.Globalization;
using System.Net;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// The look every forms email shares, carried from the dashboard: Jewel Bespoke Build's name as a
/// heading, the body, and its registered particulars at the foot. The email a stranger receives
/// asking for a passport photo or an NI number has to look like it came from a real company and
/// has to give them a way to check — hence the trading name, who sent it, and the office's number.
/// </summary>
internal static class FormEmailFrame
{
    private const string Accent = "#101111";
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    public static string FootLine { get; } =
        string.Join(" · ", new[] { JewelBespokeBuild.LegalName, JewelBespokeBuild.RegisteredOffice, JewelBespokeBuild.Phone });

    public static string Wrap(string body) =>
        "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:15px;color:#2F2F2F;line-height:1.55;max-width:560px\">"
        + $"<p style=\"font-weight:700;color:{Accent};letter-spacing:.4px;margin:0 0 18px\">{Encode(JewelBespokeBuild.ShortName.ToUpperInvariant())}</p>"
        + body
        + $"<p style=\"margin:14px 0 0;font-size:12px;color:#9AA3AF\">{Encode(FootLine)}</p></div>";

    public static string Paragraph(string html) => $"<p style=\"margin:0 0 14px\">{html}</p>";

    public static string SmallPrint(string html) =>
        $"<p style=\"margin:16px 0 0;padding-top:14px;border-top:1px solid #DDE1E6;font-size:13px;color:#6B7280\">{html}</p>";

    public static string Button(string link, string label) =>
        $"<p style=\"margin:18px 0\"><a href=\"{Encode(link)}\" style=\"display:inline-block;padding:13px 22px;background:{Accent};"
        + $"color:#ffffff;border-radius:8px;text-decoration:none;font-weight:700\">{Encode(label)}</a></p>"
        + $"<p style=\"margin:0 0 14px;font-size:13px;color:#6B7280\">Or paste this into your browser:<br>{Encode(link)}</p>";

    public static string Hello(string personName) => $"Hello {Encode(FirstName(personName))}";

    public static string FirstName(string personName)
    {
        var first = personName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first ?? "there";
    }

    public static string Day(DateTimeOffset moment) => FormClock.InLondon(moment).ToString("dddd d MMMM", British);

    public static string Day(DateOnly date) => date.ToString("d MMMM yyyy", British);

    public static string Encode(string text) => WebUtility.HtmlEncode(text);
}
