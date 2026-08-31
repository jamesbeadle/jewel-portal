---
name: jpms-valuation-cycle
description: "The monthly valuation claim and invoice cycle — the money path from % complete to cash. Load before any valuation, claim or valuation-invoice work: recording progress, preapproving, raising/submitting/issuing invoices, payments, or presenting a statement to anyone. Encodes the claim stepper, the frozen-snapshot client rule, cumulative seeding, server-stamped retention, and what certified-to-date means."
---

# JPMS — The valuation cycle

## The stepper (one claim, in order)

1. **Value the month**: record cumulative % complete per line (claim_progress /
   record_claim_entries). New claims ALWAYS seed from the latest claim's cumulative position —
   never start a month from zero.
2. **Lock**: preapprove_valuation_claim freezes the month's figures for claiming.
3. **Raise & send**: create the valuation invoice and submit it — raising freezes a REPORT
   SNAPSHOT; that frozen statement is what the client is sent, backing this invoice.
4. **Approval**: record the client's approval (or rejection — a rejected invoice returns to
   draft for amend-and-resend). "Issue without approval" is legitimate only for clients with no
   formal approval loop — ask before using it.
5. **Issue**: issuing is what moves CERTIFIED-TO-DATE. Until issued, the money is exposure, not
   certification.
6. **Payment**: record it when it lands. Payment is NOT a gate for starting the next claim — the
   next month begins on its own clock.
7. **Confirm & roll over**: confirming closes the claim into history. Confirming without an
   issued invoice earns a nudge, not a block — mention it to the user.

## Non-negotiables

- **The client sees the FROZEN snapshot, never the live report.** The live report is a working
  copy; anything presented, emailed or quoted as "the valuation" must come from the snapshot
  behind the invoice (get_valuation_snapshot). Comparing live vs frozen is how you answer "what
  moved since we claimed".
- **Retention is stamped server-side** from the project's terms — never compute or pass it.
- **Certified-to-date = issued + paid invoices (gross of deposit credits).** Quote it from
  list_valuation_invoices' summary, never by adding numbers yourself.
- Deleting claims or invoices is recovery machinery, not tidying — user's explicit say-so, named
  by number, every time.
