---
name: jpms-labour-rules
description: "Labour and timesheet doctrine — how hours become cost and what is immutable. Load before any timesheet, worker, absence or labour-cost work. Encodes view-code-approve order, approval immutability, the budget hard-block, rate confidentiality, sign-off freezing, and close-and-replace mappings."
---

# JPMS — Labour rules

## The order: view → code → approve

1. **view_labour_week first** — see the week's submitted days and their coding state.
2. **Code before approving** (code_worker_week): uncoded days REFUSE approval.
3. **approve_worker_week posts cost.** Approval snapshots the worker's rate effective on the
   worked date and posts the hours to Financials as actual labour cost. An approved timesheet is
   IMMUTABLE — hours and cost code can never change afterwards; the correction path is
   reject-and-resubmit (reject_worker_day, with a reason the worker reads) or a settlement
   variance. Never promise an edit to an approved row.

## Money rules

- **The budget hard-block is server-enforced**: approval is refused for a cost code whose
  remaining budget the new cost would exceed, and the refusal reports the code's figures. Relay
  the refusal; never route around it.
- Only APPROVED time is cost. Submitted time is exposure; quote them separately.
- **Rates are confidential to managing roles** — worker rates (list_workers) never reach site
  surfaces or any output a site role or subcontractor will see. Rate changes apply to FUTURE
  approvals only; history keeps its snapshots.
- Weekly sign-off freezes a worker-week before settlement; the Xero coding run refuses unmapped
  sites and codes by name — the fix is the mapping, not a guess.
- Xero mappings are effective-dated bridges: setting one CLOSES the old row and starts a new one
  (never edits), so historic reads still translate.

## The month-end chain (2026-08-31 — runs whole from the connector)

1. **view_worker_month / view_labour_week** — find what stands in the way: Submitted days,
   uncoded days, weeks not signed off.
2. **code_worker_week → approve_worker_week** per project (the money rules above apply).
3. **sign_off_labour_week** per worker-week (confirm-first). The server re-checks the signable
   rule: every elapsed day approved, rejected or a recorded absence. A refusal names the days —
   fix them, never work around them.
4. **view_settlement_month** — who will code, who will skip and why (FullySignedOff, verdict,
   lastCodingOutcome).
5. **run_xero_coding** (confirm-first) — recodes the covered Dext draft bill or stages a draft;
   everything lands DRAFT in Xero, approving the bill there stays human. Skips report their fix:
   a mapping gap → get_xero_mappings, then set_site_xero_mapping / set_cost_code_xero_mapping;
   not signed off → step 3. Already-coded months skip by design (run-once) — relay that.
6. After the user approves the bill in Xero and it syncs back: **set_xero_line_timesheet_cover**
   marks the line as settlement of the approved timesheets; when the totals will not tie and the
   difference is accepted, **add_labour_settlement_variance** (confirm-first) posts it visibly —
   never absorb a difference silently.
