# Every email the portal sends

Written 2026-09-18 against `main` at 3f60230 (PR #49 merged), for to-do item 1 and the first
acceptance criterion of *Confirm Email Outputs from Portal and Allow Direct Send*: **every portal
email is listed and checked, with its content signed off by a director.**

The list is the checking. The sign-off is the column on the right, which a director fills in by
reading the email each row names — send one to yourself from the door, or read the body builder
this file points at.

## There are two sending channels, not one

Everything the task and its subtasks discuss is **channel A**. Channel B was never in scope and
nobody has looked at it.

**A — the shared projects mailbox**, via Graph. The email is staged as a draft in
`projects@jewelbb.co.uk`, sent from there, and its sent copy carries the record's tag, so it files
itself back into the portal and the audit trail records it. Two pipelines inside it:
`OutboundEmailDispatcher` (ten record doors) and `SendMailboxEmailHandler` (triage compose, which
is richer — it sanitises, extracts pasted images and files to to-dos).

**B — Azure Communication Services**, a different sender address on a different path
(`AzureEmailInviteNotifier`, `ImagineNotifier`). Never touches the projects mailbox: no tag, no
audit row, no sent copy in the portal, no record. Five emails leave this way, two of them to
prospects. **The SPF/DKIM/DMARC task must cover this sender too** — it is a second From domain and
the deliverability criterion ("arrives in the inbox, not spam") is not met by fixing channel A
alone.

## Channel A — the record doors, through the dispatcher

Each is staged → sent → degraded back to a reviewed draft if the mailbox refuses → audited. Eight
All nine offer Send with Save-as-draft beside it since 2026-09-18, when row 7 — the composer,
which sent there and then — grew one like its eight siblings (Nigel's decision).

| # | Email | Door(s) in the portal | Doc attached | Editor | Body sanitised | Connector | Director sign-off |
|---|---|---|---|---|---|---|---|
| 1 | RFI / NOD / EOT document | Request page → Email document | PDF + the request's files (over ~25 MB become links) | **none — server composed, never shown** | n/a | `send_request_email` | |
| 2 | RFI / NOD / EOT in bulk | Requests register → row actions | as above, per request | **none — server composed, never shown** | n/a | `send_request_emails` | |
| 3 | RFI / NOD / EOT as a reply in its thread | Request page → Email document → append to a chain | as row 1 | **none — server composed, never shown** | n/a | `send_request_reply` | |
| 4 | Work order purchase order | 5 doors, one handler (below) | PO PDF | **raw HTML textarea** | no | `send_work_order_po_email` | |
| 5 | Valuation report to client + architect | Valuation Report → claim card → Email statement | statement PDF (the locked claim) | **raw HTML textarea** | no | `send_valuation_statement_email` | snapshot folded into the claim 2026-09-18 |
| 6 | Subcontractor statement of account | Directory record → Statement | statement PDF | **raw HTML textarea** | no | `send_subcontractor_statement_email` | |
| 7 | Bid package invite (composed) | Bid package → composer | schedule, terms, tender docs, drawings | **raw HTML textarea** | no | `send_bid_package_invite` | |
| | *(recipients still cross the wire as semicolon-separated strings — the composer task's chips reach the API surface here)* | | | | | | |
| 8 | Bid package invite to the tender list | Bid package → invite tender list | as above | **raw HTML textarea** | no | `send_bid_package_invite_to_tender_list` | |
| 9 | Programme relevant-events reply | Programme → Communications → Reply | no | plain textarea | **yes** | `send_programme_reply` | |
| 21 | Variation order document *(added 2026-09-19)* | Variation page → Actions → Email to the client… | VO PDF | **none — server composed** | no | `send_variation_order_email` | |

The five doors behind row 4 — the PO page, the tender award, the Work Orders tab, the manual order
modal and the Control Centre's staged create — all call one handler and one body builder
(`jpms/Features/Procurement/WorkOrderPoEmail.cs`), so a fix there reaches all five. The tender
award is the exception that proves it: it composes **its own subject** in
`ProjectBidPackageInviteDetail.Award.cs:33` rather than calling `WorkOrderPoEmail.Subject`.

## Channel A — through the triage compose pipeline

The richer path: rich-text editor, recipient chips from the address book, pasted images extracted
to attachments, body sanitised by `ComposeHtmlPipeline`, to-do timeline updated on send.

| # | Email | Door | Connector | Director sign-off |
|---|---|---|---|---|
| 10 | Any email or reply from the Control Centre | Control Centre composer, `MailReplyComposer` | `send_mailbox_email` | |
| 11 | Defect to its supplier (raise or chase) | Defect page → Send to supplier | `send_defect_to_supplier` | |

## Channel A — the doors that still bypass the dispatcher

| # | Email | Where | What it does instead |
|---|---|---|---|
| 12 | RFI / NOD / EOT document, resent | `worker/MailboxIntake/Actions/MailboxActionWorker.cs:136` | **stages a draft and never sends** — a person finishes it in Outlook. Writes its own audit row by hand. This is what `resend_request_document` does, and its description said it sends (FIX raised). |
| 13 | Work order reply in thread | `PrepareWorkOrderReplyDraftHandler.cs:74` | draft only, own audit row |
| 14 | Reply to a triaged message | `ReplyInThreadFromMessageHandler.cs:84` | own draft + send |
| 15 | **Sales enquiry reply** | `api/Features/Sales/Inbox/ISalesMailbox.cs:94-98` | **creates a reply draft and sends it, with no audit row and no degrade path.** Not named on any task before 2026-09-18. |

## Channel B — Azure Communication Services

Different sender address, no tag, no audit row, no sent copy in the portal.

| # | Email | Where | Goes to | Director sign-off |
|---|---|---|---|---|
| 16 | "Set your password for Jewel JPMS" | `api/Auth/InviteEmailBody.cs` | a new portal user | |
| 17 | "Reset your Jewel JPMS password" | `api/Auth/PasswordResetEmailBody.cs` | a portal user | |
| 18 | "Your concepts from Jewel Bespoke Build" | `ImagineNotifier.SendConceptsReadyAsync` | **a prospect** | |
| 19 | "Your proposal from Jewel Bespoke Build — …" | `ImagineNotifier.SendProposalAsync` | **a prospect** | |
| 20 | Imagine activity notice | `ImagineNotifier.SendToSalesAsync` | the sales address | |

## Named on the task, but no such email exists

- ~~**Variation orders.** There is no door that emails one.~~ **BUILT 2026-09-19** (Nigel: it was
  a real gap, not a decision) — row 21 above. `VariationDocumentModel.EmailSubject` had been
  written and never called; it is now the subject of a real email, and it names the project once
  like its siblings. Only Issued, Awaiting AI and Approved variations may be sent.
- **Valuations and invoices.** By decision: the portal never emails a valuation invoice
  (`CLAUDE.md`). The valuation *report* is row 5.
- **Chasers.** None. The Chaser Agent is a separate backlog task; defect chasing is row 11.
- **Contractor's Report.** By decision: downloaded as Word or PDF and sent by hand.

## What is wrong, and who owns it

1. **Five doors show raw HTML in a textarea** (rows 4–8), and **three show nothing at all**
   (rows 1–3): `SendRequestEmail` and `SendRequestReply` take no subject and no body, so the cover
   note is built server-side and the person pressing Send never sees the email. That matters for
   this task's own acceptance criterion — **a director cannot sign off content the product does
   not show them.** Owned by *One way to write an email*, whose scope should say so: the job is a
   readable preview everywhere, not only an editor where a textarea exists today.
2. **Only two of twenty bodies are sanitised.** The dispatcher unified staging, sending and
   auditing — it did **not** unify the body pipeline. `ComposeHtmlPipeline` runs on rows 9, 10, 11
   and the sales reply only. Everything on the dispatcher path goes to Graph as the caller typed
   it, and rows 1–3 never pass through a caller at all. Owned by *One way to write an email*;
   worth naming explicitly there, because the dispatcher looks like it closed this and did not.
3. ~~**The subject still repeats the project on request documents.**~~ **FIXED 2026-09-18.**
   The rule moved to `contracts/MailboxCompose/SubjectLine.NamingTheProjectOnce` and both the
   purchase order and the request document read it, so rows 1–3 name the project once.
   Pinned by `SubjectLineTests`.
4. ~~**Three subject builders for work-order mail.**~~ **FIXED 2026-09-18.** The award calls
   `WorkOrderPoEmail.SubjectForTenderAward`, which is `Subject` plus the package reference — so it
   gained the order's reference rule and its project name, both of which its own line lacked.
5. **Row 15 sends unaudited.** Add to *Every email … leaves an audit row*.
6. ~~**Three doors have no connector action** (rows 6, 7, 9).~~ **FIXED 2026-09-18.** All three
   have one. Row 6 needed an api ENDPOINT first — the command had a handler, gates and a DI
   registration and no `[HttpTrigger]` function, so the page's own button had never worked. Row 9
   needed an Authorisation and a Validation class, its gate having been a private field of its
   endpoint. Every send door is now confirm-first and reads "SENDS EMAIL", which four of them
   (rows 1–4) were not.
6a. ~~**Row 7 has no Save-as-draft**~~ — **FIXED 2026-09-18**, it grew one. It still passes
   recipients as semicolon-separated strings rather than the chips every other door is meant to
   grow; that half belongs to *One way to write an email*.
7. **Two docs still name `prepare_work_order_email_draft`** — `docs/ai/skills/jpms/jpms-tender-award.md:27`
   and a session note. The skill is loaded from the database, so it must be re-saved with
   `save_skill`, not just edited here. `EveryToolOrActionACatalogueTextNames_exists` does not read
   skill files, which is why the build did not catch it.
8. ~~**The request document's sent copy is not born on the Client pathway.**~~ **FIXED
   2026-09-18** — `SendRequestEmailHandler` tags `Client` as the reply and the worker already did.
   The valuation snapshot tag is a different, larger job and stays on its own task.
9. ~~**Line titles are not HTML-encoded** in the PO lines table.~~ **FIXED 2026-09-18** — the
   title and the unit are the supplier's own words and are encoded as text. The quantity
   formatting beside them was fixed on 17 Sept.

## Faults from the 9 Sept screenshot — where they stand

| Fault | Status |
|---|---|
| "Nothing sends from here" | **Fixed** for rows 1–11. Rows 12–13 still draft only by design; row 12 is what `resend_request_document` calls. |
| PO PDF not attached | **Fixed** — attached by the API on both send and draft, all five doors. |
| Subject repeats the project | **Fixed** — one rule (`SubjectLine.NamingTheProjectOnce`) on the work order, the tender award and the request document (2026-09-18). |
| Raw HTML in a textarea | **Open** — five doors (finding 1), plus the sanitising gap (finding 2). |
| Quantities at four decimals | **Fixed** — `QuantityText` on the shared lines table. |
