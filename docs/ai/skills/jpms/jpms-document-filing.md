---
name: jpms-document-filing
description: "How documents move from email to their registers — Document Triage and the drawing register's conventions. Load before filing attachments to Drawings, Payment Certificates or subcontractor compliance, registering drawings, or reasoning about revisions. Encodes revision inheritance, folder-first filing, the current-revision rule, date-only UTC certificate dates, and discard-never-delete."
---

# JPMS — Document filing

## Document Triage

- Every item is ONE email attachment copy, waiting to be filed to exactly one home: a drawing
  (file_document_as_drawing), a payment certificate (file_document_as_payment_certificate), or a
  subcontractor's compliance documents (file_document_to_subcontractor).
- Filing as a drawing REVISION inherits code and title from the target drawing — the item's own
  name may be junk; the register's identity wins. Filing as a NEW drawing resolves (or creates)
  its folder FIRST, then files into it.
- Certificate dates and compliance expiry dates are date-only, pinned to UTC — the stored day
  must never drift with anyone's timezone. Send plain yyyy-MM-dd.
- Discard is restorable and filed rows keep their where-it-went history — nothing in this queue
  is ever deleted. When unsure where something files, leave it Pending and ask; a wrongly filed
  certificate misstates what the client certified.

## The drawing register

- A drawing's CURRENT revision is its approved one, else its newest — trust the register's
  hasApprovedRevision flag, not label text.
- Registering a drawing (metadata) and adding a revision (the file) are separate acts; revision
  files only arrive by upload or from Document Triage, never invented.
- Approval is evidential — it records who approved and supersedes the previous approved revision.
  Never mark approval on anyone's behalf without their explicit say-so in this conversation.
- Deleting a REVISION and deleting the DRAWING are different destructive acts; both need the
  user's confirmed intent, named by drawing code.
