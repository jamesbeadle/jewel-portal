using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar's folders (docs/Pathway-Split-Platform-Flow-Plan.md §6) — the successor to the
/// old three workspace blocks + flat Company list. Grouping follows what the rows are FOR
/// (decision 2026-08-04, replacing the earlier who-is-it-with split): the external parties first
/// (Client, Subcontractor — only what each relationship actually needs), then the job (Project),
/// then the company's own machinery (Internal), time on site (Time), the working money
/// (Finance), the read-only money (Financial Reports), oversight (Audit) and finally the system
/// itself (Admin). Folders mix scopes deliberately — project-scoped rows
/// ("/projects/{project}/…" templates) and company rows sit side by side where the work does
/// (e.g. Workers lives with Labour under Time). A row that belongs to no project at all belongs
/// to no folder either: see SidebarFolders.Standalone.
/// </summary>
public enum SidebarFolder
{
    Client,
    Subcontractor,
    Project,
    Internal,
    Time,
    Finance,
    FinancialReports,
    Audit,
    Admin
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
                // and variations are one lifecycle in one place.
                new SidebarRow(new NavigationItem("Requests", "/projects/{project}/requests"),
                    DesktopNavigation.ProjectRoles),
                // The formal instructions that authorise varied work. Sits with Requests because it
                // is the same correspondence with the same people, and it is what a variation at
                // Awaiting AI is waiting for. The architect can see it: they issue them.
                new SidebarRow(new NavigationItem("Architect's Instructions", "/projects/{project}/architect-instructions"),
                    DesktopNavigation.ArchitectInstructionRoles),
                // Point-in-time captures of issued valuation reports — what the client was
                // actually sent, frozen. New page; finance-gated like the live report.
                new SidebarRow(new NavigationItem("Valuation Report Snapshots", "/projects/{project}/valuation-snapshots"),
                    DesktopNavigation.FinanceRoles)
            }),

        // ---- Subcontractor: what the subcontractor relationship needs — inviting bids, and
        // nothing more. Work Orders and WO Allocation looked at home here but are things WE do
        // with subcontractor money, not things done with subcontractors, so they live under
        // Finance (decision 2026-08-04). ----
        new SidebarFolderInfo(
            SidebarFolder.Subcontractor,
            "Subcontractor",
            "#subcontractor",
            new[]
            {
                new SidebarRow(new NavigationItem("Bid Package Invites", "/projects/{project}/bid-package-invites"),
                    DesktopNavigation.ProjectRoles)
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

        // ---- Internal: the company's own running lists — the master to-do and the people
        // directory. Labour/Workers moved to Time and the audit registers to Audit
        // (decision 2026-08-04). ----
        new SidebarFolderInfo(
            SidebarFolder.Internal,
            "Internal",
            "#internal",
            new[]
            {
                // The master to-do list: all projects plus company-wide items, with a project
                // filter (revived page). TodoListRoles, not ProjectRoles — Accounts holds no
                // project rows but this is the page its work lives on.
                new SidebarRow(new NavigationItem("Todo", "/todos"),
                    DesktopNavigation.TodoListRoles),
                // Everyone the company deals with — the unified page replaces the old separate
                // Clients and Architects entries (their routes survive; the page filters by
                // Clients · Architects · Subcontractors · Internal staff).
                new SidebarRow(new NavigationItem("Directory", "/directory"),
                    DesktopNavigation.DirectoryRoles)
            }),

        // ---- Time: timesheets — labour recorded on the picked site, and the company-wide
        // worker registry it draws from. ----
        new SidebarFolderInfo(
            SidebarFolder.Time,
            "Time",
            "#time",
            new[]
            {
                new SidebarRow(new NavigationItem("Labour", "/projects/{project}/labour"),
                    DesktopNavigation.ProjectRoles),
                // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
                new SidebarRow(new NavigationItem("Workers", "/labour/workers"),
                    DesktopNavigation.WorkerRegistryRoles)
            }),

        // ---- Finance: the money that is worked, not read — the picked project's cost ledger
        // and the subcontractor spend that feeds it, then the company-wide coding screens.
        // The read-only statements live next door in Financial Reports. ----
        new SidebarFolderInfo(
            SidebarFolder.Finance,
            "Finance",
            "#finance",
            new[]
            {
                new SidebarRow(new NavigationItem("Financials", "/projects/{project}/financials"),
                    DesktopNavigation.FinanceRoles),
                // Placing and paying for subcontract work — internal money-handling, which is why
                // these sit here and not under Subcontractor (decision 2026-08-04).
                new SidebarRow(new NavigationItem("Work Orders", "/projects/{project}/work-orders"),
                    DesktopNavigation.ProjectRoles),
                new SidebarRow(new NavigationItem("WO Allocation", "/projects/{project}/work-order-allocation"),
                    DesktopNavigation.ProjectRoles),
                // Allocation + Transactions as tabs of one page — Allocation leads (the working
                // screen); the match prefix keeps the row lit on the Transactions tab.
                new SidebarRow(new NavigationItem("Xero", "/finance/allocation", new[] { "/finance/xero" }),
                    DesktopNavigation.FinanceRoles),
                new SidebarRow(new NavigationItem("Cost Codes & Rates", "/cost-codes", new[] { "/rate-library" }),
                    DesktopNavigation.FinanceRoles)
            }),

        // ---- Financial Reports: the money that is read — the picked project's statements
        // first, then the company-wide views. ----
        new SidebarFolderInfo(
            SidebarFolder.FinancialReports,
            "Financial Reports",
            "#financial-reports",
            new[]
            {
                new SidebarRow(new NavigationItem("Valuation Report", "/projects/{project}/valuation"),
                    DesktopNavigation.FinanceRoles),
                new SidebarRow(new NavigationItem("Cashflow", "/projects/{project}/cashflow"),
                    DesktopNavigation.FinanceRoles),
                // The by-project cash summary: every project's Cashflow statement collapsed to a
                // line, plus the total cash position (bank tiles inside the page are directors
                // only, mirroring the API's GetXeroCashSummaryEndpoint gate). Replaced the
                // consolidated Valuation Summary on the same slug (finance meeting 2026-08-03) —
                // slugs don't move, and the retired company Cash Summary's /finance/cash-summary
                // route lands here too so old bookmarks keep working. Exact-only matching:
                // /finance/* otherwise belongs to the Xero and aged rows.
                new SidebarRow(new NavigationItem("Cash Summary", "/finance", new[] { "/finance/cash-summary" }, ExactMatch: true),
                    DesktopNavigation.FinanceRoles),
                // Gross profit by project: budgeted (initial contract), current (certified less
                // actual cost) and forecasted profit per project, one row each (finance meeting
                // 2026-08-03). Same audience as the per-project Financials tab it reads from.
                new SidebarRow(new NavigationItem("Profit Summary", "/finance/profit-summary"),
                    DesktopNavigation.FinanceRoles),
                // Outstanding sales invoices aged as in Xero but including drafts still being
                // prepared — the sales-side mirror of Aged Payables (finance meeting 2026-08-03).
                // Finance roles: receivables are the valuation invoices this audience already
                // raises and tracks; mirrors the API's authorisation (GetXeroAgedReceivablesEndpoint).
                new SidebarRow(new NavigationItem("Aged Receivables", "/finance/aged-receivables"),
                    DesktopNavigation.FinanceRoles),
                // Outstanding supplier bills aged as in Xero but including drafts — the invoices
                // the accounting procedure leaves in DRAFT until coded through the portal, which
                // Xero's own aged payables report cannot see. Finance roles, like the Xero row:
                // the allocation queue already shows this audience every bill and its amount due;
                // mirrors the API's authorisation (GetXeroAgedPayablesEndpoint).
                new SidebarRow(new NavigationItem("Aged Payables", "/finance/aged-payables"),
                    DesktopNavigation.FinanceRoles)
            }),

        // ---- Audit: who did what — the correspondence audit register and the assistant's
        // activity log. Near the foot deliberately: review, not day-to-day work. ----
        new SidebarFolderInfo(
            SidebarFolder.Audit,
            "Audit",
            "#audit",
            new[]
            {
                // The finance reconciliation trail (new page): every cost-centre move on the
                // valuation report — who moved which line, from where to where, when. Project-
                // scoped, so it leads the folder (the house pattern: the picked project first,
                // then company rows). Moved in from Financial Reports (decision 2026-08-04) —
                // it is a register of who-did-what, not a statement. Mirrors the API's
                // CommercialTeam gate on the filtered audit read (AuditEndpoints).
                new SidebarRow(new NavigationItem("Reconciliation Audit", "/projects/{project}/reconciliation-audit"),
                    DesktopNavigation.FinanceRoles),
                // The append-only audit register (new page) — who routed, linked and filed what.
                // Same gate as Triage (Standalone below): the people who make routing decisions
                // review them.
                new SidebarRow(new NavigationItem("Audit Trail", "/audit"),
                    DesktopNavigation.TriageRoles),
                // What the assistant has done, on whose behalf, and what it cost. Directors only —
                // the log carries spend, and the people who authorise it are the people who see it.
                // Mirrors the API's AiRoles.AllowedToUseAssistant.
                new SidebarRow(new NavigationItem("Agent Activity", "/agents/activity"),
                    DesktopNavigation.DirectorRoles)
            }),

        // ---- Admin: running the system itself, not any project — administrators only (the
        // empty role list + CanSee bypass). Users carries the active/revoked tabs
        // (WorkspaceSections.Admin); the panels lived on the admin home until 2026-07-30, when
        // user administration outgrew a homepage. Last folder deliberately: it is the least
        // day-to-day thing on the rail. ----
        new SidebarFolderInfo(
            SidebarFolder.Admin,
            "Admin",
            "#admin",
            new[]
            {
                new SidebarRow(new NavigationItem("Users", "/admin/users"),
                    DesktopNavigation.AdministratorOnly),
                // The announced app version: publishing an update here raises the refresh bar
                // (UpdateToast) on every signed-in tab. Mirrors the API's AdminGate on the
                // system/version endpoints.
                new SidebarRow(new NavigationItem("System", "/admin/system"),
                    DesktopNavigation.AdministratorOnly)
            })
    };

    /// <summary>Rows that belong to no folder — whole-company destinations that answer to no one
    /// project and no one workspace. They render as top-level links at the FOOT of the sidebar,
    /// below every folder, because the shape says what a folder cannot: these sit outside whatever
    /// the project picker has selected. Gated per row exactly like folder rows.</summary>
    public static readonly IReadOnlyList<SidebarRow> Standalone = new[]
    {
        // The mailbox intake queue — the router for ALL correspondence across EVERY project, and
        // the reason it is not a folder row: under Internal it read as this project's internal
        // work, which it never was. Mirrors the API's TriageRoles gate.
        new SidebarRow(new NavigationItem("Triage", "/requests/triage"),
            DesktopNavigation.TriageRoles)
    };
}
