---
name: jpms-xero-allocation
description: "How Xero and the portal reconcile — allocation doctrine and why the ledgers read the way they do. Load before reading or discussing Xero costs, aged payables/receivables, cost-of-sales spend, work-order invoice links, or anything built on allocated lines. Encodes drafts-are-deliberate, allocation-moves-money-views, the complete-slice-list rule, and labour-settlement bills."
---

# JPMS — Xero allocation doctrine

## Why the portal's numbers beat Xero's reports

- **Draft bills are deliberate.** The coding procedure holds purchase bills in DRAFT until they
  are allocated through the portal, so Xero's own aged-payables report UNDERCOUNTS what is owed.
  Quote get_aged_payables / get_aged_receivables (drafts included), never Xero's report, for what
  we owe or are owed.
- **A cost only reaches a project when its line is ALLOCATED** to a project + master cost centre
  (or split across several). Unallocated lines are real money not yet in any project's spend —
  when project cost figures look light, check the Unallocated queue first
  (list_xero_ledger_lines).

## Allocation rules (the page's arming rules, as doctrine)

- A line goes to ONE of: project + cost centre, or a bucket (no project, no cost centre) —
  when both are somehow set, the bucket was the later deliberate act and wins.
- Half-allocated states exist on purpose: a line can carry its project (moving it to that
  project's tab and writing the Xero site) without being allocated to a centre yet.
- Disputed lines are a conversation, not an error state — the thread survives resolution.
- **Labour-registry suppliers' bills are timesheet SETTLEMENT, not costs** — they bypass the
  allocation queue entirely; labour cost enters projects through approved timesheets.
- **A bill from a supplier with an open work order is a Work Order bill** (2026-09-08): its
  project and cost code(s) were decided when the order was approved, so it is never coded by
  hand. It sits on the allocation page's Work Order bills tab as ONE card per bill, pre-filled
  from the order (project, cost code split pro rata to the order's lines, order value, invoiced
  to date, remaining), and one Approve allocates every line, links each to the order for its
  net, writes Sites + Cost Code tracking to Xero and approves the bill there. "Open" is Released
  with value still left to invoice. The approval is audited (who, which rule) and undone as a
  bill: allocation, links and Xero tracking reverse together — but Xero never un-approves, so
  an approved bill stays awaiting payment there.
- **The matching ladder** (2026-09-11, the accountant's rules), top rung first, run on every
  unallocated read: (1) a WO number on the bill names the order — supplier + number, the bill's
  site breaks a tie; lines naming their own orders are proposed line by line; (2) the supplier
  has exactly one open order; (3) the bill's net is exactly what is left to invoice on one
  order, or (4) on one unique set of the supplier's open orders (£3,092 = WO-0055 £1,748 +
  WO-0056 £1,344) — each order's remaining value is its figure. On every rung the figures are
  proposed and the card needs only Approve; `workOrderBill.rule` says which rung. **Reference
  beats amount**: when `workOrderBill.amountNote` is set the net fits a DIFFERENT order than the
  bill names — the match stays on the reference; read the note out and let the user decide.
  Amount is bill net (ex VAT, a credit note negative) against order value less what is already
  linked to it — both net, to the penny.
- **When the ladder runs out** (no order named, and the total is a part payment or fits more
  than one way) the bill is still a Work Order bill — never the plain queue, which would lose the
  order link — and its figures have to be keyed: that card is the **Finance Director's** (queue
  `WorkOrderBillHeldForFinance` for everyone else; the owner never sees a money field; the server
  refuses other roles). As the FD, never invent the split: show the supplier's open orders with
  their remaining values and take the figure per order from the user.

## Reading the queue (2026-09-11)

- **The raw Unallocated count is NOT the to-do.** `list_xero_ledger_lines` with no status
  returns `tabBar` — the page's own tab bar: `toCode` (lines wanting a project + cost centre),
  `workOrderBills` (BILLS, one Approve each — not lines), `labourOutstanding`, `labourCovered`,
  and `awaitingAction` = toCode + workOrderBills + labourOutstanding. Quote those, never
  `counts.unallocated`, for "what is outstanding on Xero allocation".
- **Every Unallocated line carries `queue`** — the tab the page shows it in, by the page's own
  rule: `ToCode` (`projectTab` names the project tab, blank = the plain Unallocated tab),
  `Labour` (a labour-registry worker's bill awaiting the settlement run — settlement, not a
  cost to code; `labour` carries the worker), `LabourCovered` (already settled by an approved
  timesheet — nothing to do), `WorkOrderBill` (approve_work_order_bill), or
  `WorkOrderBillHeldForFinance`. Pass `queue` to read one tab. A line whose `queue` is not
  `ToCode` is never coded with set_xero_allocation.

## Where a bill stands in Xero (2026-09-08)

- Every line carries `xeroStatus` — the bill's status as Xero last reported it (DRAFT /
  SUBMITTED / AUTHORISED / PAID / VOIDED), refreshed by every sync and stamped after every
  write-back. That, not `writeBackStatus`, answers "is it still draft in Xero?": `None` also
  covers bills approved outside the portal.
- `writeBackStatus` is what the portal's write did NOW; `writeBackError` and
  `writeBackFailedAtUtc` are the LAST failure and are kept through a later success, so a bill
  that failed and then approved on retry still says so. On the allocation page the Allocated
  tab's chips "Draft in Xero" and "Write-back failed" narrow to exactly these, and the export
  carries Xero status, write-back and last error.

## Work-order invoice links

set_xero_line_work_order_links takes the line's COMPLETE slice list every time — read the
current links first (list_xero_ledger_lines with the projectId), modify, resend the whole set.
Sending a partial list silently drops the missing allocations.
