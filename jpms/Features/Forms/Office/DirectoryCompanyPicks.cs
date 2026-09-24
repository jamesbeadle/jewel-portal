namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// The directory companies a form can be filed to, as a picker lists them, and the one whose name
/// the form gave, as the picker's first guess. A person confirms the pick; nothing is filed to a
/// company picked by a guess.
/// </summary>
public static class DirectoryCompanyPicks
{
    public static IReadOnlyList<SearchSelect.Option> Options(ISubcontractorStore directory) => directory.All()
        .OrderBy(company => company.CompanyName)
        .Select(company => new SearchSelect.Option(company.SubcontractorId, company.CompanyName))
        .ToList();

    public static string NamedBy(ISubcontractorStore directory, string formCompany)
    {
        var named = formCompany.Trim();
        var match = directory.All()
            .FirstOrDefault(company => string.Equals(company.CompanyName.Trim(), named, StringComparison.OrdinalIgnoreCase));
        return match?.SubcontractorId ?? "";
    }
}
