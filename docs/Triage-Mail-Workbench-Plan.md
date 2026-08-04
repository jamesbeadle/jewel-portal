# Triage as the PM team's mail workbench — 2026-08-04

Status: **Phases 1, 3 and 4 implemented** (this change set). Phase 0 is a manual Azure step —
required before anything sends. Phase 2 (page decomposition) is deliberately deferred; see the end.

## Why

The PM team barely used triage: emails built up and the actual job — replying — happened in
Outlook. Decision (Nigel, 2026-08-04): triage becomes the place the whole job gets done. Real
sending (reversing ADR-006's draft-only rule — the ADR carries the amendment), a mail-client
composer, and a way to deal with **every** email: link it to records, raise to-dos, reply to it, or
discard it (spam). Sender is always the shared `projects@jewelbb.co.uk` mailbox.

## What changed

### Sending (Phase 1)
- `IMailboxGraphClient.SendDraftAsync` — the ONE send call in the system: every outbound email is
  still staged as a draft first (auto-Cc of projects@, category stamping, large-attachment upload
  sessions — all the existing plumbing), then `POST …/send` on an explicit human click. 429 retried
  once honouring Retry-After. No agent tool is wired to it.
- `POST /api/mailbox/compose` (`SendMailboxEmail` → `ComposeOutcome`), triage-role gated, sender
  stamped server-side. Failure ordering: validate → resolve attachments → (optional) raise request
  → stage draft (+envelope PATCH, categories) → **send** → tag the inbound thread → audit. A failed
  send loses nothing: the draft stays in Drafts, nothing is triaged, the outcome carries the
  webLink ("open in Outlook") — same for the explicit **Save as draft** button.
- **The visible envelope is authoritative**: reply prefill (reply-all computed from the new
  To/Cc/ReplyTo/Subject fields on `MailboxMessageDetail`) is editable, and the server PATCHes the
  staged draft to exactly what was submitted. The projects mailbox keeps its Cc copy server-side.
- **`JPMS/Replied`** — a reply with no record chosen triages the thread with this ordinary
  workflow tag (+ the chosen pathway): answering an email IS dealing with it. Untag it on the
  Tagged tab to send the thread back to the queue. The old forced request creation is retired
  (`AlsoRaiseRequest` remains on the command for API callers, unused by the UI).
- Reply is available on **all three pathways** now, not just Client.
- **The compact pane** (Nigel's model, 2026-08-04 v4): the email (Outlook-style header + body +
  thread) with a small action strip under it — **↩ Reply** opens the composer in place, and
  **☑ Add a to-do / To-dos · n** opens the TO-DOS MODAL, its badge showing how many drafts are
  held. The modal is one dialog with two views: a LIST of the drafted items (title, assignees,
  due) and a DETAIL form you drill into (and back out of) to add or edit; drafts live in page
  state, nothing is created until the apply. Below the email sits the one remaining section,
  **File to a record**: pathway cards, then Link to existing / Create new / **Discard** — discard
  is a filing choice ("file it as nothing"), restorable from the Tagged tab. ONE action bar at
  the bottom states exactly what it will do ("This will send your reply, raise 2 to-dos and link
  this email to the selected record") and applies it in one click — to-dos first, then the
  filing/discard, then the send, every tag verified before anything saves. "Save reply as draft"
  applies the filing but stages the reply in Outlook Drafts. A reply alone still triages the
  thread as Replied + pathway (pathway required for that case); with a filing alongside, the
  record tag speaks and the Replied stamp is skipped; reply + discard is refused as a
  contradiction. The Subcontractor↔Internal cross-filing confirm interrupts the apply and re-runs
  it with consent.
- The reply-all Cc prefill filters out the projects mailbox itself (`MailboxMessageDetail.MailboxAddress`)
  — the server auto-Cc's it on every send, so showing it was noise.
- Audit: `EmailSent` (always written, whatever the pathway — recipients snapshot in Detail,
  webLink = the sent copy) and `EmailSendFailed`. Plain int values 13/14 — **no EF migration**.

### Thread rule (Phase 1)
Triaging an email now cascades to thread members received **at or before** it
(`RecordThreadTagger.TagThreadAsync(anchorReceivedAt:)`); anything newer queues for its own
decision — even if it was already in the mailbox when the decision landed. Jump-to-latest makes
this invisible in the normal flow; it closes the open-then-act race and matches "triage the lot,
but new mail is its own decision". Restores still clear whole threads; the backfill still sweeps
whole threads.

### Composer (Phase 3)
- `jpms/Features/Triage/RichTextEditor.razor` + `wwwroot/js/rich-compose.js`: contenteditable
  rich-lite editor (bold/italic/lists). **Pasted images** land inline as `data:` `<img>`s; the
  server (`ComposeHtmlPipeline`) sanitises the HTML to a small outbound allowlist (Ganss.Xss, no
  style pass-through) and converts each pasted image to a proper **cid inline attachment** (≤4 MB
  each). Pasted Outlook HTML is flattened client-side too.
- `jpms/Features/Triage/AttachmentPicker.razor`: files from **this computer** (multipart, part
  name = `ComposeAttachmentRef.Id`, 25 MB/file and 25 MB combined), **project drawings** (by
  DrawingRevisionId, bytes from the drawings blob store server-side), **progress photos** (by
  ProgressPhotoId, progress-photos blob store), and **this email's own attachments** (forwarded
  straight from Graph). Reports deliberately not offered yet.
- **New email**: toolbar button on the triage page → modal composer for a fresh outbound thread,
  with optional filing to a record at compose time (the draft carries marker + record tag +
  pathway, so the sent copy self-files under the record; unfiled sends carry no categories).
- Multipart transport: `HttpIntakeQueue.SendComposedEmailAsync(command, files)` — same shape as
  the progress-photo upload; JSON-only when there are no files.

### To-dos alongside everything (Phase 4, now the to-dos modal)
To-dos are drafted in the modal (list ⇄ detail) and created FIRST when the action bar applies
(their command verifies every tag before saving), so a later failure leaves the items existing and
the email findable in the Tagged view under its TODO chips — per-step honesty rather than a
pretend transaction. Fan-out per assignee and pathway-neutrality unchanged. The modal carries its
own project pick (`todoProjectId`, independent of the filing section) plus the optional
open-request link.

### Mail-client look (same-day polish)
List rows are Outlook-shaped: sender in semibold with a compact right-aligned time (time today,
"Yesterday 14:21", day names this week, then dates), subject + paperclip, and the email's opening
line as a preview — under Today / Yesterday / day-name group headers in the queue. The detail pane
opens with an Outlook-style header: subject, sender avatar + name + address, To/Cc (once the
detail read lands) and the full date.

## Phase 0 — Azure (manual, do before expecting sends to work)

Code is safe to deploy first: until consent lands, `SendDraftAsync` gets 403 and every send
degrades to "saved as draft".

1. Entra admin centre → App registrations → the app whose ClientId is in `MailboxIntake:ClientId`
   → API permissions → Add a permission → Microsoft Graph → **Application** → `Mail.Send` →
   **Grant admin consent**.
2. Scope it (Exchange Online PowerShell) — if an ApplicationAccessPolicy already scopes this app's
   `Mail.ReadWrite` to projects@, `Mail.Send` inherits it and there is nothing to do. Otherwise:
   `New-ApplicationAccessPolicy -AppId <clientId> -PolicyScopeGroupId <mail-enabled security group containing projects@jewelbb.co.uk> -AccessRight RestrictAccess -Description "JPMS mailbox"`
3. Verify: `Test-ApplicationAccessPolicy -AppId <clientId> -Identity projects@jewelbb.co.uk`
   (expect Granted) and against another mailbox (expect Denied).

## Verification checklist (live mailbox)

- Reply-send to a test address: received + threaded; projects@ Cc'd; sent copy tagged
  `JPMS`/`JPMS/Replied`/pathway; inbound thread left the queue; a LATER reply re-queues with a
  "Thread: JPMS/Replied" hint chip.
- Untag `JPMS/Replied` on the Tagged tab → thread back in the queue, pathway + marker dropped.
- Pre-consent (or revoked consent) send → error banner + draft in Outlook Drafts, nothing tagged.
- Save as draft → draft in Outlook with the edited envelope applied.
- Paste a screenshot into the body → renders inline in Outlook and Gmail (cid attachment, not a
  data: URL).
- Attach 1 KB / 2.9 MB / 10 MB files (inline vs upload-session paths); a drawing revision; a
  progress photo; one of the original email's attachments.
- New email with a record chosen → sent copy under the record's correspondence and the Tagged
  view, never the queue.
- Reply + "Also add to-dos" (two assignees on one row) → fan-out count matches, TODO tags +
  Replied all on the thread; Discard + to-dos works the same.
- Older-thread-member action (via the thread panel) leaves newer members queued.
- Audit trail shows `EmailSent` rows with recipients and a webLink that opens the sent copy.

## Deferred (Phase 2) — page decomposition

`TriageQueue.razor` is ~3,400 lines and should be decomposed into `jpms/Features/Triage/`
components (`MessageList`, `MessageViewer`, `ThreadPanel`, `TriageActionBar`, `LinkRecordPanel`,
`CreateRecordPanel`, `ComposePane`, `TodoDraftsPanel`) with an Outlook-style three-zone layout.
Deferred as a pure refactor with zero behaviour change — do it component-by-component with a green
build after each extraction. `RichTextEditor` and `AttachmentPicker` are already components and
deliberately generic (the Programme reply surface can adopt the editor next). Queue page size is
already raised to 25.

## Retirement note

`ReplyInThreadFromMessage` (endpoint + contract + `IIntakeQueue` method) is superseded by
`SendMailboxEmail` and no longer called by the UI. Kept for old open tabs; delete all three once
this deploy has been out for a while.
