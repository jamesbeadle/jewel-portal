Title: FIX: the assistant reported a zero-total variation refusal as "portal cannot", and named the wrong Xero number on VI-0004
Branch: fix/zero-total-refusal-reported-as-portal-cannot → main

Jeremy's By France run, 23/09 06:57–08:03 (forwarded to James with "why"). The end state was right — Valuation 20 locked, VI-0005 issued with INV-0223, V81 (£7,843) and V89 (EOT-06 prelims) raised at 07:01 — but on the way the assistant read a blank summary-sheet cell as nil, was refused by the zero-total validation, reported the refusal as "PORTAL CANNOT — James Beadle", and separately told Jeremy VI-0004 carried INV-0219 (the register has held INV-0106 since August).

**What changed**
- `list_variations` rows carry `estimatedValue` and `approvedValue` (null until approval), matching `get_variation_context`, so an Issued variation no longer reads as `Value 0.0000`; the description says `search` matches titles and a number is `find_by_reference`.
- The zero-total refusal has one wording, `VariationLineTotals.ZeroTotalMessage` (contracts), shared by the manual create, stage, revise and the approve panel; it now says the figure is usually on the item's own tab.
- Stored skills `jpms-connector-mechanics` (a refusal is never "portal cannot"; a Xero number is read off `list_valuation_invoices`, never remembered) and `jpms-variation-lifecycle` (a workbook's summary sheet is an index) — saved to the portal already, repo copies under `docs/ai/skills/jpms/`, mirrored in the jpms-operator reference.

**Checks**: worker link check passed; fast audit 74.2%, gate passed. No SDK on the machine, so CI is the compile check.

**Left**: nothing — YBT task c59a023a.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01Xmg22FewQFcnGKgWxSuXKj
