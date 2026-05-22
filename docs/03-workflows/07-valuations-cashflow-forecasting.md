# Workflow 07 — Valuations, Cashflow & Forecasting

**Lifecycle stage:** 07 — the commercial-outputs stage. The point where everything captured upstream becomes the three financial documents that run the business.
**Purpose:** Produce the three commercial outputs JPMS exists to deliver, each from project data alone, each owned by the right person.
**Trigger:** Continuous — refreshed whenever upstream project data changes (work order awarded, variation approved, valuation issued, site % updated, timesheet approved, defect signed off).
**Frequency:** Live; monthly Claim-Period cycle for the Programme Valuation Report and CVR; weekly for cashflow review; daily for cashflow dashboard.
**Owner (target):** **QS / Estimator (P04)** for the Programme Valuation Report and the CVR; **Finance Director (P02)** for the cashflow forecast and cost-code overrun gate; **PM (P03)** for the timesheet weekly batch approval.
**Status:** Draft

---

## The three outputs

This workflow produces **three** primary outputs. Each is a first-class deliverable; each has a clear audience.

| Output | Audience | What it answers |
|---|---|---|
| **Programme Valuation Report (PVR)** | Client / Architect / CA (external) | What is the client paying for this Claim Period? |
| **CVR — Cost-Value Reconciliation** | QS, PM, FD, Directors (internal commercial control) | What does the project actually look like commercially — actual vs forecast vs tender — per package, with margin, with prelims and EOTs visible separately, with variations against original tender headings? |
| **Cashflow Forecast** | FD, Directors (internal exec) | What's the live cash position across the project portfolio? |

The PVR is what goes to the client. The CVR is what the QS and the PM live inside. The cashflow forecast is what the FD and Directors steer the business with.

JPMS replaces Planyard (and the Excel CVR workbooks JBB use today) on this surface. See [`/05-data-model/integrations.md`](../05-data-model/integrations.md).

---

## Scope rule

All three outputs are built from **project data inside JPMS only**. No source-merging from Xero, Brightpay, Dext or any accountancy system. The accountancy team uses these outputs plus their own ledger view to manage actual cash; that bookkeeping work is downstream and outside JPMS.

---

## Current state

1. PVR rebuilt manually each Claim Period from Excel valuation sheets.
2. CVR rebuilt monthly in a separate Excel workbook (see "By France" CVR — old and new pilot). The new pilot loses three things the old CVR had: clear forecast traceability (QS Accruals + Prelim Forecast), prelims and EOTs visible against tender separately, and variations shown per BoQ package rather than only on a central register.
3. Cashflow rebuilt weekly in a separate Excel by the FD.
4. The three live in three different artefacts with no shared source of truth; reconciling them is manual.

---

## Target flow

### 7.A — The Programme Valuation Report (PVR)

1. **Per Claim Period** (defined at contract setup — typically monthly, overridable per contract), JPMS auto-assembles the PVR: contract value + approved variations + current % per BoQ line item, with **Claim Value** totals (period + cumulative).
2. **QS reviews and adjusts** (judgement on % complete, narrative commentary).
3. **PM and Director approve** before issuance.
4. **Issued** from JPMS as a styled PDF to the client / CA, or via the client portal.
5. **Historic series** retained per project with prior-period diff on every line.

### 7.B — The CVR (Cost-Value Reconciliation) — done right

Built from the same project data as the PVR, but framed for internal commercial control. JPMS CVR is the answer to "is this project making margin and where is it leaking?"

The CVR explicitly fixes the three issues JBB called out on their pilot workbook (recorded in [`/00-business-context/meetings/2026-05-23-cvr-alignment.md`](../00-business-context/meetings/2026-05-23-cvr-alignment.md)):

**Fix #1 — Forecast made up of traceable components**

The Forecast Final Cost is never a single opaque number. It is the sum of explicit components, each clickable to its source:

