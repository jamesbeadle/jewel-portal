---
name: jpms-valuation-cycle
description: "The monthly valuation claim and invoice cycle — the money path from % complete to cash, including raising the sales invoice in Xero and reading payments back from it. Load before any valuation, claim or valuation-invoice work: recording progress, preapproving, raising/submitting/issuing invoices, raising in Xero, payments, or presenting a statement to anyone. Encodes the claim stepper, the frozen-snapshot client rule, cumulative seeding, server-stamped retention, what certified-to-date means, the Xero raise rule (a contact that would be created is a stop, not a go) and the payment rule: Xero is the home of what has been paid — read it, never ask."
---

# JPMS — The valuation cycle

## The stepper (one claim, in order)

1. **Value the month**: record cumulative % complete per line (claim_progress /
   record_claim_entries). New claims ALWAYS seed from the latest claim's cumulative position —
   never start a month from zero.
2. **Lock**: preapprove_valuation_claim freezes the month's figures for claiming.
3. **Raise the invoice** (create_valuation_invoice): raising freezes a REPORT SNAPSHOT — that
   frozen statement is what the client is sent, backing this invoice. Nothing is emailed by the
   portal; a person sends the statement.
4. **Record claim sent** (submit_valuation_invoice): records that the statement went to the
   architect/client. It changes portal state only.
5. **Record approval**: record the client's approval (or rejection — a rejected invoice returns to
   draft for amend-and-resend). "Issue without approval" is legitimate only for clients with no
   formal approval loop — ask before using it.
6. **Raise in Xero & issue** (raise_valuation_invoice_in_xero): creates the AUTHORISED sales
   invoice in Xero and issues here in one press. Issuing is what moves CERTIFIED-TO-DATE. Until
   issued, the money is exposure, not certification. See the Xero raise rules below.
7. **Payment**: read it from Xero — `preview_valuation_invoice_payment_sync` then
   `sync_valuation_invoice_payments_from_xero` records Paid for every issued invoice Xero holds as
   PAID (see Payments below). Payment is NOT a gate for starting the next claim — the next month
   begins on its own clock.
8. **Confirm & roll over**: confirming closes the claim into history. Confirming without an
   issued invoice earns a nudge, not a block — mention it to the user.

Buttons and actions that say "Raise …" create a portal record; ones that say "Record …" record
an outside event. None sends.

## Naming the claims

A claim's name is free text and can be changed at any status (`rename_valuation_claim`,
valuationClaimId from `get_valuation_context` or `list_valuation_snapshots`). The house form is
**`Valuation NN - Month YYYY`** — "Valuation 05 - September 2026" — the same NN the invoice and
the Xero reference carry, so the claim picker, the statement and Xero all read the same. When
the user asks to tidy the names, list the claims with their numbers and current names, propose
the renamed set, get the yes, then rename each one. Names only — nothing financial moves.

## Raising in Xero — preview, then stop or go

1. Always `preview_valuation_invoice_xero_raise` first and show the user everything it returns:
   the Xero contact mapped on the project and whether Xero still holds it, net, VAT reading,
   Sites tracking, reference and description, invoice date, due date, the certificate.
2. **The contact is the one MAPPED ON THE PROJECT.** The raise never matches the client by name
   and never creates a contact. If the preview's blockers say no Xero contact is mapped (or the
   mapped one is not found in Xero) — STOP, do not raise, and map it yourself:
   `list_xero_customers` (the same list Project settings shows), find the contact whose name
   matches the project's client or address the way a person would — spelling and abbreviations
   differ, "Ravenswood Ave" is "64 Ravenswood Avenue" — show the user the proposed contact (name
   and town), and on their yes `set_project_xero_contact` (projectId, xeroContactId). The portal
   re-reads the contact from Xero and stores Xero's own name. Then preview again. If nothing in
   the list matches, say so — the client may not be in Xero yet, and that is for a person to
   create in Xero, never the raise.
