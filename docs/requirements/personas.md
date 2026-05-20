# Personas

Nine canonical user roles for the **Project Program Scheduler** (production brand: **JPMS**). Each card stands alone — there is one source of truth per role.

Each card is **Draft** until the named person in that role has reviewed it and agreed it matches their day-to-day. The card template lives in [`_templates/personas-template.md`](_templates/personas-template.md). The Role × Workflow RBAC matrix lives in [`permission-matrix.md`](permission-matrix.md).

### Persona register

| # | Persona | Type | Sourced from |
|---|---|---|---|
| P01 | Architect | External client | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P02 | Subcontractor | External delivery partner | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P03 | Project & Commercial Lead | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P04 | Office & Compliance Coordinator | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P05 | Site Team | Internal field | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P06 | Brand & Content | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P07 | Finance Director (FD) | Internal executive | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P08 | Directors / MD | Internal executive | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P09 | Outsourced IT Helpdesk | External service partner | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |

---

## P01 — Architect

**Role:** External client architect commissioning work from Jewel Enterprises
**Reports to:** Their own firm / their own client
**Tooling today:** Architecture / CAD software, email, drawing packages, client portals
**Frequency on platform:** Periodic — at tender submission and during VO discussions
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- Get work delivered on time and to specification.
- Track their cost codes accurately through to billing.
- Be informed of Variation Orders before they affect timeline or cost.

### Pain points (current state)
- No shared system; documents passed via email or portal handoffs.
- Visibility into on-site progress is opaque.

### Owns these workflows
- Approver on 04 (RFI replies and variation sign-off). Source on 01 (issues drawings). Recipient on 05 (valuations) and 06 (live dashboard).

### Permissions needed (coarse)
- Read on their own tenders and projects. Write on tender submission. Approve / reject VOs on their work. Read on cash-call documentation for their projects.

### Devices & environment
- Desktop primarily. May use tablet on site visits.

### Notes
- The architect's client-facing **cost codes** must be referenced consistently through every screen and report that touches that architect's tender. This is a cross-cutting requirement, not specific to one journey.
- Where Jewel works with an external Quantity Surveyor (consultant), they are treated as an invited contact rather than a separate persona — internal QS work is owned by P03 Project & Commercial Lead.

---

## P02 — Subcontractor

**Role:** Field worker delivering work against tender line items
**Reports to:** Project & Commercial Lead (commercially); Site Team (operationally on site)
**Tooling today:** Phone, paper, ad-hoc messaging
**Frequency on platform:** Daily — on site
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- Quickly log progress and time on site.
- Get RFIs answered fast so work isn't blocked.
- Bid for new work without learning a new tool.
- Get paid on time and accurately.

### Pain points (current state)
- Paper timesheets get lost or delayed.
- RFI handling is slow and untracked.
- No personal view of progress against the tender.
- Compliance documentation scattered across systems; chased manually before expiry.

### Owns these workflows
- Source on 03 (returns quote), 04 (raises RFIs from site), 06 (attendance + photos), 08 (uploads compliance documents). Contributor on 07 (defect resolution evidence).

### Permissions needed (coarse)
- Update completion on line items assigned to them. Submit timesheets. Raise RFIs. Read drawings and specs for their assigned work. Self-service upload of own compliance documents.

### Devices & environment
- Mobile phone primarily. Touch-friendly UI is essential, including in poor connectivity (offline-tolerant fields where possible).

### Notes
- Subcontractors are external to Jewel Enterprises. Onboarding needs to be lightweight — typically an invite link rather than a full account-creation flow.

---

## P03 — Project & Commercial Lead

**Role:** Internal project lead combining PM and commercial responsibilities — owns the project from BoQ through close-out, including the internal QS function
**Reports to:** Directors / MD
**Tooling today:** Bluebeam, MS Project, Excel BoQ, Buildertrend, Planyard, SharePoint, Outlook
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Run the project programme and valuation cycle cleanly without manual reconciliation between MS Project and Excel.
- Make award decisions on bids and variations with side-by-side data, not assembled spreadsheets.
- Price tenders accurately into line items and keep them in sync as scope changes via VOs.
- Stop being the human bridge between drawings, BoQ, RFIs, and the programme.

