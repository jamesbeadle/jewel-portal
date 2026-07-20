namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The project workspace's three page groups — the sidebar renders them as headed blocks under
/// the project picker (in the day-to-day order: manage the job, deliver it, then the money), and
/// the project landing page renders one card per group. Configuration lives on the single
/// Project settings page (/projects/{id}/settings), not here.
/// </summary>
public enum ProjectSection
{
    ProjectManagement,
    Operations,
    Financials
}

public sealed record ProjectTabInfo(string Slug, string Label);

public sealed record ProjectSectionInfo(
    ProjectSection Section,
    string Label,
    string IconKey,
    string Blurb,
    IReadOnlyList<ProjectTabInfo> Tabs)
{
    public ProjectTabInfo FirstTab => Tabs[0];
}

public static class ProjectSections
{
    // Page slugs are unchanged from the old tab bar so existing links and bookmarks keep working.
    public static readonly IReadOnlyList<ProjectSectionInfo> All = new[]
    {
        new ProjectSectionInfo(
            ProjectSection.ProjectManagement,
            "Project Management",
            "#project-management",
            "The day-to-day running of the job — to-dos, requests, drawings, the programme and progress reporting.",
            new[]
            {
                new ProjectTabInfo("todos",          "To-do"),
                // Requests is the document register: Requests, RFIs and Variations (VOQ & VO) are
                // one lifecycle in one place — the old separate Variations tab redirects into it.
                new ProjectTabInfo("requests",       "Requests"),
                new ProjectTabInfo("drawings",       "Drawings"),
                new ProjectTabInfo("programme",      "Programme"),
                new ProjectTabInfo("progress",       "Progress"),
                new ProjectTabInfo("communications", "Communications")
            }),
        new ProjectSectionInfo(
            ProjectSection.Operations,
            "Operations",
            "#operations",
            "Getting the work delivered — labour on site, bid packages, work orders and their invoice allocation.",
            new[]
            {
                new ProjectTabInfo("labour",                "Labour"),
                new ProjectTabInfo("bid-package-invites",   "Bid Package Invites"),
                new ProjectTabInfo("work-orders",           "Work Orders"),
                new ProjectTabInfo("work-order-allocation", "WO Allocation")
            }),
        new ProjectSectionInfo(
            ProjectSection.Financials,
            "Financials",
            "#financials",
            "Cost centres, cashflow and the valuation report — what the project has earned, spent and retained.",
            new[]
            {
                new ProjectTabInfo("financials", "Financials"),
                new ProjectTabInfo("cashflow",   "Cashflow"),
                new ProjectTabInfo("valuation",  "Valuation Report")
            })
    };

    public static string HrefFor(string projectId, ProjectTabInfo tab) =>
        $"/projects/{projectId}/{tab.Slug}";

    public static string FirstTabHref(string projectId, ProjectSectionInfo section) =>
        HrefFor(projectId, section.FirstTab);
}