- **Cost Incurred** — what's been spent (work-order invoices applied, day-rate timesheets approved, dayworks, contras, retention movements).
- **Cost Committed** — open work orders not yet invoiced.
- **QS Accruals** — explicit QS judgement adjustments per category (Add / Omit / Liability). Replicates the old CVR's QS Accruals sheet. Each accrual has a description, a value and a sign-off.
- **Prelim Forecast** — week-by-week prelim spend per item with Tendered vs Actual and the Difference per item. Replicates the old CVR's Prelim Forcast sheet.
- **Cost to Complete** — remaining contract scope × current rate-card position.

Hovering or clicking the Forecast Final Cost on any package drills into the four-component breakdown above. No black-box numbers.

**Fix #2 — Prelims and EOTs visible against tender separately**

- **Prelims** are a distinct CVR section above / outside the BoQ package list. Each prelim item shows: Tendered £, Actual £ to date, Forecast £ to completion, Difference.
- **Time control header** shows Contract Programme dates, Extension of Time count, Total Period, Contract Completion vs Anticipated Completion, **Weeks Ahead / Behind**.
- **Time-related prelim overspend** is calculated automatically: weeks late × weekly prelim run rate, surfaced as a distinct line so the cause of the prelim overspend is visible.
- **EOT register** holds each Extension of Time with reason, period granted, programme impact, and any commercial recovery position.

**Fix #3 — Variations against original BoQ headings AND on a central register**

Both views are surfaced from the same underlying variation data — it isn't either/or:

- **Central Variations Register** — list of all variations with status, gross value, items, % certified, certified £, notes (existing workflow 05 register).
- **Per-package variation view inside CVR** — every package row shows: Order Cost / Order Value / Order Profit £ / Order Profit % | Variation Cost / Variation Value / Variation Profit £ / Variation Profit % | Combined Cost / Combined Value / Combined Profit £ / Combined Profit %. Replicates the old CVR Sub Contract Analysis structure so margin per package is visible including variations.
- A variation always carries a **BoQ package / cost-code link** so it can roll up against its package.

### 7.C — The cashflow forecast

1. **Live 13-week rolling cashflow** assembled from JPMS data alone:
   - **Forward income in** — Programme Valuation Report drafts for upcoming Claim Periods (7.A), approved variations awaiting client sign-off (workflow 05), settlement payments at project close (workflow 08).
   - **Forward commitments out** — open work orders by expected payment date (workflow 03), open subcontractor day-rate hours not yet invoiced, retention release dates from open close-outs (workflow 08).
   - **Completion-% prediction** — blends site-reported % (workflow 06) with timesheet-vs-budget burn rate (7.D) to give an expected close date per project.
2. **Cross-entity flag** on every commitment and income line.
3. **FD view** — full dashboard with drill-down.
4. **Directors / MD view** — scoped read with the same totals, no FD-level intervention controls.
5. **Architect / Client view (where contract provides)** — project-scoped slice.

### 7.D — Timesheets and cost-code allocation

Site team and subcontractor day-rate timesheets are captured here (cross-cutting from the site app in workflow 06) and allocated to cost codes. The **cost-code budget hard-block rule** prevents allocation to a cost code with no remaining budget unless a Work Order is raised against it (routes to workflow 03) or the entry is re-allocated to a cost code with available budget.

Approved timesheets feed:
- The CVR's Cost Incurred component.
- The cashflow forecast's labour commitment.
- The completion-% prediction.

---

## JPMS functionality required

### PVR
- Claim Period as first-class concept at contract setup.
- Auto-assembly per Claim Period from contract + variations + % complete.
- Variation roll-up.
- Approval and issuance workflow.
- Styled PDF + portal issuance.
- Historic valuation series with prior-period diff per line.

