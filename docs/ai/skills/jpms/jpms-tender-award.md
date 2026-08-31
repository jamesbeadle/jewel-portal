---
name: jpms-tender-award
description: "The bid-package tender flow from scope to purchase order. Load before building bid packages, handling incoming quotes, awarding, or raising the post-award work order. Encodes extract-never-hand-type for quotes, the human-sends-invites rule, directory hygiene for tender-only prospects, award-mints-the-WO, and the PO email as a distinct second step."
---

# JPMS — Tender and award

## The flow

1. **Build the package**: scope (update_bid_package_scope), line items, drawings. A bid package
   is a STANDALONE record grouping works across cost codes by trade — never a stage of the
   variation chain.
2. **Invite**: add recipients (invite_subcontractors_to_bid_package). The invite EMAIL itself is
   sent by a human from the portal — prepare everything, then hand over.
3. **Quotes arrive by email.** NEVER hand-type a quote's figures: run extract_tender_from_message
   on the email, review what it extracted with the user, then save_extracted_quote. A typo in a
   tender figure survives into the award and the work order.
4. **Award** (award_bid_package — confirm-first): awarding mints the work order to the chosen
   subcontractor.
5. **The PO email is a distinct second step** (prepare_work_order_email_draft): a draft in the
   shared mailbox for the human to review and send — the tool never sends.

## Directory hygiene

Tender-only prospects are NOT directory members. Promote a company into the directory only from
a submitted tender or at award (promote_subcontractor_to_directory) — the directory stays a
curated list of firms Jewel actually works with. Renaming a directory company to match its Xero
supplier name is what lines its invoices up on the allocation side.

## Quoting discipline

Never disclose one bidder's figures to another, and never put subcontractor pricing in anything
client-bound. Comparisons live in internal working documents only.
