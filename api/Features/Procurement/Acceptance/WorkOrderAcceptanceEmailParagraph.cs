namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// The paragraph in the purchase-order email that carries the acceptance link. The body is
/// composed client-side (WorkOrderPoEmail) before the token exists, so the api adds this on the
/// way out — every door that emails an order carries it, and none of them ever sees the token.
/// It goes in above the sign-off when the composed body has one, else at the end.
/// </summary>
public static class WorkOrderAcceptanceEmailParagraph
{
    private const string SignOffOpening = "<p>Kind regards,";

    public static string InsertInto(string htmlBody, string link)
    {
        var paragraph = Html(link);
        var signOffAt = htmlBody.IndexOf(SignOffOpening, StringComparison.OrdinalIgnoreCase);
        if (signOffAt < 0) return htmlBody + Environment.NewLine + paragraph;
        return htmlBody[..signOffAt] + paragraph + Environment.NewLine + htmlBody[signOffAt..];
    }

    private static string Html(string link)
    {
        var href = System.Net.WebUtility.HtmlEncode(link);
        return $"<p><strong>Please accept this work order online:</strong> <a href=\"{href}\">{href}</a><br/>"
            + "The link opens your purchase order and records your acceptance with your name and the date — "
            + "no login is needed. It is personal to your company, so please don't forward it.</p>";
    }
}