3. **Dates come from the user.** `invoiceDate` and `dueDate` (yyyy-MM-dd) go on the preview and
   on the raise. If the user did not say, ask; the defaults are today and the certificate's issue
   date + the contract's final date for payment days (else Xero's sales default), and the preview
   shows exactly which applied. Never raise with a date the user has not seen.
4. **Numbering comes from the INVOICE.** Xero's reference is `Valuation NN` (the valuation
   invoice's number, two digits — VI-0005 → "Valuation 05") and the line reads
   `Valuation NN - Payment due as per <Month yyyy> valuation report (ex VAT)`. Read both back
   from the preview before the yes; the certificate is attached, not described.
5. Take the user's explicit yes, then `raise_valuation_invoice_in_xero` with confirm true and the
   SAME invoiceDate/dueDate the preview showed. An invoice already carrying a Xero id or number is
   refused a second raise; a wrong invoice is voided in Xero, never un-raised.
6. **A Xero invoice raised by hand is recorded, not re-raised — and its number is READ, not
   asked for.** When a valuation invoice carries no Xero number (list_valuation_invoices shows
   `xeroInvoiceNumber` blank), read `list_xero_sales_invoices` for the project — every sales
   invoice on the project's mapped Xero contact, paid ones included — and match it the way a
   person reading both lists would: the net amount first (`net` against the portal invoice's
   `Amount`), then the date and the reference ("Valuation 05"). Propose the pairing to the user
   ("VI-0005 £12,000 net looks like INV-0227, dated 3 Sep, reference Valuation 05"), and on their
   yes `record_valuation_invoice_xero_number` (for one not yet issued, `issue_valuation_invoice`
   with `xeroInvoiceNumber` does both). Never pick between two rows that both fit — show both and
   let the user choose. Nothing is written to Xero either way, and the row then reads as raised.

## Payments — Xero is the home of what has been paid

- **Never ask the user whether an invoice was paid when Xero can be read.** The portal READS
  Xero rather than being told: `preview_valuation_invoice_payment_sync` (projectId) shows, per
  ISSUED valuation invoice, what Xero holds — PAID (with the fully-paid date), part paid, unpaid
  — and what the sync would do: record the payment on a linked invoice Xero holds as PAID; link
  an invoice with no Xero number to the one Xero row that matches it (reference naming the
  valuation, else the same net to the penny) and record it if PAID; nothing for the rest, with
  the reason. Show the user every row — invoice, Xero number, action, amount, paid date, note —
  take their yes, then `sync_valuation_invoice_payments_from_xero` with confirm true. The
  amount recorded is the portal's net; a Xero net that differs is flagged in the note, not
  silently taken.
- **Ambiguous is a stop, never a guess.** A row marked Ambiguous lists the Xero invoices that
  fit; resolve it by reading `list_xero_sales_invoices`, proposing the pairing, and
  `record_valuation_invoice_xero_number` on the user's yes — then preview and sync again.
- **`record_valuation_invoice_payment` is for a payment Xero does not hold** (a receipt outside
  Xero, or Xero unreadable) — not the first move.
- **The nightly worker does the same on its own** for every project with a Xero contact mapped,
  so an invoice paid in Xero yesterday reads Paid here this morning without anyone asking. A
  project with no Xero contact mapped is skipped by the night and blocked in the preview — map it
  (`list_xero_customers` + `set_project_xero_contact`), never create a contact.

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

## Correspondence

- **The live claim is a record in its own right.** Mail about the period — what to claim, the
  QS's working, the architect's early queries — files to the claim (file_email_to_record, type
  ValuationClaim, recordId = the claim's ValuationClaimId from get_valuation_context) and reads
  back with read_record_emails (recordType valuation_claim). Its mail tag is
  JPMS/VAL-{project reference}-{claim number}.
- **A snapshot inherits its claim's mail.** Every snapshot frozen from a claim shows the claim's
  correspondence beside anything tagged to the snapshot itself (type ValuationReportSnapshot),
  so the statement carries the period's whole story; the client's reply to a sent statement
  can go on either.
- **Roll-over moves the tag on its own.** Confirm & roll over starts the next claim with the
  next number — new mail files to the new period; nothing is re-tagged.
