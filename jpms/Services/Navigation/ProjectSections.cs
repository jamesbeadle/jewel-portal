namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The project view's three sections. The side nav is the primary way between sections (its
/// Financials / Project Management / Operations groups target the last-viewed project), so the
/// project view renders only the ACTIVE section's tab row — plus a landing page (one card per
/// section) at the bare project URL. Each section carries its own Setup tab so configuration
/// lives beside the thing it configures: retention/valuation dates with Financials, project
/// details/stage with Project Management, and the correspondence profile with Operations.
/// </summary>
public enum ProjectSection
{
    Financials,
    ProjectManagement,
    Operations
}

/// <summary>A tab in a section. AbsoluteHref overrides the /projects/{id}/{slug} pattern for
/// tabs that live outside the project scope (the All-projects financial overview).</summary>
public sealed record ProjectTabInfo(string Slug, string Label, string? AbsoluteHref = null);

public sealed record ProjectSectionInfo(
    ProjectSection Section,
    string Label,
    string Blurb,
    IReadOnlyList<ProjectTabInfo> Tabs)
{
    /// <summary>The first tab that belongs to THIS project — landing-page cards open here, so the
    /// Financials card opens the project's own Financials even though the cross-project Summary
    /// leads its tab row.</summary>
    public ProjectTabInfo FirstProjectTab => Tabs.First(tab => tab.AbsoluteHref is null);
}

public static class ProjectSections
{
    // Tab slugs are unchanged from the old flat bar so existing links and bookmarks keep working.
    public static readonly IReadOnlyList<ProjectSectionInfo> All = new[]
    {
        new ProjectSectionInfo(
            ProjectSection.Financials,
            "Financials",
            "Cost centres, cashflow and the valuation report — what the project has earned, spent and retained.",
            new[]
            {
                // The company-wide summary leads — one row per active project plus the total.
                new ProjectTabInfo("all-projects",     "Summary", "/finance"),
                new ProjectTabInfo("financials",       "Financials"),
                new ProjectTabInfo("cashflow",         "Cashflow"),
                new ProjectTabInfo("valuation",        "Valuation Report"),
                new ProjectTabInfo("financials-setup", "Setup")
            }),
        new ProjectSectionInfo(
            ProjectSection.ProjectManagement,
            "Project Management",
            "The day-to-day running of the job — to-dos, requests, drawings, the programme and progress reporting.",
            new[]
            {
                new ProjectTabInfo("todos",          "To-do"),
                // Requests is the document register: Requests, RFIs and Variations (VOQ & VO) are
                // one lifecycle in one tab — the old separate Variations tab redirects into it.
                new ProjectTabInfo("requests",       "Requests"),
                new ProjectTabInfo("drawings",       "Drawings"),
                new ProjectTabInfo("programme",      "Programme"),
                new ProjectTabInfo("progress",       "Progress"),
                new ProjectTabInfo("communications", "Communications"),
                new ProjectTabInfo("setup",          "Setup")
            }),
        new ProjectSectionInfo(
            ProjectSection.Operations,
            "Operations",
            "Getting the work delivered — labour on site, bid packages, work orders and their invoice allocation.",
            new[]
            {
                new ProjectTabInfo("labour",                "Labour"),
                new ProjectTabInfo("bid-package-invites",   "Bid Package Invites"),
                new ProjectTabInfo("work-orders",           "Work Orders"),
                new ProjectTabInfo("work-order-allocation", "WO Allocation"),
                new ProjectTabInfo("operations-setup",      "Setup")
            })
    };

    /// <summary>The section that owns a tab slug, or null for unknown slugs (e.g. the landing page's empty tab).</summary>
    public static ProjectSectionInfo? SectionForTab(string tabSlug) =>
        All.FirstOrDefault(section => section.Tabs.Any(tab => tab.Slug == tabSlug));

    public static string HrefFor(string projectId, ProjectTabInfo tab) =>
        tab.AbsoluteHref ?? $"/projects/{projectId}/{tab.Slug}";

    public static string FirstTabHref(string projectId, ProjectSectionInfo section) =>
        HrefFor(projectId, section.FirstProjectTab);
}
