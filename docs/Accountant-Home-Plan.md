# Accountant Home — plan

**Status: proposed, awaiting sign-off · 2026-08-11**

A bespoke homepage for the accountant that leads with the work waiting on him, in three areas: supplier-invoice lines not yet linked to work orders, valuation reports that need moving on, and cost centres whose figures have moved since he last looked at them. The third area introduces the one new concept in this plan — a **cost centre check**: a per-centre snapshot of the figures at the moment he marks it checked, against which later changes re-flag the centre automatically.

Everything below is written against the current code. File paths, type names and routes were verified against the source on 2026-08-11.

---

## 1. Who sees it, and where it lives

`Pages/Dashboard.razor` already branches the homepage by role (`Session.ActiveRole`): Admin gets `AdminHome`, everyone else `RoleHome`. We add one branch:

```csharp
else if (Session.ActiveRole == Role.Accounts) { <AccountantHome CurrentUser="Auth.CurrentUser!" /> }
else if (Session.ActiveRole is not null)      { <RoleHome ... /> }   // unchanged
```

`AccountantHome.razor` is a new component in `jpms/Components/`, built to the same shape as `RoleHome`: greeting + role brief ("The accounts work waiting on you."), a strip of count tiles, then the panels, then the "Where to next" nav cards. `RoleHome` itself is untouched — no other role's homepage changes.

**Assumption to confirm:** the accountant signs in as `Role.Accounts` (persona P13). If he actually holds `FinanceDirector`, we gate the branch on whichever role he really uses — one line. The FD/MD could also be given a way into this view later; not in scope here.

Reads are no problem: every query this page needs is (or will be) gated `JpmsRoleSets.AllInternal`, which includes Accounts. Project pages carry no client-side role gate, so links from the dashboard into WO Allocation / Financials / Valuation all work for him today even though those sidebar rows are hidden from Accounts. The sidebar stays as it is — this dashboard *is* his navigation. (§7 covers the one write-permission gap.)

---

## 2. The tile strip

Four `MetricStat` tiles, each clickable, `IsBad` when non-zero, following `RoleHome`'s `Tile` pattern:

| Tile | Source | Links to |
|---|---|---|
| Invoice lines to link | new `ListWorkOrderLinkBacklog` (§3), summed over live projects | first backlog project's `/projects/{id}/work-order-allocation` (or `/projects` if several) |
| Xero lines to allocate | existing `XeroLedgerReadModel.Counts.Unallocated` (global) | `/finance/allocation` |
| Valuation reports needing action | new `ListValuationReportStates` (§4), count of stages where the ball is with us | `/projects` |
| Cost centres to check | new `ListCostCentreCheckStates` (§5), count across live projects | `/projects` |

Tiles use `IsLoading` (jewel in place of the figure) until their store has loaded — never a `0` that later becomes `47`.

---

## 3. Panel A — Work order allocation backlog

**What he sees.** One row per live project that has supplier-invoice lines with money not yet linked to a work order: project name, count of unlinked lines, £ remainder, link straight to `/projects/{id}/work-order-allocation` (whose invoice-lines queue already defaults to the Unlinked filter — he lands exactly where the work is). Projects in `InWorkOrder()` order. Projects with a clean sheet don't appear; when the whole panel is clean it says so in one quiet line.

**Why a new query.** "Unlinked" (a line allocated to a project but whose `Net` exceeds the sum of its `XeroLineWorkOrderLinks` slices — `ProjectCostOfSalesLine.UnlinkedRemainder != 0`) is today computed only client-side, per project, from `ListProjectCostOfSalesLines`. There is no cross-project count anywhere, and having the dashboard download every project's full cost-of-sales line set would be N heavy calls.

**New query** — `contracts/Commercial/ListWorkOrderLinkBacklog.cs`:

```csharp
public sealed record ListWorkOrderLinkBacklog() : IQuery<IReadOnlyList<WorkOrderLinkBacklogRow>>;
public sealed record WorkOrderLinkBacklogRow(string ProjectId, int UnlinkedLineCount, decimal UnlinkedTotal);
```

Route `GET /api/work-order-link-backlog`, gated `JpmsRoleSets.AllInternal`. The handler must agree to the penny with the allocation page's own "Not linked" tile, so it does **not** re-derive the predicate: it extracts the row-building core of `ListProjectCostOfSalesLinesHandler` into a shared internal builder and aggregates `UnlinkedRemainder != 0` per project across non-Completed projects. Project names come from `ProjectListReadModel` on the client — the query returns ids only.

---

## Panel B — Valuation reports by stage

