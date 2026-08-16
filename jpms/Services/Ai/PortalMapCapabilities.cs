namespace Jewel.JPMS.Services.Ai;

/// <summary>
/// What each page IS and what can be DONE there — the curated half of the assistant's site map.
///
/// <para><b>Division of labour with <see cref="PortalMap"/>:</b> the route list itself stays derived
/// from <see cref="Navigation.SidebarFolders"/> (a renamed or role-gated route changes in the same
/// commit, per the house rule), and this file only ANNOTATES it. A note is keyed by the exact href
/// template the catalog carries; a note whose route leaves the catalog simply stops rendering, so
/// this file can describe pages but can never invent or resurrect one.</para>
///
/// <para>The <see cref="DetailPages"/> section is the one place that IS hand-kept route templates:
/// record detail pages have no sidebar row to derive from (they are reached from their register or
/// from a tool's returned route), and the orchestrator has to know they exist to be told "open
/// V72". Each entry names its @page route verbatim — when a detail page moves, this list is the
/// second place the commit touches, and docs/site-map.md the third.</para>
///
/// <para>Keep every note to ONE line and write it for the model: what lives there, and which of its
/// verbs work there — read, create (which dialog), tag emails. No prose the model cannot act on.</para>
/// </summary>
public static class PortalMapCapabilities
{
    /// <summary>Capability note per sidebar href template, exactly as SidebarFolders spells it.</summary>
    public static readonly IReadOnlyDictionary<string, string> ByHref = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // ---- Project ----
        ["/projects/{project}/requests"] =
            "the request/RFI register (tabs: all, general, RFIs). Open a request to read and work it",
        ["/projects/{project}/variations"] =
            "the variation book, V1..Vn with status and value. The \"Add variation manually\" dialog "
            + "(manual_variation) lives here for a brand-new standalone variation",
        ["/projects/{project}/architect-instructions"] =
            "the Architect's Instruction register — the formal instructions a variation at Awaiting AI is waiting for",
        ["/projects/{project}/valuation-snapshots"] =
            "frozen point-in-time captures of issued valuation reports — what the client was actually sent",
        ["/projects/{project}/drawings"] =
            "the drawing register with revisions; open a drawing for its revision history and viewer",
        ["/projects/{project}/programme"] =
            "the project programme (plan of work)",
        ["/projects/{project}/todos"] =
            "this project's to-do list (the master list is /todos)",
        ["/projects/{project}/progress"] =
            "progress updates and photos from site",
        ["/projects/{project}/defects"] =
            "the defect register (DEF-#### references); each defect reads its tagged mail live",
        ["/projects/{project}/communications"] =
            "ALL correspondence tagged to this project's records, in one stream",
        ["/projects/{project}/useful-information"] =
            "internal-only notes for the office — door codes, key safes, site access",
        ["/projects/{project}/settings"] =
            "project settings: names, dates, stage",

        // ---- Subcontractor ----
        ["/projects/{project}/bid-package-invites"] =
            "bid packages inviting subcontractor prices; open one for its invites, quotes and tagged emails",
        ["/projects/{project}/work-orders"] =
            "work orders placed with subcontractors; open one for its detail and purchase order",
        ["/subcontractors/communications"] =
            "general subcontractor correspondence (everything tagged JPMS/SubComms at triage), read live",

        // ---- Internal ----
        ["/todos"] =
            "the master to-do list across every project, with a project filter",
        ["/directory"] =
            "everyone the company deals with — clients, architects, subcontractors, staff; open an entry for its record",

        // ---- Time ----
        ["/projects/{project}/labour"] =
            "labour recorded on this site — timesheet days per worker",
        ["/labour/workers"] =
            "the company-wide worker registry the timesheets draw from",

        // ---- Finance ----
        ["/projects/{project}/financials"] =
            "this project's cost ledger by cost centre",
        ["/projects/{project}/work-order-allocation"] =
            "allocating subcontractor spend across work orders",
        ["/finance/payment-certificates"] =
            "the payment certificate register — what the client is paying, certified; company page with a project filter",
        ["/cost-codes"] =
            "the cost-centre master (list_cost_codes reads the same data)",
        ["/rate-library"] =
            "the rate library — priced rates by trade",

