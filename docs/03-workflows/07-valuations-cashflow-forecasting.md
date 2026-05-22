# Workflow 07 — Cashflow & Project Forecasting

**Group:** Project lifecycle (output)
**Purpose:** Produce a live cashflow forecast for the project portfolio, built **purely from JPMS project data** — work-order commitments, expected valuations per Claim Period, predicted completion %s, and retention timing. One of the two primary outputs of JPMS (the other is the Programme Valuation Report from workflow 05).
**Trigger:** Continuous — refreshed whenever upstream project data changes (new work order, approved variation, valuation issued, site % updated, timesheet approved).
**Frequency:** Live; daily review; weekly deep dive.
**Owner (target):** Finance Director (review and judgement); JPMS for the data assembly.
**Status:** Draft

---

## Scope rule for this workflow

The cashflow forecast in JPMS is built from **project data inside JPMS only**. It does **not** pull from Xero, Brightpay, Dext, or any accountancy system. The forecast is the JPMS view of what the project portfolio is going to bring in (approved AR-eligible valuations) and what it's going to cost (work-order commitments, approved timesheets, retention). The accountancy team uses this forecast plus their own ledger view to manage actual cash — that bookkeeping work is downstream and outside JPMS.

---

## Current state

1. FD rebuilds the cashflow tracker in Excel weekly from scattered sources (Xero balances, Brightpay payroll runs, the FD's mental model of project commitments).
2. Cross-entity charges between BB / PS / PFP are reconciled manually in a separate sheet.
3. Management view exists only in the FD's head until the Excel is updated.

---

## Target flow

1. **Live forecast assembled from JPMS data alone:**
   - **Forward commitments out** — open work orders by expected payment date (workflow 03), retention release dates from open close-outs (workflow 08), open subcontractor day-rate hours not yet invoiced (workflow 07).
   - **Forward income in** — Programme Valuation Report draft for the next Claim Period (workflow 05), approved variations awaiting client sign-off (workflow 05), settlement payments at project close (workflow 08).
   - **Completion-% prediction** — combines site-reported % (workflow 06) with timesheet-vs-budget burn rate (workflow 07) to give an expected close date per project.
2. **Cross-entity flag** on every commitment and expected income line so a consolidated and per-entity view both work.
3. **FD view** — full dashboard with drill-down to the underlying project record.
4. **Directors / MD view** — scoped read with the same totals, no drill-down into individual subcontractor data.
5. **Architect / Client view (where contract provides)** — scoped to their own project: programme valuation, completion %, expected close.

---

## JPMS functionality required

- Forecast engine that assembles forward commitments out and forward income in from workflows 03, 04, 05, 06, 07, 09, 11.
- Cross-entity flag on every commitment and income line.
- Completion-% prediction model — site-reported % blended with timesheet burn rate.
- Three scoped dashboards: FD (full), Directors / MD (scoped), Architect / Client (per project, optional).
- Snapshot per Claim Period so a historical series is retained.

---

## Inputs (all from JPMS)

- Work-order register (workflow 03) — expected payment dates and amounts.
- Approved variations (workflow 05) — adds to expected income / commitments.
- Programme Valuation Report (workflow 05) — current and next Claim Period.
- Site reports (workflow 06) — completion % per BoQ section.
- Defect register and retention timing (workflow 08).
- Approved timesheets (workflow 07) — labour burn vs cost-code budget.
- Settlement records (workflow 08) — final values at project close.

## Outputs (consumed downstream)

- **Accountancy team** uses the forecast alongside their Xero ledger view to manage actual cash, run AP / AR, and time payment runs. That bookkeeping work is outside JPMS.
- **Directors / MD** read the dashboard directly in JPMS.
- **Client (where contract provides)** sees the project-scoped slice.

---

## User stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-01 | P02 Finance Director | As a Finance Director, I want a live 13-week rolling cashflow dashboard built from JPMS project data alone, so that I'm no longer rebuilding it in Excel every week. | Drafted |
| US-07-02 | P02 Finance Director | As a Finance Director, I want to filter the cashflow view by entity (BB / PS / PFP / Consolidated), so that the view matches the question I'm asking. | Drafted |
| US-07-03 | P02 Finance Director | As a Finance Director, I want every commitment and expected income line to carry a cross-entity flag at source, so that cross-entity charges are accurate without month-end correction. | Drafted |
| US-07-04 | P02 Finance Director | As a Finance Director, I want an items-needing-attention queue (sliding completion %, shifting WO payment dates, late valuations, pending retention release), so that my attention goes to the exceptions instead of the whole portfolio. | Drafted |
| US-07-05 | P02 Finance Director | As a Finance Director, I want to drill from a forecast number into the underlying project record in one click, so that I can investigate the root of a movement without leaving the dashboard. | Drafted |
| US-07-06 | P02 Finance Director | As a Finance Director, I want a stress-test slider — "what if completion % slips by N%?" or "what if the next valuation is approved a week late?" — that re-runs the projection without changing source data, so that I can model risk in the moment. | Drafted |
| US-07-07 | JPMS (system) | As JPMS, I want a snapshot of the cashflow forecast retained per Claim Period, so that historical comparison is possible later. | Drafted |
| US-07-08 | JPMS (system) | As JPMS, I want completion-% prediction to blend site-reported % (workflow 06) with timesheet-vs-budget burn rate (workflow 07), so that the forecast reflects both reported and observed progress. | Drafted |
| US-07-09 | P01 Directors / MD | As a Director, I want a scoped dashboard with the same totals as the FD but no FD-level intervention controls, so that I can read the cash position any time without asking the FD. | Drafted |
| US-07-10 | P08 Architect | As an architect / client (where the contract provides), I want a project-scoped view that mirrors the in-team view of valuation, completion % and expected close, so that I have the same picture they do. | Drafted |
| US-07-11 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to see the cashflow slice for my own projects, so that I know where I stand commercially without asking the FD. | Drafted |
| US-07-12 | P05 Site Team | As a site team member, I want to log a daily timesheet entry from my phone with project + date + hours, so that capture takes seconds and I don't get chased. | Drafted |
| US-07-13 | P02 Subcontractor | As a subcontractor on day-rate, I want to submit my hours through the portal against the right project, so that the accountancy team can reconcile my invoice against approved hours downstream. | Drafted |
| US-07-14 | P05 Site Team | As a site team member, I want to allocate each timesheet entry to a cost code with remaining budget visible inline, so that I know what I'm spending against what's left. | Drafted |
| US-07-15 | JPMS (system) | As JPMS, I want to block an allocation against a cost code with 0 remaining budget and offer exactly two paths — raise a Work Order against that cost code, or re-allocate to a different cost code — so that the budget rule is enforced at source. | Drafted |
| US-07-16 | P05 Site Team | As a site team member, when budget hits zero I want a one-click "raise WO" action that hands off to workflow 03 with the cost code pre-filled, so that I'm not bouncing between screens to unblock myself. | Drafted |
| US-07-17 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want a weekly bulk-approval surface for the project's timesheet batch, so that approval is one screen rather than a spreadsheet round-up. | Drafted |
| US-07-18 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want every allocation, re-allocation, approval and budget-override decision audit-logged, so that the trail is defensible later. | Drafted |
| US-07-19 | P01 Architect | As an architect / client (where the contract provides), I want to see approved timesheet totals per cost code in the client portal, so that I have the same view as the project team. | Drafted |
| US-07-20 | P06 Finance Director | As a Finance Director, I want to approve cost-code overrun above the agreed threshold, so that overruns get visibility before they multiply. | Drafted |
| US-07-21 | P07 Directors / MD | As a Director, I want to approve overrun above the higher threshold, so that material commitments are signed off at the right level. | Drafted |
| US-07-22 | JPMS (system) | As JPMS, I want to publish approved subcontractor day-rate hours to the accountancy team in a clean shape, so that they can match invoices in Xero downstream without re-keying. | Drafted |
| US-07-23 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want the project programme to live in JPMS as a Gantt-style view tied to BoQ line items, so that progress flows through automatically from site reporting. | Drafted |
| US-07-24 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to define the Claim Period at contract setup (default monthly, overridable per contract), so that each project's valuation cadence matches its contract. | Drafted |
| US-07-25 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want JPMS to auto-generate the Programme Valuation Report each Claim Period from contract + variations + current %, so that I'm reviewing rather than rebuilding the valuation each month. | Drafted |
| US-07-26 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want the valuation report to roll up approved Variations automatically, so that nothing in scope is missed at billing time. | Drafted |
| US-07-27 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to add narrative commentary to the auto-generated valuation before issuing, so that the client gets context as well as numbers. | Drafted |
| US-07-28 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want to issue the Programme Valuation Report from JPMS as a styled PDF and into the client portal, so that the same approved version reaches the architect/CA every time. | Drafted |
| US-07-29 | P01 Architect | As an architect/CA, I want to receive each period's Programme Valuation Report with the Claim Value for that period stated up front, so that I can review the period's claim without piecing it together. | Drafted |
| US-07-30 | P03 Project & Commercial Lead | As a Project & Commercial Lead, I want the historic valuation series per project with a prior-period diff on every line, so that I can defend any changes when the architect challenges them. | Drafted |
| US-07-31 | P07 Directors / MD | As a Director, I want to approve each Programme Valuation Report before it's issued, so that high-value valuations aren't released without sign-off. | Drafted |
| US-07-32 | JPMS (system) | As JPMS, I want approved valuations and their Claim Values to be published cleanly so the accountancy team can raise AR invoices in Xero downstream without re-keying. | Drafted |

Covers spreadsheet row 32 (track in-house costs and cross-charge costs between entities / projects).

---

## Acceptance criteria — "done looks like"

- The cashflow forecast is a live dashboard built from JPMS data, not an Excel rebuild.
- Forecast accuracy improves because completion % is real, not guessed.
- Cross-entity views (consolidated / per-entity) are accurate without month-end manual correction.
- Directors view the cash position any time without asking the FD.

---

## Entities touched

`Cashflow Forecast` · `Project` · `Work Order` · `Variation` · `Valuation` · `Claim Period` · `Site Report` · `Timesheet Approval` · `Cost Code Budget` · `Settlement Record`

See [`/docs/data-models/entity-relationship.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P08 Architect | Read — project-scoped dashboard where the contract provides |
| P03 Project & Commercial Lead | Contributor — read the project slice; trigger refresh on valuation / WO completion |
| P02 Finance Director | **Owner** — review, judgement, intervention |
| P01 Directors / MD | Approver — strategic cashflow decisions |

See [`/docs/requirements/permission-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Forecast horizon — 13-week rolling? Longer for some audiences?
- [ ] Completion-% prediction model — purely site-reported, purely timesheet-vs-budget, or a blend? Affects forecast accuracy.
- [ ] Cross-entity reporting — separate dashboards per entity, or one consolidated view with filters?
- [ ] Architect / Client scoped view — opt-in per contract, or default for all client-facing projects?
- [ ] Snapshot cadence — does JPMS retain a full forecast snapshot per Claim Period for historical comparison?

---

## Confirmation checklist

- [ ] Walked through end-to-end with the Finance Director
- [ ] Forecast horizon and refresh cadence agreed
- [ ] Completion-% prediction model agreed
- [ ] Cross-entity view confirmed
- [ ] Director scoped view confirmed
- [ ] Architect / Client view confirmed against at least one client contract
- [ ] Permissions confirmed
- [ ] Signed off
