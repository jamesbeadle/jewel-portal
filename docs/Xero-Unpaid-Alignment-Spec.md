# Aligning the portal's unpaid purchase figures with Xero — spec

**Status:** Built 28 Jul 2026 (§3.1–3.5 implemented; §3.6 data fixes outstanding)
**Author:** Cowork (for Nigel Reilly)
**Date:** 28 July 2026

---

## 1. Why

The By France reconciliation (portal cashflow export 28 Jul vs Xero Aged Payables Detail as at
31 Jul, sites = By France) proved the two systems hold the same population but state it
differently. 47 of 51 invoices tie to the penny once VAT presentation is accounted for. The
remaining £76,308 of difference has exactly three causes:

1. **Part-payments are invisible.** The portal's paid/unpaid test is binary
   (`InvoiceStatus == "PAID"`), so a bill Xero has largely settled still counts in full.
   On By France that is £89,189.12 across three bills — Generation Windows 3367 (£80,000 net
   counted, £8,000 outstanding), Jewel Property Serve INV-0811 (£24,928.47 counted, £19,000
   outstanding), Matthew Luke Avis 084 (£12,380 counted, £1,119.35 outstanding).
2. **Unallocated bills are invisible.** Anything Electrical inv 1657 (£10,000) is on the Xero
   aged payables for the By France site but has no allocated line in JPMS, so no project view
   counts it anywhere.
3. **Net vs gross.** The portal states net per line; the aged payables states gross
   outstanding per bill. Correct on both sides, but it makes eyeball reconciliation hard —
   the export should offer the gross figure so the totals tie without a spreadsheet.

Goal: the Cashflow tab's "Unpaid Xero purchase invoices" figure equals the true net still
owed, and the breakdown modal's gross total ties to an Aged Payables Detail run for the same
site at the same sync point.

## 2. What exists today (verified in the codebase)

Most of the machinery arrived with work-order paid positions (27 Jul) — this spec mainly
extends it to the cost-of-sales surfaces:

- **`XeroLedgerLineEntity.InvoiceTotal` + `.AmountDue`** (migration
  `20260727120000_AddXeroLinePaymentState`) — the bill's gross total and gross outstanding,
  repeated on every stored line, refreshed by every ledger sync (`SyncXeroLedgerHandler`
  lines 168–171; credit notes map `RemainingCredit` → `AmountDue` in `XeroClient`). Rows
  synced before the migration read 0 and self-heal on the next sync.
- **`XeroPaymentMaths`** (`contracts/Xero/`) — `SettledFraction` / `PaidPartOfSlice`: the
  CIS-safe settled fraction (driven off AmountDue, not AmountPaid, so a CIS deduction never
  reads as unpaid), with `InvoiceStatus` as the fallback while amounts are unsynced.
  Already used by `WorkOrderPaidPositions` — work-order paid figures are part-payment-aware
  today.
- **`ProjectCostOfSalesLine`** (`contracts/Commercial/ListProjectCostOfSalesLines.cs`) — the
  Cashflow tab's line population. Carries `InvoiceStatus` and binary `IsPaid` only; the
  payment amounts are **not** passed through. `ProjectCashflow.razor` sums
  `Net where !IsPaid` — the binary test this spec removes.
- **`ProjectEntity.XeroSiteName`** — maps a project to its Xero site tracking option;
  `XeroLedgerLineEntity.XeroSite` holds each line's tracking as synced. Together they let
  JPMS attribute an *unallocated* line to a project — exactly how the aged payables report
  was filtered. `XeroAllocationSuggester` already normalises and matches these names.
- The only purchase-side consumers of the binary flag are `ProjectCashflow.razor` and
  `UnpaidXeroInvoicesModal.razor` (verified by grep — `XeroAllocation.razor` displays the
  status string but sums nothing from it; `CashSummary.razor` reads the sales side live
  from Xero and is unaffected).

**No new sync work and no new migration are needed.** Every figure below is derivable from
columns already on `XeroLedgerLines`.

## 3. Changes

### 3.1 Contract — carry the payment state through (`contracts/Commercial`)

Add to `ProjectCostOfSalesLine`: `decimal InvoiceTotal = 0m`, `decimal AmountDue = 0m`
(defaults keep every existing caller compiling), plus derived members so each consumer does
not reinvent the maths:

```csharp
public decimal SettledFraction => XeroPaymentMaths.SettledFraction(InvoiceStatus, InvoiceTotal, AmountDue);
public decimal PaidNet        => XeroPaymentMaths.PaidPartOfSlice(Net, InvoiceStatus, InvoiceTotal, AmountDue);
public decimal OutstandingNet => Net - PaidNet;
// Gross outstanding for tying to Xero: this line's share of the bill's AmountDue.
// The line's own Tax column carries its VAT, so gross = (net + tax) share of what's unowed.
public bool IsPaid            => SettledFraction == 1m;  // replaces the status-string test
```

`IsPaid` keeps its name and meaning ("nothing further owed") so `FinancialsTable` /
`CostCentreCostOfSalesModal` semantics don't shift — a fully settled CIS bill reads paid
exactly as before.

