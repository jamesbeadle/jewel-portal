using System.Net;
using System.Text;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>
/// The page a scan opens: one self-contained HTML document with inline styles and no script,
/// grouped by folder, each drawing a tap target that opens its shown revision in the phone's own
/// viewer. It links nowhere else — not back into the app, not to anything but its own files.
/// </summary>
public static class SiteDrawingsPageHtml
{
    private const string ApprovedBadge = "Approved";
    private const string UnapprovedBadge = "Not approved";

    public static string Render(SiteDrawingsPage page, string token)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.Append("<meta name=\"robots\" content=\"noindex, nofollow\">");
        html.Append("<title>").Append(Encode(page.LinkLabel)).Append(" · ").Append(Encode(page.ProjectName)).Append("</title>");
        html.Append("<style>").Append(SiteDrawingsPageStyles.Css).Append("</style></head><body>");
        AppendHeader(html, page);
        html.Append("<main>");
        AppendFolders(html, page, token);
        html.Append("</main></body></html>");
        return html.ToString();
    }

    private static void AppendHeader(StringBuilder html, SiteDrawingsPage page)
    {
        html.Append("<header><p class=\"brand\">Jewel Bespoke Build</p>");
        html.Append("<h1>").Append(Encode(page.ProjectName)).Append("</h1>");
        html.Append("<p class=\"label\">").Append(Encode(page.LinkLabel)).Append("</p>");
        html.Append("<p class=\"stamp\">Shown as at ").Append(Encode(page.ShownAtText)).Append("</p></header>");
    }

    private static void AppendFolders(StringBuilder html, SiteDrawingsPage page, string token)
    {
        var rows = page.Rows;
        if (!rows.Any())
        {
            html.Append("<p class=\"empty\">No drawings in this folder yet.</p>");
            return;
        }
        foreach (var folder in rows.GroupBy(row => row.FolderPath))
        {
            html.Append("<h2>").Append(Encode(folder.Key)).Append("</h2><ul>");
            foreach (var row in folder) AppendRow(html, row, token);
            html.Append("</ul>");
        }
    }

    private static void AppendRow(StringBuilder html, SiteDrawingRow row, string token)
    {
        var badgeClass = row.IsApproved ? "approved" : "unapproved";
        var badgeText = row.IsApproved ? ApprovedBadge : UnapprovedBadge;
        html.Append("<li><a href=\"").Append(FileHref(token, row.DrawingRevisionId)).Append("\">");
        html.Append("<span class=\"name\">").Append(Encode(row.Label)).Append("</span>");
        html.Append("<span class=\"meta\">").Append(Encode(row.RevisionText));
        html.Append(" · <b class=\"").Append(badgeClass).Append("\">").Append(badgeText).Append("</b>");
        html.Append(" · received ").Append(Encode(row.ReceivedText)).Append("</span></a></li>");
    }

    private static string FileHref(string token, string revisionId) =>
        Encode($"/api/site/{Uri.EscapeDataString(token)}/file/{Uri.EscapeDataString(revisionId)}?inline=1");

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
