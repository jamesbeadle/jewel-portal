---
name: jpms-variation-lifecycle
description: "The variation's one-document lifecycle and the staged build-up doctrine. Load before pricing, staging, approving, revising or reporting on variations, or working an Awaiting-AI position. Encodes one-number-through-every-stage, stage-then-USER-approves, what approval mints, evidence via architect instructions, the work-order fallback, and a workbook's summary sheet being an index and never the price."
---

# JPMS — The variation lifecycle

## One document, one number

A variation is ONE record through every stage; a user reads it as **V72**. Its status says where
it has got to: Quoting → Issued → Awaiting AI → Approved / Rejected. Never speak of "VOQ" and
"VO" as two things, and never invent a second number.

## Pricing and approval

- **Stage, don't approve.** Build the priced lines with stage_variation_order_build_up — the
  staged TOTAL becomes the estimate, and the portal's approve panel opens pre-seeded. The USER
  presses approve. Only call approve_variation_order when the user has explicitly said, in this
  conversation, to approve that variation by number.
- **Approval mints the V-ref and mirrors the priced lines onto the Valuation Report** under the
  V-number, writes the QS accrual and commits budget. Revising after approval
  (revise_variation_order_lines / revise_variation_order_value) is a REAL financial act — the
  commercial records move by the difference; treat it with approval-grade care.
- **Awaiting AI means waiting for an Architect's Instruction.** The evidence lives in the
  instruction register: check list_architect_instructions for coverage, file the instruction from
  its email when it lands (import_architect_instruction_from_message), link it, THEN the
  variation can move.
- **A manual variation is raised priced and Issued.** create_manual_variation_order takes the
  build-up (lines, at least one, each on a cost centre) and lands in Issued — raised by hand means
  it has already gone to the client — with those lines staged and their total as the estimate.
  There is no Quoting pass to chase; the next move is the client's approval.
- **A workbook's summary sheet is an index, not the price.** A blank value cell on the summary
  (column F on By France's, 23/09/2026) means "the figure is on this item's own tab", never
  "this item is nil". Before raising or pricing any variation or EOT from a workbook, open the
  item's tab and take the build-up from there; a variation is only nil when its own sheet says
  so, and the portal refuses a zero-total build-up precisely so a blank index cell cannot become
  a nil variation. On the register, `list_variations` carries `estimatedValue` (the priced
  build-up) and `approvedValue` (null until approval) — an Issued variation with no approved
  value is priced, not nil. A number (V81) is found with find_by_reference; `search` matches
  titles only.
- Pre-approval estimate changes use set_variation_order_estimate; the status ladder's
  side-effect-free moves use set_variation_order_status; rejection and return-to-quoting keep
  the same document alive, and reinstate_variation_order brings a rejected one back (Issued, or
  Quoting if never issued — unapproved, so a variation rejected after approval is re-approved).

## After approval

Issuing a work order straight from a variation is portal-only — fall back to
create_manual_work_order against the variation's cost centres, or tell the user to click it.
Client-facing variation documents show contract-basis rates; never expose subcontractor costs or
margin in anything client-bound (the commercial doctrine skills govern the wording).