### Pain points (current state)
- Line items live in spreadsheets disconnected from completion tracking and billing.
- Programme and valuation reconciliation is manual and slow.
- Three separate Excel logs for variations, RFIs and delay notices that don't talk to each other.
- Bid packages are assembled by hand from SharePoint / Outlook / Excel.
- Drawing supersedure relies on memory.

### Owns these workflows
- 02 (Tender & BoQ), 03 (Procurement), 04 (Variations / RFIs / Delays), 05 (Programme & Valuations), 07 (Close-Out & Defects). Reviewer on 01 (drawing supersede override) and 06 (site report sign-off).

### Permissions needed (coarse)
- Owner on project records and BoQ. Approver on bids, variations, valuations, defect sign-off. Read across all projects.

### Devices & environment
- Laptop primarily; tablet on site.

---

## P04 — Office & Compliance Coordinator

**Role:** Internal office and compliance hub — keeps the operational machinery running
**Reports to:** Directors / FD
**Tooling today:** Outlook, SharePoint, Monday.com, Dashpivot, RAMsApp, phone system
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Keep compliance, insurance, fleet, procurement and front-of-house comms clean and current.
- Stop being the human router for documents and client enquiries.

### Pain points (current state)
- Subcontractor compliance scattered across Monday, SharePoint and Excel.
- Insurance / accreditation renewals tracked in Outlook calendar and chased manually.
- Material orders raised via WhatsApp / email with no project linkage.
- Document filing in SharePoint is a never-ending tidy-up task.

### Owns these workflows
- 08 (Subcontractor Compliance), 14 (Client & Reactive Comms), 15 (Materials & Deliveries), 18 (Compliance / Insurance / Accreditation), 19 (Fleet), 21 (Document Management). Co-owner on 16 (HR / Onboarding).

### Permissions needed (coarse)
- Owner on supplier & subcontractor directory, compliance register, fleet register, procurement requests. Approver on routine renewals.

### Devices & environment
- Desktop primarily.

---

## P05 — Site Team

**Role:** Internal site managers, foremen, and operatives — the capture layer for site reality
**Reports to:** Project & Commercial Lead
**Tooling today:** WhatsApp groups, phone camera, paper notes, Dashpivot
**Frequency on platform:** Daily — on site
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Capture progress, photos, attendance and issues in seconds, not minutes.
- Get the right drawing on the phone without asking.
- Submit timesheets without being chased.

### Pain points (current state)
- Site information lives across WhatsApp, Photos and inboxes.
- Attendance tracking is a separate Excel / Dashpivot / calendar.
- No personal view of progress against the BoQ.

### Owns these workflows
- 06 (Site Reporting & Progress). Capture / source role on 12 (Payroll — timesheets), 15 (Materials & Deliveries — site requests and goods-in), 19 (Fleet — driver of record).

### Permissions needed (coarse)
- Write on site reports, photos, attendance, snags. Read drawings and BoQ sections for their assigned project.

### Devices & environment
- Phone primarily. Touch-friendly, offline-tolerant capture is essential.

---

## P06 — Brand & Content

**Role:** Marketing and brand custodian across Jewel entities
**Reports to:** Directors
**Tooling today:** Canva, Meta Business, LinkedIn, SharePoint / OneDrive
**Frequency on platform:** Weekly
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Run the content calendar from current project data, not screenshot hunts.
- Get Director sign-off recorded against each post before publish.

### Pain points (current state)
- Brand assets duplicated across folders.
- Eligibility-for-content and client consent are tribal knowledge.

### Owns these workflows
- 20 (Marketing & Brand).

### Permissions needed (coarse)
- Read on project completion + consent flags. Write on content items and brand assets.

### Devices & environment
- Desktop + mobile.

