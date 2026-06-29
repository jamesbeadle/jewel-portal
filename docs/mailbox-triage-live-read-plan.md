# Plan — Live-read triage (mailbox as the single source of truth)

Status: **Proposed.** Replaces the mirror-based intake (`IntakeEmails` + delta sweep + reconcile + async moves). Owner: Nigel. Date: 2026-06-29.

## 1. Principle

The mailbox is the single source of truth. The platform never keeps a parallel copy of un-triaged mail. Each triage view is a **live read of one mailbox folder**, and each triage action is a **single message move**. The database persists only **requests** (and the email content snapshotted onto them), because the mailbox can't model "this is RFI-014, awaiting response, due Friday."

This deletes the entire sync layer — the source of every ghost-row, stale-id, and duplicate-copy problem in the current build — because there are no longer two stores to reconcile and no stored message ids to go stale.

## 2. Folder ↔ view mapping

- **Inbox** = the triage queue. Un-triaged mail *is* the Inbox.
- **General** (under Inbox) = discarded. The "Discarded" tab paginates this folder live.
- **REQ-0001 … REQ-nnnn** (under the "Requests" parent) = mail assigned to that request.

An email leaves a view the instant it is moved, so no view needs a status flag, a reconcile, or a background sweep to stay correct.

## 3. Flows

- **Triage list** — live `GET /mailFolders/inbox/messages`, paged server-side. No DB.
- **Open an email** — live read of body + attachments by message id (the existing on-demand message reader).
- **Discard** — move Inbox → General.
- **Discarded list** — live read of General, paged.
- **Undiscard** — move General → Inbox.
- **Assign to existing request** — snapshot the email onto the request's thread in the DB, then move Inbox → the request's folder.
- **Create request from email** — create the Request, snapshot the email onto its thread, then move Inbox → a new request folder.

Every action is synchronous: the user clicks, the API moves the message via Graph and returns. No queue, no best-effort retry, no background re-drive — which is what was manufacturing the duplicate copies.

## 4. Identity & resilience

The message id is read live when the list renders, so it is fresh at click time — there is no long-lived stored id to go stale. If an id has changed between render and click (rare), the move re-finds the message by its stable `InternetMessageId` and moves that. Use immutable ids on these calls so an id stays valid across the move. This `InternetMessageId` re-find is the one piece of the recent work worth carrying over.

## 5. Delete

- `IntakeEmails` table, `IntakeEmailEntity`, the `IntakeStatus` enum, and the `IntakeEmail` mirror contract.
- `MailboxDeltaSweep`, `MailboxInboxReconcile`, `IntakeIngestionService`, `InboxReconciliation`.
- `MailboxSyncStateEntity` + `MailboxSyncStateStore` + the delta-cursor migration.
- Webhook path: `MailboxSubscriptionTimer`, `IntakeNotificationWorker`, subscription create/renew.
- The async move layer: the move/return cases of `MailboxActionWorker`, `IMailboxActionScheduler`/`MailboxActionScheduler`, and the move plumbing on the `mailbox-actions` queue.
- Intake queries: `ListOpenIntake`, `ListDiscardedIntake`, `GetIntakeEmailDetail` (becomes a live read).
- Intake commands as written: `ClaimIntakeEmail` (claiming is removed); `DiscardIntakeEmail`, `RestoreIntakeEmail`, `LinkIntakeToRequest`, `CreateRequestFromIntake` are rewritten to act on a live message id with no status column.

## 6. Keep / change

- **Requests** feature is unchanged — entity, documents, status, threads. Assigning/creating still snapshots the email onto `RequestMessages` and sets `Request.MailboxFolderId`.
- **Graph access moves into the API.** Triage actions are now synchronous in the API, so the API needs list-folder-messages, move-message, and ensure-folder — not just the read-detail it has today. Promote those from the worker's `GraphMailClient`.
- **Worker** keeps only outbound sending (`SendRequestDocument` and Shared replies). Everything intake/triage-related leaves it.
- **AI suggestion / thread matching** read the live message on demand instead of stored intake rows.
- **Triage UI** (`TriageQueue.razor`) keeps its shape — Queue + Discarded tabs, detail pane, assign / discard / restore — but its data comes from live folder reads.

## 7. Open considerations

- **Request replies.** A reply to an RFI lands in the Inbox and shows up in triage; the triager assigns it to the request — or we add live thread-matching to auto-suggest the request. Decide: auto-link replies, or just let them reappear in triage. (No worse than today, only without background ingestion.)
- **Performance / throttling.** One Graph call per screen load instead of a DB query. Triage volumes are low, so this is fine; page server-side and honour Graph's `Retry-After` (already implemented).
- **Availability.** Triage now depends on Graph being reachable — acceptable for an internal back-office screen.

## 8. Sequencing (no big-bang)

1. Add API-side Graph read/move/ensure-folder and live folder-list endpoints for Inbox and General.
2. Repoint the Triage UI's Queue and Discarded tabs at the live endpoints; verify reads and pagination.
3. Rewrite discard / undiscard as direct moves; rewrite assign / create as snapshot + move.
4. Once the live path is proven in production, delete the mirror — tables, sweep, reconcile, webhook, move queue — and drop the `IntakeEmails` / `MailboxSyncState` migrations.
