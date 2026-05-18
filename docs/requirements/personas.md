# Personas

Five primary user roles for the initial **Project Program Scheduler**. Each is in **Draft** status — a persona is only **Confirmed** once the named person in that role has reviewed and agreed the card matches their day-to-day.

The card template lives in [`_templates/personas-template.md`](_templates/personas-template.md).

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