### CVR
- Per-project CVR view, refreshed live.
- **Forecast component breakdown** drill-down (Cost Incurred / Cost Committed / QS Accruals / Prelim Forecast / Cost to Complete).
- **QS Accruals module** — Add / Omit / Liability per category, with description, value, sign-off, audit trail.
- **Prelim Forecast module** — week × item grid with Tendered £, Actual £, Difference £ per item.
- **EOT register** per project (reason, period, programme impact, commercial position).
- **Time control header** — Contract dates, EOT count, Anticipated Completion, Weeks Ahead / Behind, time-related prelim overspend calculation.
- **Per-package variation view** — package row × (Order, Variation, Combined) × (Cost, Value, Profit £, Profit %).
- **Movement column** — £ change since prior CVR snapshot, so the conversation is "what moved this period?".
- Snapshot per Claim Period for trend analysis.

### Cashflow
- Forecast engine consuming events from workflows 03, 04, 05, 06, 07, 08.
- Cross-entity flag on every line.
- Three scoped dashboards (FD full, Directors scoped, Client project-scoped).
- Stress-test slider (what-if scenarios).
- Snapshot per Claim Period.

### Timesheets / cost-code allocation
- Mobile timesheet entry (site app handoff from workflow 06).
- Subcontractor portal entries (day-rate).
- Cost-code register per project with budget per code; remaining-budget inline.
- **Hard validation** — no allocation to 0-remaining cost code without a linked WO or re-allocation.
- Inline "raise WO" action handing off to workflow 03 with cost code pre-filled.
- Weekly bulk approval surface.
- Audit trail.

---

## Inputs (all from JPMS)

- Work-order register (workflow 03) — expected payment dates and amounts.
- Approved variations (workflow 05) — adds to expected income / commitments and to per-package CVR view.
- BoQ + rate library (workflow 02) — basis for tender lines and rate-card positions.
- Site reports (workflow 06) — completion % per BoQ section.
- Inspections / observations (workflow 04) — feed quality signal into CVR notes where relevant.
- Defect register and retention timing (workflow 08).
- Settlement records (workflow 08) — final values at project close.

## Outputs (consumed)

- **Client / Architect / CA** — receive the issued PVR per Claim Period.
- **PM, QS, FD, Directors** — view the live CVR; approve PVR; receive cashflow.
- **Accountancy team (downstream, out of JPMS)** — pull approved valuations into Xero for AR invoicing; pull approved timesheets into AP matching for subcontractor day-rate invoices; pull CVR snapshots for management reporting.

---

## User stories

### PVR stories (existing — renumbered from workflow 05)

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-01 | P04 QS | As a QS, I want the project programme to live in JPMS as a Gantt-style view tied to BoQ line items, so that progress flows through automatically from site reporting. | Drafted |
| US-07-02 | P04 QS | As a QS, I want to define the Claim Period at contract setup (default monthly, overridable per contract), so that each project's valuation cadence matches its contract. | Drafted |
| US-07-03 | P04 QS | As a QS, I want JPMS to auto-generate the Programme Valuation Report each Claim Period from contract + variations + current %, so that I'm reviewing rather than rebuilding the valuation each month. | Drafted |
| US-07-04 | P04 QS | As a QS, I want the valuation report to roll up approved Variations automatically, so that nothing in scope is missed at billing time. | Drafted |
| US-07-05 | P04 QS | As a QS, I want to add narrative commentary to the auto-generated valuation before issuing, so that the client gets context as well as numbers. | Drafted |
| US-07-06 | P03 PM | As a PM, I want to approve and issue the Programme Valuation Report from JPMS as a styled PDF and into the client portal, so that the same approved version reaches the architect/CA every time. | Drafted |
| US-07-07 | P08 Architect | As an architect/CA, I want to receive each period's Programme Valuation Report with the Claim Value for that period stated up front, so that I can review the period's claim without piecing it together. | Drafted |
| US-07-08 | P04 QS | As a QS, I want the historic valuation series per project with a prior-period diff on every line, so that I can defend any changes when the architect challenges them. | Drafted |
| US-07-09 | P01 Director | As a Director, I want to approve each Programme Valuation Report before it's issued, so that high-value valuations aren't released without sign-off. | Drafted |
| US-07-10 | JPMS (system) | As JPMS, I want approved valuations and their Claim Values published cleanly so the accountancy team can raise AR invoices in Xero downstream without re-keying. | Drafted |

