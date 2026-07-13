# Labour Time Tracking — Scope

**Status:** Phase 1 implemented (see §10) · July 2026
**Inputs:** Junior PM technical spec (`labourtrackingspec.md`), accountant's build brief, existing JPMS codebase and docs.
**Owner decisions taken:** approval-gated posting with pending visibility · attendance + end-of-day allocation capture · hourly cost rates.

---

## 1. Purpose

Capture site labour time per worker per day, allocate it to project cost codes, convert approved hours to £, and post it to the project as **direct (non-WO) actual cost of sales** — giving live labour visibility on the Financials tab and a daily site register.

---

## 2. Reconciling the two briefs

The PM spec and the accountant's brief conflict on three points. Decisions:

| Topic | PM spec | Accountant | Decision |
|---|---|---|---|
| Posting | Cost hits Financials instantly at sign-out | Only approved time becomes cost | **Approval-gated.** Unapproved time shows on Financials as *Pending labour* (visible, not posted). PM approval converts it to actual cost of sales. Matches existing `ApproveTimesheet` contract and `approval-flows.md` row 18 (PM weekly batch approval). |
| Capture | Sign in/out attendance + end-of-day hours allocation | Clock in/out per task with breaks | **Attendance + end-of-day allocation.** One sign-in on arrival (feeds the site register), hours split across cost codes at sign-out. Per-task clock-switching is deferred indefinitely — high friction, poor fit for site reality. |
| Rates | Day rate prorated across tasks by share of hours | Cost rate (hourly/day) converting hours → £ | **Hourly rate; £ = hours × rate.** Day-rate workers get `rate = day rate ÷ standard hours (8)`. Role-based rates and overtime multipliers deferred (accountant agrees these can come later). |

Corrections to the PM spec so it fits JPMS:

- **"Budget line items" → cost codes.** JPMS budgets are cost-code centric (`CostCodeBudgetEntity`: Allocated/Spent/Committed per project × code). All labour allocation targets a cost code, not a new line-item concept.
- **No denormalised `labour_spent` counter.** The PM spec increments a running total on the budget record at sign-out. JPMS computes actuals by aggregation (`ListCostCentreActualCostsHandler`); labour will join that aggregation. No counters to drift, no atomic-increment race conditions.
- **Posting term:** approved labour posts as **non-WO cost of sales** on the Financials tab (existing column), alongside Xero purchase spend. It never touches valuation invoices.
- **"Bill of Work" assignment with worker-controlled Complete status** is deferred to Phase 2 (see §7). In Phase 1 workers pick from the project's active cost codes; task completion remains the PM/QS's call via existing `CostCentreCostProgress`, not the worker's.

---

## 3. What already exists (build on, don't duplicate)

- `TimesheetEntity` (`api/Data/Entities/CommercialEntities.cs`): ProjectId, PersonEmail, WorkedOn, Hours, CostCode, IsApproved — extend, don't replace.
- Contracts `SubmitTimesheet` / `ApproveTimesheet` (`contracts/Commercial/`).
- Workflow 07-D: timesheet weekly batch approval by PM; **budget hard-block** — allocation to a cost code with no remaining budget is rejected unless a WO is raised or budget re-allocated.
- CVR "Cost Incurred" already lists *day-rate timesheets approved* as a component.
- Financials tab actual-cost aggregation with WO / non-WO split.
- Roles enum includes ProjectManager, SiteManager, Foreman. **No Worker/Person entity yet** (PersonEmail is free text) — this scope introduces one.
- No anonymous endpoints, no mobile/offline surface — both are new build.

---

## 4. Data model (Phase 1)

New entities:

**`WorkerEntity`** — WorkerId, Name, HourlyRate (decimal £/hr), IsActive, optional Email/Phone, optional `SubcontractorId` (FK → existing `SubcontractorEntity`). The system is for **subcontractor day-rate labour** (consistent with the glossary: a Timesheet records subcontractor time for cost tracking and payment), so each worker is normally an operative of a subcontractor and the rate is that operative's agreed day rate ÷ 8. Rate is server-side only; never serialised to any worker-facing response. Registry and rates are maintained by the FD and PM roles. A per-worker standard-hours override is deferred until a real case needs it.

**`WorkerRateHistoryEntity`** — WorkerId, HourlyRate, EffectiveFrom. Rate used for costing is the rate effective on `WorkedOn`, snapshotted onto the timesheet at approval so historic cost never changes when rates change.

**`SiteAttendanceEntity`** — AttendanceId, ProjectId, WorkerId, Date, SignedInAt, SignedOutAt. One per worker per project per day. Drives the daily site register.

**`ProjectWorkerAssignmentEntity`** — ProjectId, WorkerId, IsActive. Controls which projects appear on a worker's My day page.

Changes to `TimesheetEntity`:

