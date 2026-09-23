namespace Jewel.JPMS.Models;

/// <summary>
/// Where a work order's acceptance link lands: the public page that shows the supplier their
/// purchase order and takes their electronic acceptance without a portal login (2026-09-23,
/// Nigel's ask). The token in the path is the whole authorisation — it went to one supplier in
/// one email, so whoever holds it is that supplier. The api builds the absolute link for the
/// email from here and the page answers the same route, so the two can never drift apart.
/// </summary>
public static class WorkOrderAcceptanceLink
{
    public const string Path = "/work-orders/accept";

    public static string PathFor(string token) => $"{Path}/{Uri.EscapeDataString(token)}";

    public static string On(string publicSiteUrl, string token) => publicSiteUrl.TrimEnd('/') + PathFor(token);
}