        // ---- Financial Reports ----
        ["/projects/{project}/cashflow"] =
            "this project's to-completion cash statement",
        ["/finance/cash-forecast"] =
            "the company time-phased cash forecast",
        ["/finance/profit-summary"] =
            "gross profit by project: budgeted, current and forecast",

        // ---- Xero ----
        ["/finance/xero"] =
            "Xero transactions as synced",
        ["/finance/aged-receivables"] =
            "outstanding sales invoices aged as in Xero, drafts included",
        ["/finance/aged-payables"] =
            "outstanding supplier bills aged as in Xero, drafts included",

        // ---- Audit ----
        ["/projects/{project}/reconciliation-audit"] =
            "who moved which valuation line, from where to where, when",
        ["/audit"] =
            "the append-only audit register — who routed, linked and filed what",
        ["/agents/activity"] =
            "what the assistant has done, on whose behalf, and what it cost",

        // ---- Admin ----
        ["/admin/users"] = "user administration",
        ["/admin/system"] = "the announced app version",
        ["/admin/agents"] = "the assistant's agent registry, live",
        ["/admin/skills"] = "the assistant's editable domain skills",

        // ---- Standalone work queues + the flagship report ----
        ["/control-centre"] =
            "the mailbox intake queue and router for ALL correspondence across every project. "
            + "Everything about the SELECTED email is done on this page, never by navigating away: "
            + "tag it to any record — variations, RFIs, defects, bid packages — with stage_triage_tag "
            + "(the same act as picking it in the System Tags pane; Apply lands it); stage to-dos with "
            + "stage_triage_todo (assignee, notes, due date; Apply raises them); set its project; "
            + "write a reply or forward; raise records from it in System Actions. Also the "
            + "New email composer: the compose_email dialog opens HERE (open_modal compose_email) for "
            + "any email the user asks you to draft; they review and press Send on this page",
        ["/document-control"] =
            "the attachment triage queue for all projects — files from the Control Centre, filed out to "
            + "Drawings, Payment Certificates or subcontractor records",
        ["/finance/allocation"] =
            "distributing allocated Xero purchase lines to cost centres",
        ["/projects/{project}/valuation"] =
            "the picked project's LIVE valuation report — the system's flagship output",
    };

    /// <summary>
    /// One record detail page per line: "name → route template — capabilities". Substitute real ids
    /// (tools return ready-made routes — prefer those). Rendered by PortalMap under its own heading.
    /// </summary>
    public static readonly IReadOnlyList<string> DetailPages = new[]
    {
        "Request / RFI → /projects/{project}/requests/view/{requestId} — the full working papers: "
            + "description, response, notes, every tagged email. get_request_context and "
            + "read_record_emails read it; the variation_draft dialog opens here; "
            + "\"Find & tag emails\" adds more correspondence",
        "Variation → /projects/{project}/variations/{variationOrderId} — one document through "
            + "Quoting → Issued → Awaiting AI → Approved/Rejected, with its quotes and tagged emails "
            + "(read_record_emails works here)",
        "Bid package → /projects/{project}/bid-package-invites/{bidPackageId} — scope lines, invited "
            + "subcontractors, their quotes, and the tagged email thread (get_bid_package_context and "
            + "read_record_emails read it; the bid_package_details dialog opens here to build the "
            + "package out — summary and line schedule in one update)",
        "Work order → /projects/{project}/work-orders/{workOrderId}/po — the purchase order as issued",
        "Drawing → /projects/{project}/drawings/{drawingId} — revision history and viewer",
        "To-do → /todos/{todoItemId} — one to-do with its notes and tagged mail",
        "Directory entry → /directory/{subcontractorId} — a subcontractor's master record, compliance and history",
    };

    /// <summary>The note for a sidebar href, or null.</summary>
    public static string? For(string href) => ByHref.TryGetValue(href, out var note) ? note : null;
}
