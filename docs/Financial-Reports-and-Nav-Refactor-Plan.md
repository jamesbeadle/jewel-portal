# Financial Reports & Navigation Refactor — plan

**Status: agreed direction, phasing rules awaiting FD/consultant markup · 2026-08-11**

This is the final-shape refactor of the sidebar and the financial reports, agreed with James on
2026-08-11. Decisions already taken that day: the Cash Forecast is **monthly to completion**,
phased **automatically from live dates** (no manual phasing grid in v1), with **company overheads
as a single FD-set monthly figure**, and the Valuation Report joins the Control Centre as a
**top-level link at the foot of the sidebar**. Everything in §4 (the phasing rules) is written for
the FD and the consultant to mark up line by line — each rule states the source field it reads so
a disagreement is about the rule, never about where the number came from.

File paths and type names were verified against the source on 2026-08-11.

---

## 1. The sidebar, per role

The catalog stays data-only (`jpms/Services/Navigation/SidebarFolders.cs` +
`DesktopNavigation.cs`); this is a regrouping, not a rebuild. Slugs do not move — labels move,
routes stay, old bookmarks keep landing (the standing convention).

**Administrator, Finance Director and Managing Director** see the full nav. **Every other role
(PM, QS/Estimator, Site Manager, …) sees Home only, for now** — their nav will be designed when
those roles come onto the system. This is nav visibility only: API authorisation is untouched, so
nothing widens and nothing a role could already reach by URL changes.

Folders, in rail order:

| Folder | Rows (in order) | Route notes |
|---|---|---|
| **Project** | Requests · Variation Orders · Architect Instructions · Valuation Report Snapshots · Drawings · Programme · Todo · Progress · Defects · Communications · Project Settings | All existing pages. Variation Orders = `/projects/{project}/variations` (page exists; today it is only reachable through a request's tab bar). Snapshots and Requests move in from the retired Client folder. |
| **Subcontractor** | Bid Package Invites · Work Orders · Communications | Work Orders moves in from Finance (2026-08-11, reversing the 2026-08-04 call — placing the order is done *with* the subcontractor; paying for it stays under Finance as WO Allocation). |
| **Internal** | Todo · Directory | The master `/todos` list and `/directory` — unchanged. |
| **Time** | Labour · Workers | Unchanged. |
| **Finance** | Financials · WO Allocation · Cost Codes · Rates | Cost Codes (`/cost-codes`) and Rates (`/rate-library`) split into two rows (today one merged row). The Xero row leaves this folder — the Xero folder below owns every Xero screen, one home per screen. |
| **Financial Reports** | Project Cashflow · Cash Forecast · Profit Summary | §3–4. Project Cashflow is the renamed per-project Cashflow tab (route unchanged). Cash Forecast replaces Cash Summary (§4). Aged Receivables/Payables move to the Xero folder. Valuation Report leaves for the top level. |
| **Xero** | Xero Cost Allocation · Xero Transactions · Aged Receivables · Aged Payables | Allocation (`/finance/allocation`) and Transactions (`/finance/xero`) become two rows (today tabs behind one row); each row exact-matches its own route so the right one lights. |
| **Audit** | Reconciliation Audit · System Audit Trail · Agent Activity | "Audit Trail" renamed "System Audit Trail" (label only, route `/audit` stays). |
| **Admin** | Users · System | Administrators only, unchanged. |

**Top level (foot of the sidebar, with an icon, like Home):**

- **Control Centre** — unchanged.
- **Valuation Reports** — the picked project's `/projects/{project}/valuation`, elevated out of
  Financial Reports because it is the flagship output. Standalone rows already resolve `{project}`
  templates against the picker (`SideNav.razor` resolves standalone hrefs with
  `EffectiveProjectId`), so it follows the project switcher exactly as folder rows do, and sits
  muted until a project resolves.

Implementation notes: the `Client` member leaves the `SidebarFolder` enum and `Xero` joins it
(enum is UI-only, nothing persists it); every row's `VisibleTo` becomes `DirectorRoles` (MD + FD;
Admin passes via the `CanSee` bypass) — the old per-row sets (`ProjectRoles`, `TriageRoles`,
`FinanceRoles`, …) remain in `DesktopNavigation` untouched for the API-mirroring duties and the
future role rollout, with a comment marking the nav's temporary directors-only clamp. The project
picker shows for roles whose visible folders contain at least one project-scoped row (replacing
the blanket `CanSeeProjects` check), so a role with an empty nav gets no orphaned picker.