- Add `WorkerId` (FK; PersonEmail retained for legacy rows), `AttendanceId` (FK, nullable), `RateApplied` (decimal, set at approval), `CostAmount` (decimal, = Hours × RateApplied, set at approval), `ApprovedByEmail` + `ApprovedAt`, `Status` enum (Submitted / Approved / Rejected — replaces bare `IsApproved` semantics; keep the column for migration).
- One row per worker × date × cost code (a sign-out submission creates N rows).

Constraints (from PM spec, kept): 0.5-hour increments, min 0.5; at least one entry > 0 to submit; soft warning above 12 total hours, no hard block; one sign-out per worker per project per day.

---

## 5. Capture: the My Day page (Phase 1)

**Decision (July 2026, revised): no anonymous QR capture.** Workers are normal portal users — one RBAC system everywhere. Each worker is invited as a user with the **Site Operative** role, sets a password like anyone else, and their account is linked to their worker record by email on the Workers page.

Their whole portal experience is the mobile-first **My day** page (`/my-day`):

1. **Sign in** — tap Sign in on the project card → `SiteAttendance` row created (the site register).
2. **Sign out** — enter hours per cost code (0.5 steppers), running total, soft >12 hr warning, Submit & sign out → attendance closed, timesheet rows created with Status = Submitted.
3. **Rejected days** appear on the same page for correction and resubmission. No £ anywhere in the worker UI, ever.

Missed sign-outs: attendance left open overnight is flagged in the PM approval view; the PM enters hours on the worker's behalf (manual entry on the Labour tab).

Offline: connectivity is needed at sign-in and sign-out only (a few seconds each). Full offline queue is Phase 2.

## 6. Approval & posting (Phase 1)

New **Labour** tab on the project (follows the existing tab pattern: store with `Refresh(projectId)` called once from `OnInitializedAsync`, per CLAUDE.md convention):

