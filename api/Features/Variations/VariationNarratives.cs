namespace Jewel.JPMS.Api.Features.Variations;

/// <summary>
/// The one shared cleaning rule for a variation order's narrative sections (commercial basis,
/// programme impact, exclusions): trim, treat blank as null (an empty section is an absent one),
/// and clamp to the entity's 4000-character allowance. Every route that writes these fields —
/// creation and the narratives update — goes through here, so a wording that one route accepts
/// can never be refused by another.
/// </summary>
internal static class VariationNarratives
{
    public const int MaxNarrativeChars = 4000;

    public static string? Clean(string? narrative)
    {
        var cleaned = (narrative ?? "").Trim();
        if (cleaned.Length == 0) return null;
        if (cleaned.Length > MaxNarrativeChars) cleaned = cleaned[..MaxNarrativeChars];
        return cleaned;
    }
}