**What he sees.** One row per live project: the current claim ("Claim 7 · June 2026"), a stage pill, and next-action wording, linking to `/projects/{id}/valuation`. Rows where the ball is with us sort first (then `InWorkOrder()`); rows waiting on the client render quiet.

**The stage machine already exists** — but as a private enum inside `Pages/ProjectValuation.razor` (`ClaimStage`: Draft → AwaitingInvoice → InvoicePending → AwaitingPayment → ReadyToConfirm → Confirmed, fused from `ValuationClaimStatus` + the linked `ValuationInvoiceStatus`). Step one is a behaviour-neutral refactor: promote it to `contracts/Commercial/ValuationReportStage.cs` (`ValuationReportStage.For(claimStatus, invoiceStatus)`) and have `ProjectValuation.razor` consume it, so the report tab and the dashboard can never disagree about what stage a report is at.

Stage → dashboard wording:

| Stage | Ball | Wording |
|---|---|---|
| Draft | us | "Value the claim and lock it" |
| AwaitingInvoice | us | "Raise the invoice" |
| InvoicePending | client | "Invoice awaiting approval" |
| AwaitingPayment | client | "Invoiced — awaiting payment" |
| ReadyToConfirm | us | "Payment in — confirm & roll over" |
| Confirmed / no open claim | us if next valuation date due/overdue | "Start the next claim" (reuses `NextExpectedValuationDate`, same overdue rules as `NextValuationsPanel`) |

**New query** — `contracts/Commercial/ListValuationReportStates.cs`: `GET /api/valuation-report-states`, `AllInternal`. Per non-Completed project: latest claim (highest `ClaimNumber`) with its status, display name and dates, the linked invoice's status/reference/amount, and `NextExpectedValuationDate`. The stage itself is derived client-side via the shared `ValuationReportStage` so there is exactly one derivation.

For Accounts, this panel *replaces* `NextValuationsPanel` (it strictly supersedes it: dates plus statuses). Other roles keep `NextValuationsPanel` unchanged.

---

## 5. Cost centre checks — the new concept

### The idea

The accountant reconciles a cost centre, then marks it **checked**. Checking stores a snapshot of the three figures he tracks — **contract value, drawdown, overspend** — for that centre. From then on the system compares the live figures against the snapshot; the moment any of the three differs, the centre is flagged **needs checking** again. A cost centre that has never been checked but carries figures is flagged by definition — which also covers "a new line appeared in the financial summary", since a new centre has figures and no snapshot. Check state is shared across users (one truth: has this centre been looked at since it last moved), with who/when recorded.

### The figures, precisely

The three figures are the ones the Financial summary table shows per row, on its default basis (hide-packaged-scope on):

