# Cashflow tab — scope for discussion

**Status:** Draft for discussion (scope only, no build yet)
**Author:** Cowork (for Nigel Reilly)
**Date:** 8 July 2026

---

## 1. What we're building

A new **Cashflow** tab in the project view, sitting after **Financials**. Per cost-centre line item it shows, on the left, the **claim position** — contract value, budgeted cost (contract value less the assumed 10% margin), completion %, and the expected cost at that completion — and, to the right, **one column per supplier** holding a work order that covers that line's work, so committed supplier spend can be read against the expected cost line by line.

Work orders exist in the schema today but are only created when a bid package is awarded and are not surfaced anywhere in the UI. This work adds the ability to record work orders for suppliers directly and allocate them to cost centres, which is what makes the supplier columns possible.

---

## 2. What exists today (verified in the codebase)

### 2.1 Claim data — everything the left-hand block needs already exists

- `ValuationLineItemEntity` (`api/Data/Entities/ValuationReportEntities.cs`) — every valuation report line carries a **`CostCode`**, `LineAmount`, `LineType` (Declined/TBC excluded from totals).
- `ValuationClaimEntity` + `ClaimLineEntity` — the latest claim gives **`PercentComplete`** and **`CumulativeClaimed`** per valuation line.
- `GetProjectFinancialSummaryHandler` (`api/Features/Commercial/Queries/`) already computes, per cost centre: budgeted sales (sum of counting valuation lines), **budgeted cost = sales × 0.9** (`FinancialSummaryAssumptions.MarginPercent = 10` in `contracts/Commercial/GetProjectFinancialSummary.cs`), **completion % = claimed ÷ budgeted sales** from the latest claim, and **expected actual cost = budgeted cost × completion %**. The Cashflow tab's claim block is exactly these figures — we reuse the calculation, not reinvent it.

### 2.2 Work orders — half-built

- `WorkOrderEntity` (`api/Data/Entities/ProcurementEntities.cs`): `ProjectId`, `BidPackageId`, `SubcontractorId`, `Value`, `Scope`, `AwardedAt`, `AwardedByEmail`.
- Created only by `AwardBidPackageHandler` (awarding a bid package). `ListWorkOrders` / `UpdateWorkOrder` endpoints exist in `api/Features/Procurement/` but **no front-end page uses them**.
- **Gap 1:** a work order has **no cost-centre link** — its value can't currently be placed against a cost-centre row.
- **Gap 2:** a work order **requires a bid package** — no way to record an order for a supplier directly.

### 2.3 Suppliers

`SubcontractorEntity` is the supplier record (Subcontractors page exists). Bid package lines already map to commercial homes (`BidPackageLineItemEntity.Coverage` → BoQ line / VOQ), which could seed cost-centre allocation for awarded packages.

### 2.4 Naming clash to resolve

There is already an `api/Features/Cashflow/` + `CashflowSnapshotEntity`: a **company-wide 13-week snapshot** (expected income / committed spend / net), with capture + get-latest endpoints and a front-end read model — unrelated to this per-project tab. New code should be namespaced **ProjectCashflow** (or the old feature renamed) to avoid collision.

### 2.5 Xero actuals (optional enrichment)

`XeroLedgerLineEntity` carries `ContactName` and `CostCenterCode`, so **paid-to-date per supplier per cost centre** is derivable if we want an "invoiced/paid" figure under each supplier column later.

---

## 3. Proposed table

Rows: one per **cost centre** with activity on the project (same row set as Financials). Sticky left columns, horizontal scroll for supplier columns — same treatment as `FinancialsTable.razor`.

| Code | Cost Centre | Contract value | Budgeted cost (−10%) | Completion % | Expected cost @ completion | ⟨Supplier A⟩ | ⟨Supplier B⟩ | … | Total committed | Uncommitted / variance |
|---|---|---|---|---|---|---|---|---|---|---|

