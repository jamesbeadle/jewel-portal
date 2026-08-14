using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The bill's area sub-headings — "Electrics", "External", "Plumbing &amp; Heating" — so the
/// valuation report reads in the same titled areas as the estimate it was priced from instead
/// of one flat run of lines. One rule shared by every surface (live report table, snapshot
/// viewer, snapshot/working-copy PDF, both Excel exports), so the same line is always titled
/// the same way: the estimate section recorded on the line when there is one, otherwise the
/// name of the cost centre the line is allocated to. A line with neither carries no title and
/// simply continues the area above it. Variation lines are never area-grouped — they group by
/// their V-ref.
/// </summary>
public static class ValuationReportAreas
{
    /// <summary>
    /// The area this line belongs under. <paramref name="costCentreNameFor"/> resolves a cost
    /// code to its master name (return null when unknown — the raw code is shown rather than
    /// nothing, so a retired centre still titles its lines).
    /// </summary>
    public static string TitleFor(string sectionName, string costCode, Func<string, string?> costCentreNameFor)
    {
        if (!string.IsNullOrWhiteSpace(sectionName)) return sectionName.Trim();
        if (string.IsNullOrWhiteSpace(costCode)) return "";
        var name = costCentreNameFor(costCode);
        return string.IsNullOrWhiteSpace(name) ? costCode.Trim() : name!.Trim();
    }

    /// <summary>Element types whose lines group under area titles — everything except variations.</summary>
    public static bool GroupsByArea(ValuationElementType elementType) =>
        elementType != ValuationElementType.Variation;

    /// <summary>
    /// True when this line opens a new area run: its title is non-empty and differs from the
    /// run in progress. Lines render in display order (the estimate's own order), so areas are
    /// consecutive runs, never a re-sort — an untitled line continues the area above it.
    /// </summary>
    public static bool StartsNewArea(string title, string currentArea) =>
        !string.IsNullOrWhiteSpace(title)
        && !string.Equals(title, currentArea, StringComparison.OrdinalIgnoreCase);
}