### Timesheets / cost-code allocation stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-12 | P05 Site Manager | As a site team member, I want to log a daily timesheet entry from my phone with project + date + hours, so that capture takes seconds and I don't get chased. | Drafted |
| US-07-13 | P10 Subcontractor | As a subcontractor on day-rate, I want to submit my hours through the portal against the right project, so that the accountancy team can reconcile my invoice against approved hours downstream. | Drafted |
| US-07-14 | P05 Site Manager | As a site team member, I want to allocate each timesheet entry to a cost code with remaining budget visible inline, so that I know what I'm spending against what's left. | Drafted |
| US-07-15 | JPMS (system) | As JPMS, I want to block an allocation against a cost code with 0 remaining budget and offer exactly two paths — raise a Work Order against that cost code, or re-allocate to a different cost code — so that the budget rule is enforced at source. | Drafted |
| US-07-16 | P05 Site Manager | As a site team member, when budget hits zero I want a one-click "raise WO" action that hands off to workflow 03 with the cost code pre-filled, so that I'm not bouncing between screens. | Drafted |
| US-07-17 | P03 PM | As a PM, I want a weekly bulk-approval surface for the project's timesheet batch, so that approval is one screen rather than a spreadsheet round-up. | Drafted |
| US-07-18 | P03 PM | As a PM, I want every allocation, re-allocation, approval and budget-override decision audit-logged, so that the trail is defensible later. | Drafted |
| US-07-19 | P08 Architect / P09 Client | As an architect / client (where the contract provides), I want to see approved timesheet totals per cost code in the client portal, so that I have the same view as the project team. | Drafted |
| US-07-20 | P02 FD | As an FD, I want to approve cost-code overrun above the agreed threshold, so that overruns get visibility before they multiply. | Drafted |
| US-07-21 | P01 Director | As a Director, I want to approve overrun above the higher threshold, so that material commitments are signed off at the right level. | Drafted |
| US-07-22 | JPMS (system) | As JPMS, I want to publish approved subcontractor day-rate hours to the accountancy team in a clean shape, so that they can match invoices in Xero downstream without re-keying. | Drafted |

### CVR stories — Forecast traceability (Fix #1)

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-23 | P04 QS | As a QS, I want every Forecast Final Cost number in the CVR to drill into its four components — Cost Incurred, Cost Committed, QS Accruals, Prelim Forecast, Cost to Complete — so that the forecast is never a black box. | Drafted |
| US-07-24 | P04 QS | As a QS, I want a QS Accruals module with Add / Omit / Liability entries per category, each with description, value, sign-off and audit trail, so that judgement adjustments are explicit and reviewable. | Drafted |
| US-07-25 | P04 QS | As a QS, I want approved Variations to feed Cost Committed automatically and only flow into Cost Incurred when invoiced, so that the components reflect real commercial status. | Drafted |
| US-07-26 | P03 PM | As a PM, I want a Movement column on the CVR showing £ change since the prior snapshot per line, so that monthly reviews are "what moved?" not "what's the absolute value?". | Drafted |
| US-07-27 | P04 QS | As a QS, I want a CVR snapshot retained per Claim Period, so that the trend per package is visible over time. | Drafted |

