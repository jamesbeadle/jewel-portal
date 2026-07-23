using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar's five folders (docs/Pathway-Split-Platform-Flow-Plan.md §6) — the successor to
/// the old three workspace blocks + flat Company list. Grouping follows how the business thinks:
/// who the correspondence is with (Client, Subcontractor, Internal), then the job (Project),
/// then the money (Financials); the people directory lives under Internal. Folders mix scopes
/// deliberately — project-scoped rows ("/projects/{project}/…" templates) and company rows sit
/// side by side where the work does (e.g. Triage and Workers live with Labour under Internal).
/// </summary>
public enum SidebarFolder
{
    Client,
    Subcontractor,
    Internal,
    Project,
    Financials
}

/// <summary>One sidebar row: a destination plus the roles that may see it. Per-row gates
/// reproduce the visibility each page had before the folder regrouping — grouping widens
/// nothing; administrators bypass every gate (DesktopNavigation.CanSee).</summary>
public sealed record SidebarRow(NavigationItem Item, IReadOnlyList<Role> VisibleTo);

/// <summary>A folder in the catalog: a collapsible header in the sidebar, one icon in the
/// collapsed rail, one destination card on the role landing page. The IconKey is a "#…" group
/// key resolved by NavIcon — never navigated to.</summary>
public sealed record SidebarFolderInfo(
    SidebarFolder Folder,
    string Label,
    string IconKey,
    IReadOnlyList<SidebarRow> Rows);

public static class SidebarFolders
{
    // Page slugs are unchanged from the old blocks so existing links and bookmarks keep working
    // (the standing convention: labels move, slugs don't). Role sets live on DesktopNavigation —
    // the single home for nav RBAC — so the catalog and the gates can never drift apart.
    public static readonly IReadOnlyList<SidebarFolderInfo> All = new[]
    {
        // ---- Client: correspondence and money as the client sees it. First folder, so its
        // first row (Requests) is the bare-project-URL landing for full-access roles. ----
        new SidebarFolderInfo(
            SidebarFolder.Client,
            "Client",
            "#client",
            new[]
            {
                // Requests is the client-side document register: Requests, RFIs and Variations
                // (VOQ & VO) are one lifecycle in one place.
                new SidebarRow(new NavigationItem("Requests", "/projects/{project}/requests"),
                    DesktopNavigation.ProjectRoles),
                // Point-in-time captures of issued valuation reports — what the client was
                // actually sent, frozen. New page; finance-gated like the live report.
                new SidebarRow(new NavigationItem("Valuation Report Snapshots", "/projects/{project}/valuation-snapshots"),
                    DesktopNavigation.FinanceRoles)
            }),

        // ---- Subcontractor: getting the work bought and delivered, in lifecycle order —
        // invite bids, place work orders, allocate their invoices. ----
        new SidebarFolderInfo(
            SidebarFolder.Subcontractor,
            "Subcontractor",
            "#subcontractor",
            new[]
            {
                new SidebarRow(new NavigationItem("Bid Package Invites", "/projects/{project}/bid-package-invites"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Work Orders", "/projects/{project}/work-orders"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("WO Allocation", "/projects/{project}/work-order-allocation"),
                    DesktopNavigation.ProjectRoles)
            }),

        // ---- Internal: the company's own machinery — the triage router, the master to-do
        // list, labour on site and the worker registry, the audit register, and the people
        // directory. ----
        new SidebarFolderInfo(
            SidebarFolder.Internal,
            "Internal",
            "#internal",
            new[]
            {
                // The mailbox intake queue — the router for ALL correspondence, not internal
                // mail only; it sits here because triaging is internal work. Mirrors the API's
                // TriageRoles gate.
                new SidebarRow(new NavigationItem("Triage", "/requests/triage"),
                    DesktopNavigation.TriageRoles),
                // The master to-do list: all projects plus company-wide items, with a project
                // filter (revived page).
                new SidebarRow(new NavigationItem("Todo", "/todos"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Labour", "/projects/{project}/labour"),
                    DesktopNavigation.ProjectRoles),
                // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
                new SidebarRow(new NavigationItem("Workers", "/labour/workers"),
                    DesktopNavigation.WorkerRegistryRoles),
                // The append-only audit register (new page) — who routed, linked and filed what.
                // Same gate as Triage: the people who make routing decisions review them.
                new SidebarRow(new NavigationItem("Audit Trail", "/audit"),
                    DesktopNavigation.TriageRoles),
                // Everyone the company deals with — the unified page replaces the old separate
                // Clients and Architects entries (their routes survive; the page filters by
                // Clients · Architects · Subcontractors · Internal staff).
                new SidebarRow(new NavigationItem("Directory", "/directory"),
                    DesktopNavigation.DirectoryRoles)
            }),

        // ---- Project: the day-to-day running of the picked job. ----
        new SidebarFolderInfo(
            SidebarFolder.Project,
            "Project",
            "#project",
            new[]
            {
                // The project-specific to-do view — second way in, alongside Internal's master list.
                new SidebarRow(new NavigationItem("To-do", "/projects/{project}/todos"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Drawings", "/projects/{project}/drawings"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Programme", "/projects/{project}/programme"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Progress", "/projects/{project}/progress"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Communications", "/projects/{project}/communications"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("Project Settings", "/projects/{project}/settings"),
                    DesktopNavigation.ProjectRoles)
            }),

        // ---- Financials: the picked project's money first, then the company-wide views. ----
        new SidebarFolderInfo(
            SidebarFolder.Financials,
            "Financials",
            "#financials",
            new[]
            {
                new SidebarRow(new NavigationItem("Financials", "/projects/{project}/financials"),
                    DesktopNavigation.FinanceRoles),
                new SidebarRow(new NavigationItem("Valuation Report", "/projects/{project}/valuation"),
                    DesktopNavigation.FinanceRoles),
                new SidebarRow(new NavigationItem("Cashflow", "/projects/{project}/cashflow"),
                    DesktopNavigation.FinanceRoles),
                // One row per active project plus the total. Exact-only: /finance/* belongs to
                // the Xero and Cash Summary rows.
                new SidebarRow(new NavigationItem("Financial Summary", "/finance", ExactMatch: true),
                    DesktopNavigation.FinanceRoles),
                // Live cash position from Xero. Bank balances are the company's most sensitive
                // figures — directors only, deliberately tighter than FinanceRoles; mirrors the
                // API's authorisation (GetXeroCashSummaryEndpoint).
                new SidebarRow(new NavigationItem("Cash Summary", "/finance/cash-summary"),
                    DesktopNavigation.DirectorRoles),
                // Allocation + Transactions as tabs of one page — Allocation leads (the working
                // screen); the match prefix keeps the row lit on the Transactions tab.
                new SidebarRow(new NavigationItem("Xero", "/finance/allocation", new[] { "/finance/xero" }),
                    DesktopNavigation.FinanceRoles),
                new SidebarRow(new NavigationItem("Cost Codes & Rates", "/cost-codes", new[] { "/rate-library" }),
                    DesktopNavigation.FinanceRoles)
            })
    };
}