- **Contract value** = `BudgetedSales − PackagedSales` (the table's Contract Sales Value cell)
- **Drawdown / Overspend** = the sign-split per-centre remainder — `round((BudgetedSales − PackagedSales) × CostFactor, 2) − (NonWorkOrderActualCost − PackagedNonWoCost) − committed + PackagedWoCommitted`, positive → drawdown, negative → overspend

That per-centre loop already lives in `ProjectDrawdown.SplitForProject` (contracts) — it just isn't exposed per centre. **Refactor:** add `ProjectDrawdown.PerCentre(summaryRows, committedByCostCode) → IReadOnlyList<CostCentrePosition>(CostCode, ContractValue, Drawdown, Overspend)`, implemented by extracting the existing loop body; `SplitForProject` becomes a sum over it (plus the package add-back, which stays project-level — packages net off the centres and carry their own drawdown, exactly as today). `FinancialsTable`'s `DrawdownFor`/`OverspendFor` cells switch to consuming it. One formula, three consumers — table, check command, dashboard — none of which can drift.

Since `contracts/` is referenced by the API, the server can compute these figures too: `GetProjectFinancialSummaryHandler` output + `ProjectDrawdown.CommittedByCostCode` over the project's work orders.

**Finalised (locked) centres are out of scope.** Locking banks the remainder as profit/loss and the table shows "—" for drawdown/overspend; locking is itself an explicit sign-off, so a locked centre never asks to be checked. (Open question §7 if you'd rather keep locked centres in scope for contract-value moves.)

### Data model

New entity + migration `AddCostCentreChecks` (table `CostCentreChecks`):

| Column | Type | Notes |
|---|---|---|
| `CostCentreCheckId` | string(64) PK | `cck-{guid:N}` |
| `ProjectId` | string(64) | indexed |
| `CostCode` | string(32) | unique with ProjectId (`UX_CostCentreChecks_Project_Code`) |
| `CheckedAt` | DateTimeOffset | |
| `CheckedByEmail` | string(256) | |
| `ContractValue` / `Drawdown` / `Overspend` | decimal | the snapshot, 2 dp |

Upsert on re-check — no history table; the audit need ("who checked, when") is on the row, and figure history already lives in the reconciliation audit / snapshots elsewhere. Purely additive migration; per the house rule the exact scoped `sqlcmd` apply commands ship in the same reply as the code.

### Commands & queries

Standard CQRS quartets under `api/Features/Commercial/`:

| | Contract | Route |
|---|---|---|
| Command | `MarkCostCentresChecked(ProjectId, IReadOnlyList<string>? CostCodes = null)` — null = every centre currently needing a check | `POST /api/projects/{projectId}/cost-centre-checks` |
| Query | `ListCostCentreCheckStates(string? ProjectId = null)` — null = all live projects | `GET /api/cost-centre-check-states[?projectId=]` |

The command **recomputes the three figures server-side at check time** and stamps `CheckedByEmail` from the session — never trusting figures or identity from the browser, so a stale tab can't snapshot stale numbers. One command covers both the per-row "Mark checked" and the toolbar "Mark all checked".

The query returns, per centre with any activity: current figures, snapshot figures + `CheckedAt`/`CheckedByEmail` (null if never checked), and `NeedsCheck` with which of the three moved. `NeedsCheck` = no snapshot and any figure ≠ 0, **or** any figure differs from the snapshot (compared at 2 dp — every input is already deterministically rounded).

Authorisation: new `CostCentreCheckRoles = Director, FinanceDirector, ProjectManager, Estimator, Accounts` for the command; the query is `AllInternal`.

### UI

**On the Financial summary** (`FinancialsTable.razor`): a Checked column at the row's action end — a quiet tick with hover text "Checked 4 Aug by Nigel" when clean; an amber "needs check" marker when flagged, hover text naming what moved ("Drawdown moved £1,240 since last checked"); a per-row "Mark checked" action. "Mark all checked" joins the view's `Toolbar` as a `ToolbarButton` (glyph + mandatory hover text, grouped with the data actions). Nothing here is gated behind a jewel — check state arrives with its own read model and the markers simply appear when loaded, per the never-gate-a-single-line rule.

**On the dashboard, Panel C**: one row per live project with flagged centres — project name, "3 centres to check", the largest movement as a strapline ("SUPER-STR drawdown −£4,210"), linking to `/projects/{id}/financials`. `InWorkOrder()` order; clean projects absent; all-clean states itself in one line.

---

## 6. Frontend plumbing & conventions

Three new read models in `jpms/Features/Commercial/` following the house pattern exactly — nullable backing store (`Current` null until a fetch lands, never `Array.Empty`), `LoadedFor`/`IsLoaded`, `LastRefreshFailed`, `RefreshAsync` (stale-while-revalidate, refreshed once from `OnInitializedAsync`), `OnChanged`:

`WorkOrderLinkBacklogReadModel` · `ValuationReportStatesReadModel` · `CostCentreCheckStatesReadModel` (this last keyed per project *and* offering the cross-project view, so the Financials page and the dashboard share one cache).

`AccountantHome` loading follows CLAUDE.md to the letter: each panel is a `Panel IsLoading` gated on `LoadState.UntilAll(...)` of *its own* sources plus the shared `ProjectListReadModel`; every fetch pairs with a `dataFailed` flag (`try/catch` + `LastRefreshFailed`) so a failed fetch opens the gate to a message rather than pulsing forever; tiles use `MetricStat.IsLoading`; page chrome renders on `sessionReady` alone. Route registrations go in `CommercialRouteRegistration` / feature registration files as usual.

---

## 7. Open questions (none block the build starting)

1. **Is the accountant `Role.Accounts`?** Assumed yes; the Dashboard branch is one line to change if not.
2. **Should Accounts be able to *do* the linking?** `SetXeroLineWorkOrderLinksAuthorisation` is currently Director/FD/PM/Estimator — the dashboard will show him a backlog he cannot clear himself. Since the request says allocation is *his* job, I recommend adding `Role.Accounts` to that one command's authorisation (and nothing else — claim lifecycle, finalisation etc. stay as they are). Confirm and it's included.
3. **Locked centres**: fully out of the check regime (recommended), or kept in scope for contract-value changes only?

## 8. Delivery order

Three slices, each shippable alone: **(1) Backend** — contracts (`ValuationReportStage`, `ProjectDrawdown.PerCentre`, three new query/command contracts), entity, migration + apply commands, handlers/endpoints, `ProjectValuation.razor` + `FinancialsTable` refactors to the shared helpers (behaviour-neutral, verifiable against today's rendered figures). **(2) Accountant home** — read models, `AccountantHome` + three panels + tiles, Dashboard branch. **(3) Financials checked column** — row markers, mark-checked actions, toolbar button.
