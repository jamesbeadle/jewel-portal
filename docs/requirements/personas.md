# Personas

The initial **Project Program Scheduler** (production brand: **JPMS**) is scoped against twelve user roles. The first five (P01–P05) were drafted from the 2026-05-18 domain discovery and frame the customer-and-craft side of the platform. P06–P12 were added on 2026-05-20 from the JBB workflow audit and represent the operational roles that own the twenty-one workflows (see [`/docs/workflows/`](../workflows/)).

Each card is **Draft** until the named person in that role has reviewed and agreed it matches their day-to-day.

The card template lives in [`_templates/personas-template.md`](_templates/personas-template.md). The Role × Workflow RBAC matrix lives in [`permission-matrix.md`](permission-matrix.md).

### Persona register

| # | Persona | Type | Sourced from |
|---|---|---|---|
| P01 | Architect | External client | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P02 | Quantity Surveyor (QS) | Internal / external specialist | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P03 | Subcontractor | External delivery partner | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P04 | Accountant _(now folds into P10 Finance Director)_ | Internal | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P05 | Managing Director (MD) _(now part of P11 Directors)_ | Internal executive | [2026-05-18](../meetings/2026-05-18-domain-discovery.md) |
| P06 | Project & Commercial Lead | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P07 | Office & Compliance Coordinator | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P08 | Site Team | Internal field | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P09 | Brand & Content | Internal | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P10 | Finance Director (FD) | Internal executive | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P11 | Directors / MD | Internal executive | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |
| P12 | Outsourced IT Helpdesk | External service partner | [2026-05-20](../meetings/2026-05-20-jbb-workflow-audit.md) |

> **Reconciliation note.** P04 Accountant and P05 MD were drafted before the workflow audit. The audit clarified that on the JBB operational structure these roles are better named **Finance Director** (P10) and **Directors / MD** (P11) respectively. The original P04/P05 cards are kept as historical anchors and inherit their goals/pain-points into the new cards; future updates land on P10/P11 first.

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

### Key journeys they participate in
- _To be captured in discovery._ Candidates: submitting a tender, reviewing / approving a VO, signing off completion against cost codes.

### Permissions needed
- _High level — feeds the permission matrix._ Likely: read on their own tenders and projects; write on tender submission; approve / reject VOs on their work; read on cash-call documentation for their projects.

### Devices & environment
- Desktop primarily. May use tablet on site visits.

### Notes
- The architect's client-facing **cost codes** must be referenced consistently through every screen and report that touches that architect's tender. This is a cross-cutting requirement, not specific to one journey.

---

## P02 — Quantity Surveyor (QS)

**Role:** Pricing and measurement specialist
**Reports to:** Managing Director _(to confirm)_
**Tooling today:** Spreadsheets, measurement tools, drawing review software
**Frequency on platform:** Daily during tender phase; periodic during projects when VOs are raised
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- Produce accurate line-by-line pricing for tenders.
- Capture site measurements quickly and accurately.
- Keep line items in sync as scope changes via VOs.

### Pain points (current state)
- Line items live in spreadsheets disconnected from completion tracking and billing.
- Manual reconciliation between tender pricing and actual work delivered.

### Key journeys they participate in
- _To be captured in discovery._ Candidates: pricing a new tender from drawings, a measurement site visit, updating line items in response to a VO.

### Permissions needed
- Create / edit tender line items. Initiate VOs. Read access across the project portfolio.

### Devices & environment
- Laptop in office, tablet on site.

### Notes
- Whether the QS is internal Jewel staff or an external consultant needs confirming. The platform may need to support both.

---

## P03 — Subcontractor

**Role:** Field worker delivering work against tender line items
**Reports to:** _to confirm_ — likely site manager or QS
**Tooling today:** Phone, paper, ad-hoc messaging
**Frequency on platform:** Daily — on site
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- Quickly log progress and time on site.
- Get RFIs answered fast so work isn't blocked.
- Get paid on time and accurately.

### Pain points (current state)
- Paper timesheets get lost or delayed.
- RFI handling is slow and untracked.
- No personal view of their progress against the tender.

### Key journeys they participate in
- _To be captured in discovery._ Candidates: updating line-item completion, submitting a timesheet, raising an RFI, actioning a VO once approved.

