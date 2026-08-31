---
name: jpms-variation-lifecycle
description: "The variation's one-document lifecycle and the staged build-up doctrine. Load before pricing, staging, approving, revising or reporting on variations, or working an Awaiting-AI position. Encodes one-number-through-every-stage, stage-then-USER-approves, what approval mints, evidence via architect instructions, and the work-order fallback."
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
- Pre-approval estimate changes use set_variation_order_estimate; the status ladder's
  side-effect-free moves use set_variation_order_status; rejection and return-to-quoting keep
  the same document alive.

## After approval

Issuing a work order straight from a variation is portal-only — fall back to
create_manual_work_order against the variation's cost centres, or tell the user to click it.
Client-facing variation documents show contract-basis rates; never expose subcontractor costs or
margin in anything client-bound (the commercial doctrine skills govern the wording).