### CVR stories — Prelims and EOTs visible (Fix #2)

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-28 | P04 QS | As a QS, I want a Prelim Forecast module that's separate from the BoQ packages, with a week × item grid showing Tendered £, Actual £, Forecast £ and Difference per item, so that prelim overspend doesn't hide inside a general cost code. | Drafted |
| US-07-29 | P03 PM | As a PM, I want the CVR header to show Contract Programme dates, EOT count, Anticipated Completion vs Contract Completion, and Weeks Ahead / Behind, so that time control is visible alongside cost control. | Drafted |
| US-07-30 | P04 QS | As a QS, I want JPMS to calculate time-related prelim overspend automatically (weeks late × weekly prelim run rate) and surface it as a distinct line, so that the cause of prelim overspend is visible. | Drafted |
| US-07-31 | P03 PM | As a PM, I want an EOT register per project capturing reason, period granted, programme impact, and any commercial recovery position, so that EOTs are tracked rather than buried in correspondence. | Drafted |

### CVR stories — Variations against packages (Fix #3)

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-32 | P04 QS | As a QS, I want every Variation to carry a BoQ package / cost-code link, so that it can roll up against the right package as well as appearing on the central Variations Register. | Drafted |
| US-07-33 | P04 QS | As a QS, I want the per-package CVR view to show each package row as Order (Cost / Value / Profit £ / Profit %) plus Variation (Cost / Value / Profit £ / Profit %) plus Combined (Cost / Value / Profit £ / Profit %), so that I can see margin by package including variations. | Drafted |
| US-07-34 | P04 QS | As a QS, I want to flip the same data between the central Variations Register view and the per-package CVR view, so that one source of truth answers both questions. | Drafted |
| US-07-35 | P03 PM | As a PM, I want a portfolio CVR view summarising margin by project + by package, so that I can spot under-performing trades across projects (feeds workflow 09 Portfolio Analytics). | Drafted |

### Cashflow stories

| ID | Role | Story | Status |
|---|---|---|---|
| US-07-36 | P02 FD | As an FD, I want a live 13-week rolling cashflow dashboard built from JPMS project data alone, so that I'm no longer rebuilding it in Excel every week. | Drafted |
| US-07-37 | P02 FD | As an FD, I want to filter the cashflow view by entity (BB / PS / PFP / Consolidated), so that the view matches the question I'm asking. | Drafted |
| US-07-38 | P02 FD | As an FD, I want every commitment and expected income line to carry a cross-entity flag at source, so that cross-entity charges are accurate without month-end correction. | Drafted |
| US-07-39 | P02 FD | As an FD, I want an items-needing-attention queue (sliding completion %, shifting WO payment dates, late valuations, pending retention release), so that my attention goes to the exceptions instead of the whole portfolio. | Drafted |
| US-07-40 | P02 FD | As an FD, I want to drill from a forecast number into the underlying project record in one click, so that I can investigate the root of a movement without leaving the dashboard. | Drafted |
| US-07-41 | P02 FD | As an FD, I want a stress-test slider — "what if completion % slips by N%?" or "what if the next valuation is approved a week late?" — that re-runs the projection without changing source data, so that I can model risk in the moment. | Drafted |
| US-07-42 | JPMS (system) | As JPMS, I want a snapshot of the cashflow forecast retained per Claim Period, so that historical comparison is possible later. | Drafted |
| US-07-43 | JPMS (system) | As JPMS, I want completion-% prediction to blend site-reported % (workflow 06) with timesheet-vs-budget burn rate (workflow 07), so that the forecast reflects both reported and observed progress. | Drafted |
| US-07-44 | P01 Director | As a Director, I want a scoped dashboard with the same totals as the FD but no FD-level intervention controls, so that I can read the cash position any time without asking the FD. | Drafted |
| US-07-45 | P08 Architect / P09 Client | As an architect / client (where the contract provides), I want a project-scoped view that mirrors the in-team view of valuation, completion % and expected close, so that I have the same picture they do. | Drafted |
| US-07-46 | P03 PM | As a PM, I want to see the cashflow slice for my own projects, so that I know where I stand commercially without asking the FD. | Drafted |

---

## Acceptance criteria — "done looks like"

