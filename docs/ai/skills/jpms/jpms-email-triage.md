---
name: jpms-email-triage
description: "How Jewel triages the projects mailbox — the Control Centre's procedure translated for the connector. Load before working the triage queue: listing untriaged mail, filing emails to records, raising records from emails, discarding, or replying. Encodes the apply ordering (file everything before any reply), the one-create-per-pass rule, attachments-before-body, the pane-choice-is-the-decision cross-filing rule, and when a thread counts as handled."
---

# JPMS — Email triage

The Control Centre stages a whole email's decisions and lands them in one Apply. Over the
connector you perform the same decisions as individual actions — so the ORDER the page enforced
by machinery, you must enforce by discipline.

## The order of work on one email

1. **Read the thread, not just the message** (list_mailbox_conversation). Later replies often say
   how the earlier messages should be triaged. If the thread already carries record tags
   (threadTags), the decision is usually to file to the same record — one step, not a re-triage.
2. **Attachments before body** (doc-first rule). If the email carries drawings, certificates or
   compliance documents, send them to Document Triage (send_attachments_to_document_control)
   before acting on the words.
3. **To-dos next** — anything the email demands of the team (create_todos_from_message).
4. **File to records** (file_email_to_record) — every record the email genuinely concerns. An
   email can feed a request AND a cost centre AND the programme at once; multiple filings are
   normal, not a smell.
5. **Create at most ONE new record per email per pass** (create_request_from_message,
   create_work_order_from_message, create_defect_from_message, log_tender_enquiry_from_message,
   create_inventory_item_from_message, …). Creating mints the record's tag onto the email, so the
   next filing decision sees it. If an email seems to need two new records, do the second on a
   second pass, after the first exists.
6. **Discard only what needs nothing** (discard_mailbox_message) — circulars, pure
   acknowledgements. Discard is restorable; when in doubt, file rather than discard.
7. **Replies LAST, and only as drafts.** Everything above must be filed before any reply is
   prepared, so a failed send loses nothing already filed. The connector never sends email —
   prepare_*_draft actions stage a draft in the shared mailbox and the human sends from Outlook.

## Decisions, not defaults

- **The pathway choice IS the decision.** Filing a Subcontractor-pathway thread onto a client
  record (or the reverse) is allowed — but it is a cross-filing the user must confirm. When an
  action answers that the thread is already filed under another pathway, ask the user; never
  silently pass allowCrossPathway true on your own judgment.
- **Project match is a guess until confirmed.** The queue's project hint comes from the email;
  say which project you are filing under and let the user correct it.
- **A thread is "handled" when its business is filed**, not when it has been read. Do not chase
  an inbox-zero count; chase every email's business landing on the right record.

## What you cannot do (by design)

Sending email (all paths are draft-then-human-sends) and bulk retagging are portal-only. Say so
rather than improvising.
