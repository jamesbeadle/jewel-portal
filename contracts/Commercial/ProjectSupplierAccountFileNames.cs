namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The one name a supplier account goes out under — the same shape as ValuationReportFileNames,
/// for the same reason: project reference, project name, the supplier, the document, then the date
/// it was produced, and nothing else. "JBB-2026-001 - By France - S Williams Plumbing &amp; Heating -
/// Supplier account 2026-09-14.pdf" files itself in a downloads folder or an email thread.
/// </summary>
public static class ProjectSupplierAccountFileNames
{
    private const string DocumentTitle = "Supplier account";
    private const string DateFormat = "yyyy-MM-dd";
    private const string Separator = " - ";
    private const string UnsafeCharacters = "\\/:*?\"<>|";

    /// <summary>The full name, date included, without an extension.</summary>
    public static string For(string projectReference, string? projectName, string? supplierName, DateTimeOffset producedOn)
    {
        var parts = new[] { projectReference, projectName, supplierName, DocumentTitle }
            .Select(part => Safe(part ?? ""))
            .Where(part => part.Length > 0);
        return $"{string.Join(Separator, parts)} {producedOn.ToString(DateFormat)}";
    }

    private static string Safe(string part)
    {
        var kept = part.Where(character => !UnsafeCharacters.Contains(character) && !char.IsControl(character));
        return new string(kept.ToArray()).Trim();
    }
}