---

## 2. What the three financial reports each mean

One sentence each, because the boundary between them is the point:

- **Project Cashflow** — *"If this job runs to the end, where does its cash land?"* The
  to-completion statement per project. It informs how the client is approached — a job sitting
  cash-negative reads differently in a valuation conversation than one funding itself. Logic
  unchanged in this refactor.
- **Cash Forecast** — *"Between now and then, does the company stay above water — and when is the
  low point?"* The time axis Cash Summary never had. §4.
- **Profit Summary** — *"What do we earn?"* Margin, three ways (budget/current/forecast).
  Unchanged.

Cash Summary's arithmetic was never the problem — every figure in it survives inside the Cash
Forecast. What it lacked was time: it could report the company £400k positive at completion while
being silent about a £150k trough in October on the way there.

---

## 3. Cash Forecast — page shape

Route `/finance/cash-forecast`; the retired Cash Summary's routes (`/finance`,
`/finance/cash-summary`) land on the same page so bookmarks survive. Same audience split as
today: finance roles see the project flows, the bank tiles and the closing-balance line are
directors-only (they read the Xero cash position, mirroring `GetXeroCashSummaryEndpoint`'s gate).

Top to bottom:

1. **KPI strip** (directors): Cash in bank now · **Lowest forecast balance and its month** (the
   headline this page exists for; negative renders red) · Cash at horizon end. Non-directors see
   the strip without bank-anchored tiles (their view is movements, not balances).
2. **Project filter** — the same `ProjectMultiSelect`, defaulting to live jobs.
3. **The forecast table** — one column per calendar month, this month → the latest dated flow
   across selected projects (last retention release / defects-period end). Pinned first column;
   horizontal scroll. Rows in three bands:
   - **Cash in:** Valuation invoices outstanding · Future valuations · Retention releases
   - **Cash out:** Supplier bills unpaid · Work orders still to invoice · Drawdowns still to spend
     · Company overheads
   - **The running answer:** Net movement · **Closing bank balance** (directors; seeded from the
     Xero cash position).
   Every category row expands to one line per contributing project, so "why is March bad?" is one
   click, and each project line links to its Project Cashflow.
4. **Reconciliation footer** — the invariant, stated on the page (§4.9).

Loading follows the house rules: one gate for the whole table (every column is arithmetic over
every source), controls never gated, failed fetches open the gate to a message.

---

## 4. Cash Forecast — the phasing rules (for markup)

The forecast phases **exactly the figures the statements already compute** — same helpers
(`CashflowMaths`, `ProjectDrawdown`, `RetentionSchedule`, `ValuationSummaryFigures`), same
sources — into months. No figure is invented; only *when* is added. Rules below are per project,
then summed; **overdue means "assume this month", never "assume it disappears".**

### 4.1 Valuation invoices outstanding (in)

Issued, unpaid valuation invoices (`ProjectValuationInvoiceSummary.Outstanding`, itemised from
the project's invoices). Expected receipt month = `IssuedAt` + the contract's payment mechanism
(`ProjectContract.FinalDateForPaymentDays` after the due date arising from
`PaymentNoticeDays`); a project with no contract terms falls back to 30 days. Already past that
date → this month. Submitted/Approved invoices (awaiting approval) count one month later than
their Raised-equivalent timing, reflecting the approval loop.

### 4.2 Future valuations (in)

The project's **Left to Claim** (`CashflowMaths.LeftToClaim` — already net of cash received,
retention outstanding and retention still to be withheld), spread evenly across the valuation
months remaining between `Project.NextExpectedValuationDate` (fallback: next month) and
**practical completion** (`ProjectRetention.PracticalCompletionAt`; fallback
`ProjectContract.CompletionDate`; a project with neither shows its claim in an "undated" column
that keeps the total honest rather than pretending a date). Each month's claim lands as cash one
payment-mechanism lag later (§4.1's arithmetic). Deposit projects: the pro-rata deposit release
deduction (`ProjectRetention.DepositPercent`, as the claims already compute it) reduces each
phased receipt, so the phased months sum to real cash, not gross certificates.

### 4.3 Retention releases (in)

`RetentionSchedule`'s two lines, exactly as Project Cashflow shows them: R1 (still-forecast
only) in the month of `PracticalCompletionAt`, R2 in the month of PC + `DefectsPeriodMonths`.
No PC date → the undated column. Confirmed releases are already inside the claim and are not
added back — same rule as the statements.

### 4.4 Supplier bills unpaid (out)

Each allocated purchase line's `OutstandingNet` (part-payment aware, drafts included), in the
month of the bill's Xero due date; overdue or undated → this month. Implementation note:
`ListProjectCostOfSalesLines` rows don't currently carry the bill's `DueDate` — the query gains
that one field (additive), keeping the forecast on the exact same lines the statements read
rather than re-matching against the aged-payables snapshot.

### 4.5 Work orders still to invoice (out)

Per project, `WoCommitted − WoInvoiced` (same filters as the statements — rejected orders
excluded), spread evenly this month → PC month, each month's slice paid one month later
(supplier terms ≈ 30 days end-of-month). v1 deliberately spreads at project level, not per
order; per-order phasing is where the July spec's manual grid (Workstream A) would slot in later.

### 4.6 Drawdowns still to spend (out)

`ProjectDrawdown.ForProject` (netted: overspent centres reduce it, finalised centres realised —
the Financials tab's figure to the penny), spread evenly this month → PC month, paid one month
later. This is the "budget on top of orders and bills" money, so it phases like spend that has
yet to be committed.

### 4.7 Company overheads (out)

A single monthly figure, **entered and owned by the FD** — new one-row setting (amount, who,
when; directors-only command; shown with "set by Nigel, 11 Aug" on the page), charged to every
month in the horizon. Deliberately not derived from Xero in v1: an FD-owned number is honest and
maintainable; deriving the un-sited P&L average is noisy and debuggable-by-nobody. The row keeps
project rows and company reality from quietly diverging.

### 4.8 What is deliberately excluded

Unapproved variations (nothing confirmed — they stay Project Cashflow's dashed "potential" card);
unallocated site-tracked bills (warned about, per the existing guard, but never silently counted);
VAT (all figures net, as everywhere else in the portal — worth a consultant confirmation, since
the bank movement is gross); financing lines (none exist in the data).

### 4.9 The invariant that keeps it honest

For every project: **the sum of its phased future months equals its Project Completion Cashflow
on Project Cashflow, to the penny.** The forecast is the same numbers spread in time — never a
second opinion. The page computes both sides and renders any variance red with the offending
project named; it is a bug surface, not a judgement call. (Rounding from even spreads is absorbed
into each spread's final month so the identity holds exactly.)

---

## 5. Retired / moved

| Today | Becomes |
|---|---|
| Cash Summary (`/finance`, `/finance/cash-summary`) | Cash Forecast (routes land on the new page). The combined to-completion statement it carried survives as the reconciliation view behind the invariant (§4.9) — its per-project table is one click away on each Project Cashflow. |
| Cashflow (label) | Project Cashflow (label only) |
| Valuation Report row in Financial Reports | Top-level "Valuation Reports" beside Control Centre |
| Aged Receivables / Aged Payables rows | Xero folder |
| Client folder | Gone (rows redistributed to Project) |
| Cost Codes & Rates merged row · Xero merged row | Split rows (§1) |

## 6. Delivery order

1. **Nav restructure** — pure catalog + gating change, no API work, shippable alone.
2. **Cash Forecast backend** — `DueDate` on cost-of-sales lines (additive), overheads setting
   (entity + migration, apply commands shipped in the same reply per the house rule), contracts
   for the forecast read.
3. **Cash Forecast page** — phasing engine (pure, unit-tested against the §4.9 invariant), table,
   KPI strip, Excel export in the same layout.
4. **Retire Cash Summary** — route re-point, catalog row swap.
