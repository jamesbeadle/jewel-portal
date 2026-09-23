# Pull request for `feature/payment-terms-from-work-order-not-tender` (committed here, not pushed)

    git push -u origin feature/payment-terms-from-work-order-not-tender
    gh pr create --base main --title "A subcontractor learns the 30-day payment terms from the work order, not from the tender invitation" --body-file PR-payment-terms-from-work-order.md

---

**Title:** A subcontractor learns the 30-day payment terms from the work order, not from the tender invitation

**What changed.** Clause 3.1 of the bid package terms (`docs/bid-package-terms/JBB Bid Package Terms and Conditions.docx` and the `.pdf` re-rendered from it) no longer names a payment period. It read "Unless the Order states otherwise, invoices are paid within 30 days of receipt. Payment for partial shipments…"; it now reads "Invoices are paid on the terms stated in the Works Order. Payment for partial shipments…" (wording confirmed by Nigel, 23 Sep). The rest of the clause and retention at 3.2 are unchanged. Two pages as before; only 3.1's text differs.

**Why.** The 30 days was putting subcontractors off at tender, before they had priced; the Works Order already states the terms (header "Payment terms: N days" and the Invoice and Payment Requirements section), so the award is where they read it. Scope is the bid package route only — the work order is untouched.

**Checked.** Nothing else on the bid package route names a payment period: the invite email body, the pricing schedule workbook, the bid package pages and page guides, the tender skill and the connector's descriptions.

**Left for a person.** The live attachment is the PDF uploaded in Admin → System → Tender terms & conditions (`CompanyTenderTermsStore`), and that upload has no connector door — upload the new PDF there or the next invite still carries the old clause.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_016zT4bzH34jXg68BipfvaxF