### Notes
- The JewelBB brand voice and palette are encoded in the `jewelbb-brand-voice` Cowork skill — that's where copy and design briefs are reviewed against the brand system. JPMS only needs to surface the consent flag and asset-library link.

---

## P07 — Finance Director (FD)

**Role:** Internal owner of finance across BB / PS / PFP
**Reports to:** Directors / MD
**Tooling today:** Dext, Xero, Brightpay, Chaser HQ, online banking, Outlook, Excel
**Frequency on platform:** Daily — heavy
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals — **the platform's primary driver**
- Produce an accurate cashflow forecast across entities without rebuilding it weekly in Excel.
- Spend time on judgement and intervention, not data assembly.
- Catch subcontractor invoice errors before payment, not after.
- Ring-fence incoming cash to the correct project; time cash calls to actual % completion.
- Get out of the IT-helpdesk role.

### Pain points (current state)
- Forecast accuracy depends on knowing real completion %, which is currently unreliable because line-item completion isn't tracked consistently.
- AP coding and matching consumes ~80 h/month — the single largest workflow in the audit.
- Accounts inbox triage consumes ~60 h/month.
- Cashflow forecast rebuilt weekly in Excel; cross-entity charges manual.
- Currently doubling as IT support (~50 h/month) at the cost of finance focus.

### Owns these workflows
- 09 (Accounts Payable), 10 (Accounts Receivable), 11 (Cashflow & Management Reporting), 12 (Payroll), 13 (Accounts Inbox Triage). Approver on 16 (IT access gate). Current owner — target outsourced — on 17 (IT support).

### Permissions needed (coarse)
- Owner on AP, AR, payroll, cashflow dashboard. Approver on payment runs up to threshold. Allocate received funds against projects. Read across all projects.

### Devices & environment
- Desktop primary. Mobile for approvals.

### Notes
- This is the **litmus test** for every scoping decision: if a proposed feature doesn't make the FD's cashflow forecast more accurate or remove inbox / AP load, ask whether it belongs in phase one.

---

## P08 — Directors / MD

**Role:** Business owners — executive decisions across BB / PS / PFP
**Reports to:** —
**Tooling today:** Email, Teams, reports from FD
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- See cashflow position independently, in real time, without asking the FD.
- Sign-off on high-value commercial decisions in-system with audit trail.
- See at-a-glance status across the project portfolio.
- Identify at-risk projects early.

### Pain points (current state)
- Cashflow visibility lives in the FD's head until Excel is updated.
- Sign-offs scattered across email; no single audit trail.
- Pipeline and active-project visibility is fragmented across emails and spreadsheets.

### Owns these workflows
- Approver on 03 (high-value award), 04 (high-value variations), 09 (above-threshold payments), 10 (high-value AR), 11 (strategic cashflow decisions), 16 (confirm starter / leaver), 17 (IT governance), 18 (annual compliance review), 20 (brand sign-off). Read on all.

### Permissions needed (coarse)
- Read all. Approve high-value commercial decisions. Admin on users, roles, integrations.

### Devices & environment
- Mobile + desktop.

---

## P09 — Outsourced IT Helpdesk

**Role:** External tier-1 IT support partner — target owner of workflow 17
**Reports to:** Finance Director (governance only)
**Tooling today:** Their own ticketing system + M365 Admin / Entra / Intune access
**Frequency on platform:** Daily — limited surface
**Status:** Draft (provider not yet selected)
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Resolve tier-1 IT issues from staff without involving the FD.
- Execute provisioning from JPMS workflow 16 triggers.

### Pain points (current state)
- N/A — the role doesn't exist yet; today the FD does it.

### Owns these workflows
- 17 (IT & Systems Administration) — target. Contributor on 16 (provisioning execution).

### Permissions needed (coarse)
- Scoped admin on M365 Admin / Entra / Intune. No access to commercial or project data inside JPMS.

### Devices & environment
- Their own.

### Notes
- This is an **external partner** role. The JPMS surface should be deliberately narrow — provisioning hooks and audit reports only — to keep their access scope minimal.