- Week grid: workers × days, hours per cost code, submitted vs approved state.
- PM can adjust hours, re-code to a different cost code, reject with reason, and approve individually or batch-approve the week (`ApproveTimesheet`, extended to batch).
- **Rejected timesheets** re-open for the worker: next time they open the capture page they see the rejected day and can re-submit corrected hours. No deadline is enforced (site reality: workers won't reliably act next day) — rejected entries simply stay visible in the PM's approval view until resolved, and the PM can always correct and approve on the worker's behalf instead.
- **On approval:** rate resolved from history → `RateApplied`/`CostAmount` snapshotted → **budget hard-block check** against `CostCodeBudget` remaining (reject with "raise WO or re-allocate" message per workflow 07-D) → cost joins the non-WO actual cost of sales aggregation.
- Financials tab: labour appears in **Non-WO cost of sales** once approved; a *Pending labour* figure (submitted, unapproved hours × current rate) shows separately so PMs get the live view without polluting actuals.
- Pending labour is a Financials-tab figure only in Phase 1. The cashflow forecast and CVR consume **approved** labour cost exclusively (per workflow 07: Cost Incurred includes "day-rate timesheets approved") — unapproved entries are too noisy to steer FD-level forecasts. Feeding a labour-commitment line into the cashflow forecast can be revisited in Phase 2 once data quality is proven.

**Avoiding double-count with Xero invoices.** Day-rate subcontractors also invoice, and those invoices land in Xero and are today allocated to cost codes as actual cost of sales. If approved timesheet cost *and* the covering invoice both post, labour double-counts. Rule: **the approved timesheet is the timely actual; the paid invoice is the truth at settlement.** During Xero line allocation, lines from a day-rate subcontractor are marked *covered by timesheets* — linked to the approved timesheets for the period (same pattern as `XeroLineWorkOrderLink`) and excluded from the cost-of-sales aggregation.

**When invoice ≠ approved timesheet £**, the variance stays open on the reconciliation view until closed by one of four paths: (1) timesheets were wrong/incomplete → PM corrects and re-approves, actuals update; (2) invoice is wrong → query/short-pay, amended invoice closes it; (3) invoice legitimately includes non-labour (materials, plant, other projects) → split via the existing `XeroCostSplit` mechanism, labour portion links to timesheets, remainder allocates as normal cost; (4) difference accepted → residual posts as a visible *settlement variance* adjustment to the cost code(s), so posted cost of sales always equals cash paid and nothing is silently absorbed. The reconciliation view lists unresolved variances per subcontractor per period — labour cost is timely all month via timesheets and provably true at settlement.
- Rate visibility restricted: rates and £ visible to ProjectManager/QS/FD/Admin roles only; the tab shows hours-only to other roles.

Reporting (Phase 1): hours and £ by worker / cost code / project / week; labour budget vs actual per cost code (Financials tab drill-down); daily site register (who was on site, in/out times) per project per date.

---

## 7. Phasing

**Phase 1 — core loop** (everything above): worker registry + hourly rates + project assignment · QR sign-in / sign-out capture page · end-of-day allocation to cost codes · PM Labour tab with batch approval + hard-block · posting to non-WO cost of sales + pending labour · site register + basic reports.

**Phase 2:** offline capture queue (PWA, background sync) · Bill of Work assignments with estimated hours and worker-visible task lists (PM spec §Bill of Work) · role-based default rates and overtime rules (accountant §3) · payroll export / Brightpay reconciliation report · inline "raise WO" action from a hard-block rejection (already in backlog).

**Explicitly out of scope:** per-task clock in/out with breaks · worker-controlled task completion driving cost-progress · real-time unapproved posting · geofencing/photo verification · integration of labour cost into valuation invoices.

---

## 8. Security & platform notes

- **No anonymous surface.** All labour endpoints use the standard session-cookie auth and role gates. Worker self-service endpoints (`/api/my/labour/*`) are gated to the LogOwnTime role set (SiteOperative, Foreman, SiteManager) and resolve the caller to their own worker record by email — no impersonation is possible, and rates are never sent to any worker-facing response.
- **My day is a normal jpms page.** Same Blazor WASM app, same session, mobile-first layout.
- Terminology: all UI copy, identifiers and docs use *timesheet*, *cost code*, *non-WO cost of sales*. (No "valuation invoice" interactions anywhere in this feature.)

---

## 9. Decisions log & open questions

Resolved (July 2026, revised): **QR/anonymous capture dropped** — workers are portal users with the SiteOperative role, logging time on the authenticated My day page; one RBAC system everywhere · standard day = 8.0 hours, override deferred (§4) · worker registry and rates managed by FD/PM (§4) · rejected timesheets re-submittable via the capture app with no enforced deadline, PM-side correction always available (§6) · pending labour is Financials-tab only; cashflow/CVR consume approved cost exclusively (§6) · **the system is for subcontractor day-rate labour** — workers link to `SubcontractorEntity`, approved timesheets are the actual cost, covering Xero invoices are marked as settlement and excluded from cost of sales to prevent double-counting (§4, §6).

**Open — for the accountant to confirm:** the settlement model in §6 — timesheet as timely actual, paid invoice as final truth, variances closed only via the four defined paths (correct timesheets / amend invoice / split non-labour / post settlement variance). Specifically: are they happy that cost of sales for day-rate labour is timesheet-driven intra-month, and that their Xero allocation workflow gains the covered-by-timesheets linking step?

---

## 10. Implementation notes (Phase 1, July 2026)

**Data:** `WorkerEntity`, `WorkerRateHistoryEntity`, `ProjectWorkerAssignmentEntity`, `SiteAttendanceEntity`, `SiteAccessTokenEntity`, `XeroLineTimesheetCoverEntity`, `LabourSettlementVarianceEntity` (`api/Data/Entities/LabourEntities.cs`); `TimesheetEntity` extended (WorkerId, SiteAttendanceId, Status, RateApplied, CostAmount, ApprovedByEmail/At, RejectionReason). Migration `20260713100000_AddLabourTracking` (legacy `IsApproved` rows backfilled to Status Approved).

**API:** vertical slices in `api/Features/Labour/` — registry/assignment (ManageWorkers gate: MD/FD/PM), Labour-tab queries and adjust/add/approve/reject (approve gate MD/PM; £ stripped for non-commercial roles), settlement (CommercialTeam gate), and the worker self-service surface under `api/my/labour/*` (LogOwnTime gate; caller resolved to their worker record by email via `WorkerByEmail`; rates never serialised). Pure rules in `contracts/Labour/LabourRules.cs` (half-hour steps, rate-effective-on-date, hours × rate, budget hard-block) — tested in `tests/Jewel.JPMS.Tests/LabourRulesTests.cs`. `GetProjectFinancialSummaryHandler` now adds approved labour + settlement variances to ActualCost/Non-WO, excludes covered Xero lines, and returns `LabourActualCost` / `PendingLabourCost` per row.

**Front end:** project **Labour** tab (`jpms/Pages/ProjectLabour.razor` — week grid with batch approve, manual entry for missed sign-outs, worker assignment, site register, settlement view with covered-line marking); **Workers** registry page (`/labour/workers`, day-rate ÷ 8 entry, portal-email link); **My day** worker page (`jpms/Pages/MyDay.razor`, mobile-first, no £); pending-labour banner on Financials. New `Role.SiteOperative` (users invited through the normal directory flow). Migration `20260713150000_DropSiteAccessTokens` removes the abandoned QR token table.

**Before deploy:** run `dotnet build` + `dotnet test` locally (this change was authored without a compiler to hand — expect at most minor fix-ups); apply the migration through the usual process; if the EF model snapshot drifts, regenerate with `dotnet ef migrations add` rather than trusting the hand-written Designer. Known polish items: unmarking covered lines from the UI (API supports it), offline queue for My day (Phase 2), `docs/05-data-model/entities.md` registry not yet updated.
