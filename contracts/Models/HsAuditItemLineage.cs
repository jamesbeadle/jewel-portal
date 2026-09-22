namespace Jewel.JPMS.Models;

/// <summary>
/// Where an item of the 27 Aug 2026 framework lives on the 15 Sep one, so a repeat finding and
/// a score trend line up across versions. Only sections 2, 8 (from 8.07), 9 and 10 renumbered;
/// every other code is the same item on both. An old code that folded into a broader item maps
/// to that item; one with no successor (Lorries & Trailers) maps to nothing.
/// </summary>
public static class HsAuditItemLineage
{
    private static readonly IReadOnlyDictionary<string, string> SuccessorOfFirstVersionCode = new Dictionary<string, string>
    {
        ["2.01"] = "2.01", ["2.02"] = "2.01", ["2.03"] = "2.01", ["2.04"] = "2.01", ["2.05"] = "2.01", ["2.06"] = "2.01",
        ["8.07"] = "8.07", ["8.08"] = "8.07", ["8.09"] = "8.08", ["8.10"] = "8.09",
        ["9.01"] = "9.01", ["9.02"] = "9.02", ["9.03"] = "9.03", ["9.04"] = "9.04", ["9.05"] = "9.01",
        ["9.06"] = "9.04", ["9.07"] = "9.04", ["9.08"] = "9.01", ["9.09"] = "9.03",
        ["10.02"] = "10.02", ["10.03"] = "10.02", ["10.04"] = "10.02", ["10.05"] = "10.02", ["10.06"] = "10.02",
        ["10.07"] = "10.02", ["10.08"] = "10.03", ["10.09"] = "10.04", ["10.10"] = "10.04",
        ["10.11"] = "10.05", ["10.12"] = "10.06",
    };

    private static readonly IReadOnlySet<string> DroppedFirstVersionCodes = new HashSet<string> { "8.11" };

    /// <summary>The current code for an item of any version, or null when the item was dropped.</summary>
    public static string? CurrentCodeFor(string templateVersion, string code)
    {
        if (templateVersion != HsAuditTemplate.FirstVersion) return code;
        if (DroppedFirstVersionCodes.Contains(code)) return null;
        return SuccessorOfFirstVersionCode.TryGetValue(code, out var successor) ? successor : code;
    }
}
