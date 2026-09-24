namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Programme, labour, documents (the former drawings register), site records and project pages. Data only.</summary>
public static class SitePageGuides
{
    public static readonly IReadOnlyList<PageGuide> Guides = new PageGuide[]
    {
        new("/projects/{project}/programme", "Programme",
            "The project programme tab: four sub-tabs — Programme (a Gantt of tasks measured "
            + "against the latest baseline, with a movement banner when completion slips), Claims "
            + "(Notices of Delay and Extensions of Time raised by Jewel, plus Liquidated Damages "
            + "claims recorded against Jewel), Critical Path RFIs, and Relevant Events (emails "
            + "tagged to the project's scheduling bucket, read live). Manually: Add task, Add "
            + "dependency and Baselines forms build the programme; the slip banner offers Raise NOD "
            + "per delay event; the Claims tab has Raise Notice of Delay, Raise Extension of Time "
            + "and Record LADs claim forms; a Relevant Event email expands to its full body and "
            + "\"Reply in thread\" creates a reply-all draft in the projects mailbox (sent from "
            + "Outlook, not here). NODs, EOTs and critical-path RFIs live in the request register "
            + "(list_requests, get_request_context). Emails become Relevant Events in the Control "
            + "Centre, and an RFI is marked critical path from its own detail page — not here."),

        new("/projects/{project}/labour", "Labour",
            "This project's labour: the weekly timesheet approval grid plus the daily site register "
            + "and subcontractor settlement. Manually: Prev/Next moves the week; submitted rows can "
            + "be ticked and bulk-approved (only approved time posts to Financials as cost; a "
            + "per-cost-code budget hard-block applies server-side), Adjusted (hours in half-hour "
            + "steps, re-coded) or Rejected with a reason the worker sees; the MD/FD/Admin also "
            + "see Unapprove on approved rows (back to Submitted, cost withdrawn, reason to the "
            + "audit trail) and Move… on any row (to another project as it stands — an approved "
            + "day meets the destination's budget hard-block); a manual-entry form "
            + "covers missed sign-outs — and that form is YOUR dialog here: manual_timesheet "
            + "(open_modal) enters one worker's single day (worker as the registry spells them, "
            + "date, hours in half-hour steps, cost code only when one clearly fits); "
            + "workers from the registry are assigned to or removed from "
            + "the project; \"Mark invoice lines as covered…\" reconciles Xero invoice lines "
            + "against approved timesheet cost; Export to Excel covers timesheets, register and "
            + "settlement. Workers log their own time on their My day page, and the worker records "
            + "and rates are managed on /labour/workers, not here."),

        new("/labour/overview", "Labour overview",
            "The company-wide labour month: projected spend with the submission-confidence bar, "
            + "the workers × days placement grid (chips coloured by site), by-site and by-cost-code "
            + "cuts, the chase list, weekly sign-off and the settlement schedules. Manually: "
            + "Prev/Next moves the month; \"Enter a week\" opens the weekly entry dialog — one "
            + "worker's whole week of site days, hours defaulting to 8, cost codes optional "
            + "(each day lands as a Submitted timesheet on its site; the MD codes and approves on "
            + "that project's Labour tab); Record absence logs holiday/half-day/not-worked/sick; "
            + "expanding a worker row edits contracted days and CIS rate; Sign-off marks a week "
            + "looked-at-whole; \"Code month into Xero\" stages draft bills from signed-off "
            + "schedules. Your dialogs here: worker_week (a whole week in ONE update — the "
            + "WhatsApp-transcription path; one worker per fill, reopen for the next) and "
            + "record_absence (one date per confirm). Approval itself lives on each project's "
            + "Labour tab, not here."),

        new("/labour/workers", "Workers",
            "The company-wide registry of day-rate site operatives the timesheets draw from, with "
            + "their cost rates. Manually: Add worker opens a modal (name, portal email that links "
            + "their user account, day rate — stored as hourly = day rate ÷ 8, phone, linked "
            + "subcontractor); Edit reuses it with an Active toggle; Delete is two-click, and a "
            + "worker with timesheet history can only be deactivated; Export to Excel exports the "
            + "table. Rate changes apply to future approvals only — approved timesheets keep their "
            + "snapshotted rate. Assigning workers to a project is done on that project's Labour "
            + "tab, not here."),

        new("/projects/{project}/progress", "Progress",
            "The project's progress page: client-facing progress reports assembled from the "
            + "progress updates below them — updates are titled groups of site photos with a "
            + "description, optional work date and a recorded weather line. Manually: \"+ New "
            + "report\" and Edit open the report form; Download PDF regenerates the report from the "
            + "register on every download; \"+ Record progress\" opens the update form; photos can "
            + "be added to an existing update or deleted, and reports/updates deleted (two-click "
            + "confirm). \"Contractor's Reports…\" is the weekly report register. Over the connector: "
            + "list_progress reads it, create_progress_update / update_progress_update write the "
            + "words, add_progress_photos takes photographs from a source the portal can reach, and "
            + "photographs that only exist in a folder on the laptop come in through the Site "
            + "photos pool (/site-photos) and land here by fingerprint — match_site_photos, then "
            + "file_site_photos onto the day's update."),

        new("/site-photos", "Site photos",
            "The company-wide site photo pool — James's \"big dumping ground for photos and any "
            + "project\" (2026-09-16): photographs dropped in BEFORE anyone has said which project "
            + "or day they belong to, so the weekly-report run can find them by fingerprint instead "
            + "of carrying image bytes through the assistant. Reached from the Project folder; not "
            + "per project. Manually (Admin, Director, PM, Site Manager): drop or pick up to fifty "
            + "JPEG / PNG / HEIC files at a time — each is prepared exactly as a progress photo "
            + "(HEIC → JPEG, upright, 1600px, metadata stripped) and skipped when the pool already "
            + "holds the same bytes; Unfiled / Filed / Archived / All chips; a thumbnail opens the "
            + "viewer; an Archived photo (left out of the report by the weekly run, with its "
            + "reason) is kept and has Restore; "
            + "Delete (two-click) removes a pool photo and its file — a copy already filed onto an "
            + "update stays there. Nothing here names a project: filing does. The site manager "
            + "drops the week's files here from the SAME files that go in the WhatsApp export "
            + "folder, so the laptop-side SHA-256 of a file is the pool's key for it. You: "
            + "list_site_photos (unfiledOnly is the working view, archivedOnly the archive), "
            + "match_site_photos with the folder's hashes, archive_site_photos for what the "
            + "jpms-contractors-report skill keeps out of the report, file_site_photos per day onto "
            + "the progress update, restore_site_photo, delete_site_photo "
            + "(confirm first). You never upload here — a tool call carries words, not files."),

        new("/projects/{project}/documents", "Document register",
            "The project's document register with revisions — drawings, party-wall awards, "
            + "building-control letters, reports, anything issued for the project. Each row is a "
            + "document with its code, title, original file name, latest approved revision, "
            + "pending/archived counts and pipeline status, plus an \"ambiguous\" count badge for "
            + "older mailbox imports that couldn't be auto-classified (new uploads never are — a "
            + "blank revision is simply \"no revision\"). Rows group into folders, and folders nest "
            + "(sub-folders indent beneath their parent; documents sit at any level; Ungrouped "
            + "last). Code, title, revision and issuer are all optional on upload — a document "
            + "with no title shows its file name. Manually: a toggle switches between all "
            + "documents and approved-only; \"+ Add documents\" (Admin/MD/PM) opens the upload "
            + "form (a zip is unpacked into its files); \"Extract all\" queues every unprocessed "
            + "PDF through Bluebeam + the text-layer reader — any PDF, drawing or not; \"+ New "
            + "folder\" and each folder's + / pencil / bin buttons add a sub-folder, rename or "
            + "delete it (contents move up a level); Export to Excel exports the register; opening "
            + "a row goes to the document's detail page for revision history and the viewer. "
            + "Incoming files from correspondence are filed to this register from Document "
            + "Triage, not uploaded here. Was \"Drawings\" at /drawings until 2026-09-03 — the "
            + "old URL redirects.",
            Aliases: new[] { "/projects/{project}/drawings" }),

        new("/projects/{project}/documents/{drawingId}", "Document detail",
            "One document's page: revision history alongside an inline viewer (PDFs and images), "
            + "previewing the approved revision if there is one, else the most recent revision with "
            + "a file, with Previous/Next stepping through the register. Manually (Admin/MD/PM): "
            + "the pencil by the code/title edits them in place (both optional); the folder picker "
            + "moves the document to any folder or sub-folder; \"Extract data\" queues the "
            + "previewed PDF revision through Bluebeam (markups) and the text-layer reader — it "
            + "works on ANY PDF, not only drawings, and the Extracted data panel below shows the "
            + "run; \"+ Upload new version\" adds a revision; \"Delete document\" (confirm modal) "
            + "permanently removes the document, all its revisions and files; the revision list "
            + "carries each revision's approval and pipeline status, and a pencil by the revision "
            + "label sets it (uploads may have none). You have no dialogs here; navigate_to opens "
            + "it (tools that return a document route are preferred).",
            Aliases: new[] { "/projects/{project}/drawings/{drawingId}" }),

        new("/projects/{project}/documents/ambiguous", "Ambiguous document revisions",
            "The queue of uploaded document revisions JPMS couldn't auto-classify — filenames that "
            + "didn't match the expected revision pattern, awaiting PM action. It renders the same "
            + "revision list as the register; a breadcrumb links back to the document register, "
            + "whose header badge shows the pending count. Reached by URL, not from the sidebar.",
            Aliases: new[] { "/projects/{project}/drawings/ambiguous" }),

        new("/projects/{project}/communications", "Communications",
            "The cross-cutting roll-up of ALL correspondence tagged to this project's records, read "
            + "live from the mailbox, newest first with paging. Manually: a segmented control "
            + "filters by pathway (Client / Subcontractor / Internal) and a \"Tagged to\" dropdown "
            + "by record type; each row shows its pathway chip and the record(s) it is tagged to, "
            + "and Reply / Forward opens the composer above the list — sending happens there and "
            + "then from the projects mailbox, and the sent copy files back into this list by the "
            + "thread's tags. A search box finds emails within the project's tagged mail (subject, "
            + "body, sender, attachment name — list_project_communications search); while a search "
            + "is live the list is one relevance-ordered page. Each row's \"Add tag\" (triage roles) "
            + "links an already-triaged email to another of this project's records — a record type "
            + "then the record — without a trip to the Control Centre; that is the only tagging "
            + "here. Initial triage (stage_triage_tag) and a brand-new email (open_modal "
            + "compose_email) still happen in the Control Centre — not here."),

        new("/projects/{project}/defects", "Defects",
            "The project's defect register — each defect carries a sequential DEF-#### reference "
            + "which is also its mailbox tag stem. A defect is raised WITH a supplier: a directory "
            + "record (Subcontractor / Supplier category) picked from the list, the way a work order "
            + "names its supplier; a free-typed contact email is the stop-gap for a company not yet "
            + "in the directory. Manually: \"Raise defect\" opens an inline form (location, "
            + "supplier, description) and then opens the new defect's own page; the register shows "
            + "Supplier and Sent (when the defect was first emailed to the supplier, or \"Not "
            + "sent\"); each row's Status dropdown walks Open → In progress → Resolved → Verified; "
            + "the reference, description and \"Open\" go to the defect's page, where sending, "
            + "correspondence and to-dos live. A defect can also be raised from a subcontractor "
            + "email in the Control Centre (System Tags → Create new → Defect). Raising a defect "
            + "emails nobody. Assistant: list_defects, raise_defect (subcontractorId from "
            + "search_directory), update_defect."),

        new("/projects/{project}/defects/{defectId}", "Defect detail",
            "One defect's own page — its facts (supplier as a directory record with contact, sent "
            + "to supplier, raised, resolved), Edit (location, supplier, contact email, "
            + "description — a modal), the status dropdown, and the two things that move a defect. "
            + "\"Send to supplier\" (primary button, shown until the first send) opens the shared "
            + "composer pre-addressed to the supplier's directory email and pre-written from the "
            + "defect; the user edits and sends from the projects mailbox; the sent copy carries "
            + "JPMS/DEF-#### so the supplier's replies file themselves back into this page's "
            + "Communications, and the server stamps Sent and moves Open → In progress. After that "
            + "the button reads \"Chase supplier\" (a reminder, same composer). Communications is "
            + "the shared thread list with Find & tag emails, Reply/Forward and New email, all "
            + "filed to the defect. The To-dos panel lists the to-dos ABOUT this defect (an "
            + "explicit link on the to-do, not a mail tag) and \"New to-do\" raises one on this "
            + "project with the title and link pre-filled; each opens the to-do's page, which "
            + "links back. Assistant: list_defects / find_by_reference (this route), "
            + "update_defect, list_todos aboutRecordId = the defectId, add_todo aboutRecordType "
            + "Defect + aboutRecordId; sending/chasing the supplier has no connector action — it "
            + "is done here."),

        new("/projects/{project}/building-control", "Building Control",
            "The project's building control — the statutory sign-off trail. The case panel holds "
            + "who signs the work off (local authority or registered approver), their reference, "
            + "the contact, the official dates and the case documents (notice, acknowledgement, "
            + "decision notice, completion certificate), with a status ladder Notice submitted → "
            + "In force → Completion requested → Completion certified (Lapsed for a dead case). "
            + "Below it is the inspection register: stages seeded from a standard checklist and "
            + "freely edited, each with a sequential BCI-#### reference that is also its mailbox "
            + "tag stem. Manually: \"Set up building control\" creates the case; \"Add "
            + "inspection\" adds a stage (a date makes it Booked); a row click opens the stage's "
            + "own page; only a Planned stage with no files can be removed. An inspection can also "
            + "be raised from the inspector's email in the Control Centre (System Actions → Raise "
            + "Building Control Inspection), and tagging further emails happens there "
            + "(stage_triage_tag) — not on this page."),

        new("/projects/{project}/building-control/inspections/{inspection}", "Building Control Inspection",
            "One inspection stage: its status ladder (Planned → Booked → Inspected → Passed / "
            + "Actions required → Closed — moving to Inspected stamps the visit date; a failed "
            + "visit is re-booked on the SAME record, not a new row), the official booked/"
            + "inspected dates and inspector, the outcome notes, the photo evidence grid, the "
            + "documents list (the inspector's site report), and the correspondence read live by "
            + "the stage's JPMS/BCI-#### tag. Manually: edit the details and Save; upload photos "
            + "(on site, from the phone's camera) and documents; \"Copy attachments\" pulls "
            + "the inspector's report and photos off a linked email into the stage's files; Reply/"
            + "Forward under an email sends from the projects mailbox and files itself back here."),

        new("/projects/{project}/hs", "H&S",
            "The project's health & safety in three panes (2026-09-15). Audits: the officer's site "
            + "inspection reports (HSA-####), each planted from the inspection framework — 11 "
            + "sections, 165 items on version 2026-09-15 (182 on 2026-08-27) — and scored exactly "
            + "as her sheet scores it: sum of rates ÷ (rated items × 10), unrated items excluded, "
            + "less a penalty for each class present on the report once (A 25%, B 15%, C 5%, D 1%) "
            + "and 5% once for any repeat, banded Poor under 70% / Fair / Good 85–94 / Very good "
            + "95+; status Draft → Issued → Closed; \"Download PDF\" on the audit gives the report "
            + "in her layout to send on (the portal never emails it); a row "
            + "click opens the audit's form. Actions: the corrective actions on the H&S register, "
            + "most minted by an audit's Issue (one per item with an owner or a rate below 10, not "
            + "N/A), owned by a named person who needs no login, overdue rows in warning, status "
            + "changed on the row (Open / In progress; Closed is the H&S officer's or a director's "
            + "alone, on her next visit or on a photograph — a site manager cannot close one). A "
            + "row opens the action's thread (2026-09-23): comments by the site manager and the "
            + "officer, each with an optional photograph of the work done; a comment moves an Open "
            + "action to In progress by itself. What is said or changed reaches the other side as "
            + "one email per project per sitting — the site manager's address is on the project's "
            + "settings — never one per item. Register: observations, near "
            + "misses, incidents, toolbox talks, permits, logged here with \"Log record\". "
            + "Manually: \"New audit\" takes the type, date, officer and site manager and opens "
            + "the form; \"Site check forms\" opens one of Katy-Louise's paper sheets (toolbox "
            + "talk, ladder, equipment schedule, PUWER, first aid kit, fire extinguishers) or an "
            + "incident report to fill in, filed under the site on /forms. Assistant: "
            + "list_hs_audits, get_hs_audit, list_hs_records (with each action's comments and "
            + "photos), create_hs_audit, update_hs_audit_items, issue_hs_audit (confirm-first), "
            + "close_hs_audit, log_hs_record, update_hs_record, comment_on_hs_record."),

        new("/projects/{project}/hs/audits/{audit}", "H&S Audit",
            "One site audit — the inspection report form. The Report panel is the front sheet "
            + "(type, inspection date, officer, site manager, operatives, summary, further "
            + "comments; the previous audit's score and the framework version read-only). The "
            + "score pill is live as rows change. Each section below is one block of the "
            + "framework: per item a comment code (N/A, N, N/C, N/S, R), rate 0 / 5 / 10, class "
            + "A–E, the points the class takes off (read-only, the sheet's Minus column), time-scale "
            + "(I, 1, 3, 7, 1M, O), findings, owner name, date rectified — saved a section at a time "
            + "(\"Save section\"). \"Download PDF\" renders the report in her layout in any status. "
            + "\"Issue audit\" is the officer's "
            + "declaration: Draft → Issued, and every finding becomes a corrective action on the "
            + "Actions pane, linked back to its row (an \"Action\" pill). \"Close audit\" is "
            + "the manager's declaration and is refused while any of those actions is still open; "
            + "closing an action on the register stamps the row's date rectified. A Closed audit "
            + "is read-only. Assistant: get_hs_audit (every item with its hsAuditItemId), "
            + "update_hs_audit_details, update_hs_audit_items, issue_hs_audit, close_hs_audit."),

        new("/projects/{project}/useful-information", "Useful Information",
            "Titled free-text notes for the office's own use — door codes, key safe locations, "
            + "site access. Strictly internal: the API gates reads and writes to internal roles, so "
            + "nothing here can reach a client, architect or subcontractor login."),

        new("/projects/{project}/settings", "Project settings",
            "The project's single settings page, in four panes: Details (stage, entity, project "
            + "manager, client, site address and the Xero \"Sites\" mapping — \"Not set\" on Xero "
            + "site blocks the Xero write-back), Deposits, retentions & valuation (the next "
            + "valuation date and the retention profile), Contract (the executed contract document "
            + "and terms — get_project_contract reads the same data), and Correspondence (the "
            + "profile that routes documents the project issues). You read project facts with "
            + "list_projects and the contract with get_project_contract; there are no registered "
            + "dialogs here."),

        new("/projects/{project}/todos", "Project to-dos",
            "This project's to-do list (the master list across all projects is /todos). Items are "
            + "added here directly or captured from an email at triage — you stage those in the "
            + "Control Centre with stage_triage_todo, not on this page."),

        new("/projects", "Projects",
            "The project portfolio register: reference, name, client, entity, stage and next "
            + "expected valuation date (Overdue in red, Due soon in amber, Not set when blank) for "
            + "every project, hiding completed ones unless \"Show completed\" is ticked. "
            + "\"Overdue valuations only\" narrows the table to the rows the dashboard's Valuations "
            + "overdue tile counted (?valuations=overdue opens the page already filtered). Manually: "
            + "\"+ New project\" (MD/PM) opens the New project modal, though projects are normally "
            + "created from a won lead; Export to Excel exports the portfolio. You read the same "
            + "data with list_projects and use navigate_to to open a project, which lands on the "
            + "role's first project tab."),

        new("/projects/{project}", "Project (redirect)",
            "A redirect hub, not a page: the bare project URL immediately forwards to the "
            + "signed-in role's first project-scoped tab (Requests for full-access roles). Never "
            + "send users here expecting content — navigate_to a specific tab instead."),
    };
}