- All three outputs (PVR, CVR, Cashflow) come from the same project data; no separate source of truth.
- Monthly PVR takes minutes to review, not hours to rebuild.
- Every CVR Forecast Final Cost drills into its components (no black boxes).
- Prelims and EOTs are visible against tender separately, with weeks ahead / behind on the CVR header.
- Variations show against original BoQ packages on the CVR AND on the central register — both from the same underlying data.
- Cashflow is a live dashboard, not an Excel rebuild; forecast accuracy improves because completion % is real.
- Cost-code overrun is never silent — every overrun has an audited resolution (WO raised, re-allocation, or threshold approval).
- Approved valuations and timesheet day-rate hours publish cleanly for accountancy downstream.
- **Planyard is no longer required** — JPMS delivers the same surface plus the fixes James called out (forecast traceability, prelims / EOTs visibility, per-package variations).

---

## Entities touched

`Project` · `Tender` · `BoQ Line Item` · `Cost Code` · `Cost Code Budget` · `Cost Code Allocation` · `Work Order` · `Variation` · `Claim Period` · `Valuation` · `Programme Valuation Report` · **`CVR Snapshot`** · **`QS Accrual`** · **`Prelim Item`** · **`Prelim Forecast Entry`** · **`EOT (Extension of Time)`** · **`Forecast Component`** · **`Margin Trace`** · **`Daywork`** · **`Contra Charge`** · **`Subcontractor Retention`** · `Cashflow Forecast Snapshot` · `Timesheet` · `Timesheet Approval`

See [`/05-data-model/entities.md`](../05-data-model/entities.md).

---

## Roles involved (RBAC)

| Role | Involvement |
|---|---|
| P02 Finance Director | **Owner of Cashflow + cost-code overrun gate.** Approver on PVR release (above threshold). Reads the CVR live for portfolio risk. |
| P03 Project Manager | Approver on PVR issuance per project; contributor to CVR commentary; **owns timesheet weekly batch approval**. Drives cashflow on their projects via valuation cadence. |
| P04 QS / Estimator | **Owner of the PVR build and the CVR.** Owns QS Accruals, Prelim Forecast, EOT register, per-package margin view. |
| P05 Site Manager | Contributor — captures timesheet entries; signals completion % through workflow 06. |
| P10 Subcontractor | Contributor — day-rate timesheet entries via portal. |
| P11 Foreman / Site Team | Contributor — own timesheets. |
| P01 Director / MD | Approver on PVR release above threshold; approves cost-code overrun above higher threshold. Reads cashflow and CVR portfolio view. |
| P08 Architect | Receives the PVR per Claim Period. |
| P09 Client | Reads project-scoped cashflow + CVR slice where contract provides. |

See [`/05-data-model/permissions-matrix.md`](../05-data-model/permissions-matrix.md).

---

## Open questions

- [ ] Cost-code overrun policy — hard-block vs soft-warn-and-proceed with FD sign-off?
- [ ] Are timesheets allocated **per day** or **per task** within a day?
- [ ] Forecast horizon for cashflow — 13-week rolling, or different per audience?
- [ ] Completion-% prediction model — site-reported, timesheet burn rate, or blended (default)?
- [ ] Snapshot cadence — per Claim Period only, or also weekly cashflow snapshots?
- [ ] CVR review cadence — monthly with the QS lead, or weekly for active projects?
- [ ] EOT commercial recovery — captured against the EOT entry, or routed through workflow 05 variations?
- [ ] Time-related prelim run rate — derived automatically from the Prelim Forecast week × item grid, or set per project?

---

## Confirmation checklist

- [ ] PVR walked end-to-end with the QS, PM and a Director.
- [ ] CVR walked end-to-end with the QS and PM; explicitly confirms the three Planyard-pilot fixes.
- [ ] Cashflow walked with the FD.
- [ ] Cost-code overrun rule confirmed.
- [ ] Permissions confirmed.
- [ ] Confirmed CVR replaces the JBB Excel CVR workbook and removes the need for Planyard.
- [ ] Signed off.
