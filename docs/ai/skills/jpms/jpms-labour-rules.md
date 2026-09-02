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
   - **A week that straddles the month end signs off per month** (2026-09-02, the accountant's
     ask: August must close on the 1st). view_worker_month marks such a week with `monthPart`;
     pass `monthStart` (any date in the month you are closing) to sign that month's days only —
     31 Aug signs August's part of the week of 31 Aug, 1 Sep signs September's. The new month's
     days never hold the old month's settlement up, and a whole week inside one month is
     unchanged (one marker).
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

## Settlement identity (2026-08-31)

- Every worker settles through a COUNTERPARTY: their linked directory company, or themselves when
  flagged a sole trader (they bill Dext/Xero under their own name). The company link always wins.
- A "no settlement identity" refusal is fixed with **link_worker_to_company** or
  **set_worker_sole_trader** — NEVER by inventing a directory company for a sole trader; fake
  companies pollute compliance and tendering.
- **import_xero_supplier** auto-links workers whose names match the imported supplier;
  **reconcile_worker_directory_links** (confirm-first; run apply:false first) backfills the rest
  and reports the ambiguous/unmatched remainder for a human decision.

## The chase list (2026-08-31)

- A day is chased only when it was EXPECTED: inside the worker's engagement window AND the worker
  is contracted, assigned to a project, or holds an open sign-in. Signed-off weeks, settled
  worker-months and dismissed days never chase, and the unconfirmed-cost figures always agree
  with the list.
- **view_labour_chase** reads the list; a wrong item is answered with a timesheet
  (submit_worker_week), an absence (record_worker_absence), or **dismiss_labour_chase_day** with
  a REAL reason (audited; restore_labour_chase_day undoes). A worker chased every day needs the
  real fix — contracted days, an assignment, or engagement dates — not day-by-day dismissals.