Split shares (`IsSplit`) scale their own share: the split's `Net` is the slice, the bill-level
`InvoiceTotal`/`AmountDue` still apply, so `OutstandingNet` apportions part-payment pro-rata
across the split — same rule Xero applies to the bill as a whole. Add `decimal Tax = 0m`
passed through the same way (whole line: the line's tax; split share: tax × share/net) so the
modal can state gross.

### 3.2 API — pass the columns through (`ListProjectCostOfSalesLinesHandler`)

Mechanical: both the whole-line and split-share projections add `line.InvoiceTotal`,
`line.AmountDue`, and the tax share. Credit notes keep their negative sign on `Net` as today;
`PaidPartOfSlice` already carries the sign so an applied credit's settled part subtracts.

### 3.3 Cashflow tab (`ProjectCashflow.razor`)

- `BillsUnpaid` becomes `Σ OutstandingNet` over **all** cost lines (a fully paid line
  contributes 0, so no filter needed — and a part-paid line contributes only its remainder).
  On By France this moves the row from £171,711.62 to roughly £78–83k net (the exact figure
  depends on each part-paid bill's VAT basis, which the synced `InvoiceTotal` resolves) and
  the practical / project completion cashflows by the same amount.
- Row subline when part-payments exist:
  `"£X on part-paid bills already settled"` — so the drop from the old figure is explained
  on the face of the statement.
- The row keeps stating **net** — every cost figure on the tab is net, and VAT on unpaid
  bills is recoverable on payment; the gross tie-out lives in the modal (3.4). If we later
  want a gross cash view, it is one derived column, not a schema change.

### 3.4 Breakdown modal (`UnpaidXeroInvoicesModal.razor`)

- Population: lines with `OutstandingNet != 0` (replaces `!IsPaid`).
- Columns become: Date · Supplier · Invoice · Description · Centre · Xero status ·
  Line net £ · **Settled £** · **Outstanding net £** · **Outstanding gross £** — the last
  ties line-for-line to the Aged Payables Detail TOTAL column (part-paid bills show their
  true remainder; 1p VAT-rounding differences remain possible, as Xero rounds per line).
- Footer totals all four money columns; the gross total is the number to hold against the
  aged payables run.
- Excel export mirrors the columns; keep the split-line flag.

### 3.5 The unallocated guard — nothing tracked to the site goes uncounted

New query `ListUnallocatedSiteBills(projectId)` (`api/Features/Commercial/Queries/` or
`Xero/Queries/` — either home works, keep the contract in `contracts/Commercial`):

- Population: `XeroLedgerLines` where `AllocationStatus == Unallocated`, `AmountDue != 0`
  (or `InvoiceTotal == 0 && InvoiceStatus != "PAID"` for pre-migration rows), and `XeroSite`
  matches the project's `XeroSiteName` (reuse `XeroAllocationSuggester`'s normalisation —
  do not string-compare raw).
- Returns supplier, invoice number, date, net, outstanding gross, ledger line id.
- Surfaces in two places:
  - **The modal**, as a second section under the allocated table:
    *"In Xero for this site but not yet allocated in JPMS"* — with a link/hint to the Xero
    allocation screen. Included in the export on a second sheet.
  - **The statement row subline**, as a warning count when non-empty:
    `"⚠ £10,000.00 tracked to this site in Xero is not yet allocated"` — the statement's own
    figure stays allocation-based (allocation is what distributes cost to centres), but the
    gap is no longer silent.
- Deliberately **not** auto-allocated and **not** added into `BillsUnpaid`: allocation is a
  human decision (project + cost centre), the guard just makes the queue visible where the
  money is missed.

### 3.6 Data fixes surfaced by the reconciliation (one-off, no code)

- Allocate Anything Electrical inv 1657 (£10,000, Feb 2026) on the Xero allocation screen.
- Confirm the payments on Generation Windows 3367 in Xero (£8,000 of £96,000 gross
  outstanding) — if that bill is actually disputed rather than part-paid, the portal figure
  was closer to the truth than Xero's.
- Anything Electrical 1678 ties at net assuming reverse-charge VAT; if it carries 20% VAT it
  is part-paid by £3,000 — 3.1's maths will state it correctly either way once amounts sync.

## 4. Acceptance

1. Cashflow "Unpaid Xero purchase invoices" = Σ net outstanding per allocated line
   (part-paid bills at remainder, settled bills at 0), on every project.
2. Modal gross-outstanding total = Aged Payables Detail total for the project's site, to
   within per-line VAT rounding, when run against the same sync point.
3. A project whose site has unallocated unpaid bills shows the warning count; allocating the
   line moves it from the guard section into the table with no other action.
4. Work-order paid positions unchanged (they already use `XeroPaymentMaths`).
5. Pre-migration rows (InvoiceTotal 0) behave exactly as today until the next ledger sync —
   no backfill required, no figure goes backwards.

## 5. Delivery order

1. **Contract + handler passthrough** (3.1, 3.2) — small, compiles everywhere, no visible
   change until the page reads the new members.
2. **Cashflow figure + modal columns** (3.3, 3.4) — the headline alignment.
3. **Unallocated guard** (3.5) — new query + modal section + subline warning.
4. Data fixes (3.6) alongside, in Xero / the allocation screen.

Touches: `contracts/Commercial/ListProjectCostOfSalesLines.cs`,
`api/Features/Commercial/Queries/ListProjectCostOfSalesLinesHandler.cs`,
`jpms/Pages/ProjectCashflow.razor`, `jpms/Components/UnpaidXeroInvoicesModal.razor`,
plus the new query pair and its route registration. No migrations, no sync changes.