- **Contract value** — budgeted sales per cost centre (counting valuation lines).
- **Budgeted cost** — contract value × 0.9.
- **Completion %** — from the latest claim (amount-weighted, as Financials does today).
- **Expected cost @ completion** — budgeted cost × completion %. This is the money we'd expect to be paying out for that line right now.
- **Supplier columns** — one per supplier holding a work order on the project; cell = order value allocated to that cost centre. Column header links to the supplier; cell click could open the work order.
- **Total committed** — sum of supplier cells on the row.
- **Uncommitted / variance** — expected cost (or budgeted cost — see open question 3) less total committed: shows unprocured exposure per line.
- Footer totals row across all columns.

---

## 4. Scope of work

### 4.1 Data model (one migration)

1. **`WorkOrderAllocationEntity`** — `WorkOrderAllocationId`, `WorkOrderId`, `CostCode`, `Amount`. A work order's value split across one or more cost centres (single-cost-centre orders are one row). Keyed by id only, no FK constraints, matching every JPMS table.
2. **`WorkOrderEntity.BidPackageId` becomes optional** — permits direct (non-tender) orders. Existing award flow unchanged.

Alternative considered: a single `CostCode` column on the work order. Rejected — real orders (groundworks + drainage, supply-and-fit packages) span cost centres, and the allocation table costs little more.

### 4.2 API (`api/Features/ProjectCashflow/` + `Procurement` additions)

- **Query `GetProjectCashflow`** — returns per-cost-centre rows (claim block reusing the financial-summary calculation) + the supplier/allocation matrix, in one payload. Contracts in `contracts/ProjectCashflow/`.
- **Command `CreateWorkOrder`** — supplier, value, scope, allocations; bid package optional.
- **Command `UpdateWorkOrderAllocations`** (and extend existing `UpdateWorkOrder`) — edit value/scope/allocations.
- Award flow: when a bid package is awarded, pre-seed allocations from the package's line coverage where mappable; otherwise leave unallocated for manual assignment.

### 4.3 Front-end (jpms)

- `Pages/ProjectCashflow.razor` — `@page "/projects/{ProjectId}/cashflow"`, wrapped in `ProjectPageShell`, auth/session handling copied from `ProjectFinancials.razor`.
- Tab added to `Components/ProjectTabNav.razor` after Financials.
- `ProjectCashflowReadModel` store — per-project keyed cache, `RefreshAsync(projectId)` called once from `OnInitializedAsync` (stale-while-revalidate, per the front-end convention in CLAUDE.md).
- `Components/CashflowTable.razor` — modelled on `FinancialsTable.razor` (sticky code/name columns, right-aligned money, hide-empty-rows toggle).
- **Work order entry UI** — an "Add work order" action on the Cashflow tab (and/or the bid package detail page): supplier picker, value, scope, allocation editor (cost centre + amount rows that must sum to the order value). Unallocated orders surface in an "Unallocated" bucket on the tab so nothing silently disappears.

### 4.4 Out of scope (v1)

- Time-phased forecasting (weekly/monthly curves, the 13-week company view) — this tab is the current position, not a forecast.
- Matching Xero bills to individual work orders; paid-to-date per supplier can follow (§2.5).
- Retention on the supplier side, contra charges, CVR integration (workflow 07 territory).

---

## 5. Open questions for discussion

1. **Row granularity** — cost centre (proposed, matches Financials and how claims aggregate) or individual valuation line items? Line-level `PercentComplete` exists, so a per-line drill-down (expand a row) is a natural v1.1 if wanted.
2. **Which claim** — latest claim regardless of status (Financials behaviour today, reflects live site progress) or last confirmed claim only?
3. **Variance baseline** — compare committed orders against *expected cost at completion* (cash-now view) or against *full budgeted cost* (procurement-coverage view)? Could show both.
4. **Supplier cell contents** — committed order value only (proposed for v1), or committed + invoiced/paid from Xero underneath?
5. **Where work orders are added** — Cashflow tab only, or also from bid package award and the Subcontractor detail page?
6. **Naming** — happy with "Cashflow" as the tab label given the existing company-wide cashflow snapshot feature? (Code will be namespaced `ProjectCashflow` either way.)

---

## 6. Suggested delivery order

1. **Work-order capture** — migration, `CreateWorkOrder` + allocations, entry UI. (Standalone value: orders finally visible in the system.)
2. **Cashflow tab read view** — query, store, page, table.
3. **Refinements** — line-item drill-down, paid-to-date, bid-package pre-seeding polish.
