namespace Jewel.JPMS.Contracts.Ai;

/// <summary>To-dos, the directory registers, audit, agents and admin. Data only.</summary>
public static class OfficePageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/todos", "To-dos",
            "The master to-do list across every project — company-wide and project items together, "
            + "with a project filter. The MD and administrators see every item; everyone else sees "
            + "only items assigned to a role they hold, minus items pinned to a different named "
            + "person. Manually: Board view (drag a card between Open and Done) or List view with "
            + "Open/Done/All tabs; filters for project scope, role and — for MD/admin — person; "
            + "\"Add to-do\" opens a modal with Project picker (blank = company-wide, MD/admin "
            + "only), title, assignee, due date and notes. Clicking an item opens its detail page. "
            + "find_by_reference resolves a spoken \"TODO-0074\" to the item (title, notes, "
            + "project, route), and read_record_emails (record_type todo) reads its tagged mail — "
            + "so ACTION an item yourself rather than telling the user to click it: e.g. a "
            + "\"raise this WO\" item means read its emails, then open_modal work_order_create "
            + "with the item's projectId. You navigate_to here; no dialog opens on this page "
            + "itself. To-dos staged from an email (stage_triage_todo) happen in the Control "
            + "Centre, not here."),

        new("/todos/{todoItemId}", "To-do detail",
            "One to-do item's own page — its full facts, multiline notes, the to-dos linked to it "
            + "by shared tagged mail, and its communications: the item's tagged emails read live, "
            + "each answerable here, plus a new outbound email filed to the item. Manually: Mark "
            + "done / Reopen; a two-click armed Delete; the reassign editor (role, optionally "
            + "pinned to a person) and the move editor (to another project, or company-wide for "
            + "MD/admin). Reassign, move and delete need the manage gate; completing needs manage "
            + "or the item being the reader's own. You navigate_to with the ready-made route "
            + "(find_by_reference resolves \"TODO-0074\" to it), and read_record_emails "
            + "(record_type todo) reads this item's communications; no dialog opens here, but "
            + "actioning the item's work often opens one elsewhere — e.g. work_order_create for a "
            + "\"raise this WO\" item. Items are added on /todos or a project's To-do tab, not "
            + "on this page."),

        new("/architects", "Architect practices",
            "The register of architect practices — when an architect is a project's party, RFIs "
            + "and other request documents go to the contact email held here. Manually: a table of "
            + "Practice / Contact / Contact email with per-row Contacts (the party contacts "
            + "editor) and Edit buttons, a \"New architect\" button, and Export to Excel. You can "
            + "navigate_to only — no dialog is registered, so creating or editing a practice is "
            + "the user's manual act. Client accounts are managed on /clients, not here."),

        new("/clients", "Client accounts",
            "The register of client accounts — when a client is a project's party, request "
            + "documents go to the primary contact held here. Manually: a table of Client / "
            + "Primary contact / Contact email with per-row Contacts and Edit buttons, a \"New "
            + "client\" button, and Export to Excel. You can navigate_to only — no dialog is "
            + "registered here. Architect practices are managed on /architects; when Jewel works "
            + "through an architect, the practice is the party on the project or request."),

        new("/dashboard", "Home",
            "The signed-in home page, rendered per active role: administrators get the admin home "
            + "(user directory and pending access-request panels), every other role gets its role "
            + "home — for site-floor roles that is the My Day workspace. There are no page-level "
            + "actions of its own. The retired /my-day route redirects here.",
            Aliases: new[] { "/my-day" }),

        new("/audit", "Audit trail",
            "The append-only audit register — who routed, linked and filed what: every recorded "
            + "client-facing interaction plus finance reconciliation events, newest first, 50 per "
            + "page with Load more. Manually: pathway tabs (All / Client / Subcontractor / "
            + "Internal), an event-type select and a project filter; the Email column's \"Open in "
            + "Outlook\" link opens the drafted email; request references link to the request "
            + "detail page. You navigate_to only — the register is read-only; nothing is created "
            + "or edited here."),

        new("/agents/activity", "Agent activity",
            "What the assistant has done, on whose behalf, and what it cost — every agent run, "
            + "newest first, with when, agent, who it ran as (rows marked \"unattended\" ran with "
            + "nobody watching), action, outcome, tools used, duration, tokens and cost, plus "
            + "totals underneath. Manually: All runs and Unattended only filters; clicking a row "
            + "that carries a route navigates to the page that run touched. You navigate_to only — "
            + "the log is read-only. This is the run log of the agents listed on /admin/agents; it "
            + "is not the request-watching queue at /agents."),

        new("/agents", "Agent queue",
            "The queue of requests being watched by applied discipline agents, across every "
            + "project — each row shows the agent, its discipline, an Active or Complete badge, "
            + "the request reference and title, and a status message. The page is a read-only "
            + "list; clicking a row opens that request's detail page. Agents are applied FROM a "
            + "request page — nothing is added or configured here. You navigate_to; list_requests "
            + "and get_request_context read the underlying requests. Distinct from /admin/agents "
            + "(the AI agent registry) and /agents/activity (the run log)."),

        new("/admin/agents", "AI agents",
            "The assistant's agent registry, live — the real catalogue the turn loop runs on, one "
            + "card per agent showing its key, description, who may engage it, its triggers and "
            + "route prefixes, its code-owned working instructions and \"done means\", and the "
            + "skills it works from. The page is read-only apart from links — \"Add a skill\" and "
            + "each skill row deep-link into /admin/skills. Agent configuration is code and cannot "
            + "be changed here; agent knowledge is edited on /admin/skills. You navigate_to; "
            + "switch_agent changes the agent in force in conversation, not anything on this page."),

        new("/admin/skills", "AI skills",
            "The assistant's editable domain skills — versioned markdown manuals that are in force "
            + "on the assistant's very next message, no deploy. Manually: a table of skills "
            + "(agent, Pinned/On demand, Active/Off, version, size, references, updated); \"New "
            + "skill\" or clicking a row opens the editor: skill key (fixed after creation), "
            + "owning agent or Shared, display name, description (what the assistant routes on), "
            + "Pinned and Active toggles, and the markdown body — pasting a whole SKILL.md lifts "
            + "its frontmatter into the empty fields; each save is a new version, and reference "
            + "documents can be added per skill. You navigate_to; no dialog opens here — the "
            + "editor is the user's own form."),

        new("/admin/users", "Users",
            "User administration — everyone who can sign in, with invites and the role "
            + "assignments that decide what each person sees. Revoking a user moves them to the "
            + "Revoked list at /admin/users/revoked, where they can be restored or permanently "
            + "deleted. Requires the Administrator active role. You navigate_to only — role "
            + "changes and invites are the administrator's manual acts."),

        new("/admin/users/revoked", "Revoked users",
            "The people whose access has been revoked — they cannot sign in, but their record and "
            + "old roles are kept. Manually: Restore puts a user back exactly as they were; Delete "
            + "permanently removes the record and is offered only here, never on a live account. "
            + "Requires the Administrator active role. You navigate_to only."),

        new("/admin/system", "System",
            "The announced app version — the version every signed-in tab is asked to run. "
            + "Manually: the Announced version panel's \"Publish update\" button (two-step "
            + "confirm) increments the version and raises the refresh bar on every open tab, used "
            + "once a deploy has finished; below it, the Tender terms & conditions panel holds the "
            + "company's single standard T&C PDF that is attached automatically to every "
            + "tender-invite email on every project. Requires the Administrator active role. You "
            + "navigate_to only — publishing and uploading are the administrator's manual acts."),

        new("/admin/trades", "Trades",
            "Admin → Trades: the curated master list of trades the directory and bid packages "
            + "pick from — this page is where that vocabulary is managed. A person adds, renames "
            + "or deletes a trade; deletes are refused while directory records or packages still "
            + "use one. You navigate_to only — no dialog or page action is registered here."),

        new("/registers", "Registers",
            "The company registers — insurances, subscriptions, vehicles and trade accounts, one "
            + "tab per kind, each dated field tracking a renewal so nothing lapses unseen. A "
            + "person adds and edits entries in place. You navigate_to only — no dialog or page "
            + "action is registered here."),

        new("/policies", "Policies & sign-off",
            "Staff sign-off forms: NDAs, staff policies and H&S documents. An admin publishes a "
            + "document to named users; each recipient reads and signs, and the page tracks who "
            + "has signed what. You navigate_to only — no dialog or page action is registered "
            + "here."),

        new("/labour/xero-mapping", "Xero mapping",
            "The effective-dated bridges between the portal and Xero: project to Sites tracking "
            + "category, worker to Xero contact, and the account codes labour lands on. Finance "
            + "sets a mapping with a from-date; the labour coding runs read whatever is effective. "
            + "You navigate_to only — no dialog or page action is registered here."),

        new("/sales-analytics", "Sales analytics",
            "The CRM's sales analytics page — source attribution and win rate by channel, drawn "
            + "from the lead store: three stat tiles (Total leads, Won, Win rate) and a "
            + "leads-by-source table. It is read-only with no controls. You navigate_to only; "
            + "leads themselves are not created or edited here."),
    };
}