### Permissions needed
- Update completion on line items assigned to them. Submit timesheets. Raise RFIs. Read drawings and specs for their assigned work.

### Devices & environment
- Mobile phone primarily. Touch-friendly UI is essential, including in poor connectivity (offline-tolerant fields where possible).

### Notes
- Subcontractors are external to Jewel Enterprises. Onboarding needs to be lightweight — probably an invite link rather than a full account-creation flow.

---

## P04 — Accountant

**Role:** On-site accountant — owns financial visibility for the business
**Reports to:** Managing Director
**Tooling today:** Excel; an accounting package _(to confirm which)_
**Frequency on platform:** Daily / weekly
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- Produce an accurate cashflow forecast despite fast-moving project data.
- Ring-fence incoming cash to the correct project.
- Time cash calls to actual % completion so projects stay funded.

### Pain points (current state) — **the platform's primary driver**
- Forecast accuracy depends on knowing real completion %, which is currently unreliable because line-item completion isn't tracked consistently.
- Incoming cash sometimes isn't clearly allocated to a job → projects get mis-funded.
- Cash calls misaligned with completion → projects get under-funded and interrupted.

### Key journeys they participate in
- _To be captured in discovery._ Candidates: producing a cashflow forecast, issuing a cash call against a project, allocating incoming cash to projects.

### Permissions needed
- Read across all projects, line items, completion %, VO status. Create cash calls. Allocate received funds against projects.

### Devices & environment
- Desktop primary. Tablet for ad-hoc.

### Notes
- The Accountant persona is the **litmus test for every scoping decision**. If a proposed feature doesn't help the Accountant produce an accurate forecast, ask whether it belongs in the initial scope.

---

## P05 — Managing Director (MD)

