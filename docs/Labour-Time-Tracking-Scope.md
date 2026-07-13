# Labour Time Tracking — Scope

**Status:** Draft for review · July 2026
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

**`WorkerEntity`** — WorkerId, Name, HourlyRate (decimal £/hr), IsActive, optional Email/Phone. Rate is server-side only; never serialised to the capture app.

**`WorkerRateHistoryEntity`** — WorkerId, HourlyRate, EffectiveFrom. Rate used for costing is the rate effective on `WorkedOn`, snapshotted onto the timesheet at approval so historic cost never changes when rates change.

**`SiteAttendanceEntity`** — AttendanceId, ProjectId, WorkerId, Date, SignedInAt, SignedOutAt, DeviceKey (optional, see §8). One per worker per project per day. Drives the daily site register.

**`ProjectWorkerAssignmentEntity`** — ProjectId, WorkerId, IsActive. Controls whose names appear on that project's sign-in list.

Changes to `TimesheetEntity`:

- Add `WorkerId` (FK; PersonEmail retained for legacy rows), `AttendanceId` (FK, nullable), `RateApplied` (decimal, set at approval), `CostAmount` (decimal, = Hours × RateApplied, set at approval), `ApprovedByEmail` + `ApprovedAt`, `Status` enum (Submitted / Approved / Rejected — replaces bare `IsApproved` semantics; keep the column for migration).
- One row per worker × date × cost code (a sign-out submission creates N rows).

Constraints (from PM spec, kept): 0.5-hour increments, min 0.5; at least one entry > 0 to submit; soft warning above 12 total hours, no hard block; one sign-out per worker per project per day.

---

## 5. Capture app (Phase 1)

A minimal mobile web page (not the Blazor portal — see §8), reached via project QR code / short URL:

1. **Sign in** — worker picks their name from the project's assigned list, taps Sign In → `SiteAttendance` row created.
2. **During day** — page shows the project's active cost codes as reference. Nothing to do.
3. **Sign out** — worker enters hours per cost code worked (0.5 steppers, large touch targets), running total shown, soft >12 hr warning, Submit & Sign Out → attendance closed, timesheet rows created with Status = Submitted.
4. **Confirmation** — summary of hours logged. No £ anywhere in the worker UI, ever.

Missed sign-outs: attendance left open overnight is flagged in the PM approval view; the PM enters/adjusts hours on the worker's behalf.

Offline: Phase 1 requires connectivity at sign-in and sign-out only (a few seconds each) — much smaller exposure than per-task clocking. Full offline queue is Phase 2.

## 6. Approval & posting (Phase 1)

New **Labour** tab on the project (follows the existing tab pattern: store with `Refresh(projectId)` called once from `OnInitializedAsync`, per CLAUDE.md convention):

- Week grid: workers × days, hours per cost code, submitted vs approved state.
- PM can adjust hours, re-code to a different cost code, reject with reason, and approve individually or batch-approve the week (`ApproveTimesheet`, extended to batch).
- **On approval:** rate resolved from history → `RateApplied`/`CostAmount` snapshotted → **budget hard-block check** against `CostCodeBudget` remaining (reject with "raise WO or re-allocate" message per workflow 07-D) → cost joins the non-WO actual cost of sales aggregation.
- Financials tab: labour appears in **Non-WO cost of sales** once approved; a *Pending labour* figure (submitted, unapproved hours × current rate) shows separately so PMs get the live view without polluting actuals.
- Rate visibility restricted: rates and £ visible to ProjectManager/QS/FD/Admin roles only; the tab shows hours-only to other roles.

Reporting (Phase 1): hours and £ by worker / cost code / project / week; labour budget vs actual per cost code (Financials tab drill-down); daily site register (who was on site, in/out times) per project per date.

---

## 7. Phasing

**Phase 1 — core loop** (everything above): worker registry + hourly rates + project assignment · QR sign-in / sign-out capture page · end-of-day allocation to cost codes · PM Labour tab with batch approval + hard-block · posting to non-WO cost of sales + pending labour · site register + basic reports.

**Phase 2:** offline capture queue (PWA, background sync) · Bill of Work assignments with estimated hours and worker-visible task lists (PM spec §Bill of Work) · role-based default rates and overtime rules (accountant §3) · payroll export / Brightpay reconciliation report · inline "raise WO" action from a hard-block rejection (already in backlog).

**Explicitly out of scope:** per-task clock in/out with breaks · worker-controlled task completion driving cost-progress · real-time unapproved posting · geofencing/photo verification · integration of labour cost into valuation invoices.

---

## 8. Security & platform notes

- **Anonymous capture surface is new risk.** The QR encodes a per-project opaque token; the endpoint only exposes worker names assigned to that project and only accepts sign-in/out actions. Token rotatable by the PM. Rates are never sent to the client (both briefs agree). Accepted residual risk in Phase 1: a worker can sign in as a colleague — mitigated by the PM approval review and the site register being visible to the foreman; device-key pinning can tighten this later if abused.
- **Capture page is not Blazor WASM.** The portal's WASM payload and cookie-auth session are wrong for an anonymous, poor-signal phone page. Build the capture page as a small static page + JS against dedicated `/api/site-labour/*` endpoints. The PM-facing Labour tab lives in jpms as normal.
- Terminology: all UI copy, identifiers and docs use *timesheet*, *cost code*, *non-WO cost of sales*. (No "valuation invoice" interactions anywhere in this feature.)

---

## 9. Open questions

1. Standard hours for day-rate → hourly conversion: 8.0 confirmed? Per-worker override needed?
2. Who maintains the worker registry and rates — FD only, or PM for names with FD for rates?
3. Should subcontractor day-rate labour (glossary currently defines Timesheet as subcontractor time) run through this same capture flow in Phase 1, or is this employed/agency labour only?
4. Rejected timesheets: does the worker re-submit next day via the capture app, or is correction always PM-side?
5. Does Pending labour need to appear in the cashflow forecast's labour commitment, or only on the Financials tab?
