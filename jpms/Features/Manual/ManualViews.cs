namespace Jewel.JPMS.Features.Manual;

/// <summary>The published views by name and address, in the order the tabs show them.</summary>
public static class ManualViews
{
    public static readonly IReadOnlyList<ManualView> All = new[]
    {
        ManualView.SiteManager, ManualView.HealthAndSafetyOfficer, ManualView.Foreman, ManualView.Office,
    };

    public static string Label(ManualView view) => view switch
    {
        ManualView.SiteManager => "Site Manager view",
        ManualView.HealthAndSafetyOfficer => "H&S Officer view",
        ManualView.Foreman => "Foreman quick view",
        _ => "Office master view",
    };

    public static string Address(ManualView view) => $"/manual/view/{view}";

    public static string ModuleAddress(string manualModuleId) => $"/manual/{manualModuleId}";

    public static string FormAddress(string slug) => $"/forms/{slug}";

    public static IReadOnlyList<TabItem> Tabs() =>
        All.Select(view => new TabItem(view.ToString(), Label(view), Address(view))).ToList();

    public static string StatusLabel(ManualModuleStatus status) => status switch
    {
        ManualModuleStatus.InReview => "In review",
        ManualModuleStatus.Superseded => "Retired",
        _ => status.ToString(),
    };
}