**Role:** Business owner — executive decisions across all projects
**Reports to:** —
**Tooling today:** Email, Teams, reports from the Accountant
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-18-domain-discovery.md`](../meetings/2026-05-18-domain-discovery.md)

### Goals
- At-a-glance view of business health.
- Confidence in the cashflow forecast.
- Identify at-risk projects early.

### Pain points (current state)
- Decisions rely on lagging or inaccurate cashflow data.
- Pipeline and active-project visibility is fragmented across emails and spreadsheets.

### Key journeys they participate in
- _To be captured in discovery._ Candidates: reviewing the cashflow forecast, sign-off on major commercial decisions, opening / approving a new project.

### Permissions needed
- Read all. Approve high-value commercial decisions. Admin access to the platform (users, roles, integrations).

### Devices & environment
- Mobile + desktop.

---

## P06 — Project & Commercial Lead

**Role:** Internal project lead combining PM and commercial responsibilities — owns the project from BoQ through close-out
**Reports to:** Directors / MD
**Tooling today:** Bluebeam, MS Project, Excel BoQ, Buildertrend, Planyard, SharePoint, Outlook
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Run the project programme and valuation cycle cleanly without manual reconciliation between MS Project and Excel.
- Make award decisions on bids and variations with side-by-side data, not assembled spreadsheets.
- Stop being the human bridge between drawings, BoQ, RFIs, and the programme.

### Pain points (current state)
- Programme and valuation reconciliation is manual and slow.
- Three separate Excel logs for variations, RFIs and delay notices that don't talk to each other.
- Bid packages are assembled by hand from SharePoint/Outlook/Excel.
- Drawing supersedure relies on memory.

### Owns these workflows
- 02 (Tender & BoQ), 03 (Procurement), 04 (Variations/RFIs/Delays), 05 (Programme & Valuations), 07 (Close-Out & Defects). Reviewer on 01 and 06.

### Permissions needed (coarse)
- Owner on project records and BoQ. Approver on bids, variations, valuations, defect sign-off. Read across all projects.

### Devices & environment
- Laptop primarily; tablet on site.

### Notes
- This role absorbs much of what the original P02 QS persona was assumed to do internally. Where Jewel uses an external QS, P02 remains the external-consultant view.

---

## P07 — Office & Compliance Coordinator

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
- Subbie compliance scattered across Monday, SharePoint and Excel.
- Insurance/accreditation renewals tracked in Outlook calendar and chased manually.
- Material orders raised via WhatsApp/email with no project linkage.
- Document filing in SharePoint is a never-ending tidy-up task.

### Owns these workflows
- 08 (Subbie Compliance), 14 (Client & Reactive Comms), 15 (Materials & Deliveries), 18 (Compliance/Insurance/Accreditation), 19 (Fleet), 21 (Document Management). Co-owner on 16 (HR/Onboarding).

### Permissions needed (coarse)
- Owner on supplier & subbie directory, compliance register, fleet register, procurement requests. Approver on routine renewals.

### Devices & environment
- Desktop primarily.

---

## P08 — Site Team

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
- Attendance tracking is a separate Excel/Dashpivot/calendar.
- No personal view of progress against the BoQ.

### Owns these workflows
- 06 (Site Reporting & Progress). Capture/source role on 12 (Payroll), 15 (Materials & Deliveries), 19 (Fleet — driver of record).

### Permissions needed (coarse)
- Write on site reports, photos, attendance, snags. Read drawings and BoQ sections for their assigned project.

### Devices & environment
- Phone primarily. Touch-friendly, offline-tolerant capture is essential.

---

## P09 — Brand & Content

**Role:** Marketing and brand custodian across Jewel entities
**Reports to:** Directors
**Tooling today:** Canva, Meta Business, LinkedIn, SharePoint/OneDrive
**Frequency on platform:** Weekly
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Run the content calendar from current project data, not screenshot hunts.
- Get director sign-off recorded against each post before publish.

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

## P10 — Finance Director (FD)

**Role:** Owner of finance across BB/PS/PFP — replaces and broadens P04 Accountant
**Reports to:** Directors / MD
**Tooling today:** Dext, Xero, Brightpay, Chaser HQ, online banking, Outlook, Excel
**Frequency on platform:** Daily — heavy
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals — **the platform's primary driver**
- Spend time on judgement and intervention, not data assembly.
- Hold an accurate cashflow forecast across entities without rebuilding it weekly in Excel.
- Catch subbie invoice errors before payment, not after.
- Get out of the IT-helpdesk role.

### Pain points (current state)
- AP coding and matching consumes ~80 h/month — the single largest workflow.
- Accounts inbox triage consumes ~60 h/month.
- Cashflow forecast rebuilt weekly in Excel; cross-entity charges manual.
- Currently doubling as IT support (~50 h/month) at the cost of finance focus.

### Owns these workflows
- 09 (AP), 10 (AR), 11 (Cashflow & Management Reporting), 12 (Payroll), 13 (Accounts Inbox Triage). Approver on 16 (IT access gate). Current owner — target outsourced — on 17 (IT support).

### Permissions needed (coarse)
- Owner on AP, AR, payroll, cashflow dashboard. Approver on payment runs above threshold. Read across all projects.

### Devices & environment
- Desktop primary. Mobile for approvals.

### Notes
- This is the **litmus test** for every scoping decision: if a proposed feature doesn't make the FD's cashflow forecast more accurate or remove inbox/AP load, ask whether it belongs in phase one.

---

## P11 — Directors / MD

**Role:** Business owners — executive decisions across BB/PS/PFP. Replaces and broadens P05 MD.
**Reports to:** —
**Tooling today:** Email, Teams, reports from FD
**Frequency on platform:** Daily
**Status:** Draft
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- See cashflow position independently, in real time, without asking FD.
- Sign-off on high-value commercial decisions in-system with audit trail.
- See at-a-glance status across the project portfolio.

### Pain points (current state)
- Cashflow visibility lives in FD's head until Excel is updated.
- Sign-offs scattered across email; no single audit trail.

### Owns these workflows
- Approver on 09, 10, 11, 16 (HR/IT), 17 (IT governance), 20 (Brand sign-off). Read on all.

### Permissions needed (coarse)
- Read all. Approve high-value commercial decisions. Admin on users, roles, integrations.

### Devices & environment
- Mobile + desktop.

---

## P12 — Outsourced IT Helpdesk

**Role:** External tier-1 IT support partner — target owner of workflow 17
**Reports to:** Finance Director (governance only)
**Tooling today:** Their own ticketing system + M365 Admin / Entra / Intune access
**Frequency on platform:** Daily — limited surface
**Status:** Draft (provider not yet selected)
**Reviewed by:** —
**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

### Goals
- Resolve tier-1 IT issues from JBB staff without involving the FD.
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
