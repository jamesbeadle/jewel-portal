namespace Jewel.JPMS.Features.Directory;

/// <summary>How the directory's records read — the category label, a company's location, the
/// dash for a blank — shared by the page, its tables, its export and its consolidation.</summary>
public static class DirectoryDisplay
{
    public static readonly DirectoryCategory[] AllCategories =
        (DirectoryCategory[])Enum.GetValues(typeof(DirectoryCategory));

    public static string Label(DirectoryCategory category) => category switch
    {
        DirectoryCategory.Subcontractor => "Subcontractor",
        DirectoryCategory.Client => "Client",
        DirectoryCategory.Architect => "Architect",
        DirectoryCategory.Supplier => "Supplier",
        _ => "Other"
    };

    public static string Location(Subcontractor company) =>
        string.Join(", ", new[] { company.Town, company.County }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
