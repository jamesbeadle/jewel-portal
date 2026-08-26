namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// One display spelling for variation references on exported documents: "V" plus the number
/// padded to two digits — V2 → V02, V28 → V28, V123 keeps its three. Refs were minted over time
/// in mixed spellings (V2, V04, V8); the register keeps what was minted (persisted identifiers
/// are never rewritten — the CLAUDE.md rule), and every export formats through here instead, so
/// the workbook tabs, statement rows and PDF read uniformly (accountant 2026-08-26).
/// </summary>
public static class VariationRefs
{
    /// <summary>A ref with no digits (or blank) comes back trimmed but otherwise as it arrived.</summary>
    public static string Padded(string variationRef)
    {
        var trimmed = variationRef.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? Padded(number) : trimmed;
    }

    public static string Padded(int number) => number > 0 ? $"V{number:00}" : "";
}
