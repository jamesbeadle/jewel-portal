# Plan — Make mailbox triage mirror the Inbox (single source of truth) + server-side paging

Status: **Plan only — not implemented.** Owner: Nigel. Date: 2026-06-28.

## 1. The problem and its root cause

The triage queue only ever grows. When an email leaves the `projects@` Inbox (someone
deletes or moves it directly in Outlook), the triage queue keeps showing it as a ghost row.

It is **not** that we never see the removal — we see it and deliberately discard it:

- `MailboxDeltaSweep` walks the Inbox `/messages/delta` feed every 5 minutes. Graph reports
  deleted/moved-out items as `@removed` entries.
- `GraphMailClient.ParseMessage` correctly turns those into `GraphMessage { IsRemoved = true }`.
- `IntakeIngestionService.IngestAsync` then drops them:

  ```csharp
  if (message.IsRemoved)
      return false; // a delete in the delta feed — nothing to ingest.
  ```

Nothing anywhere reacts to a removal, so the row stays `NeedsTriage` forever. The webhook only
subscribes to `changeType = "created"`, so the sweep is the *only* place a removal could ever be
handled — and it doesn't. That single early-return is the whole bug.

`ListOpenIntakeHandler` returns every row with status `NeedsTriage` or `Claimed`, and
`TriageQueue.razor` paginates that full list in memory (`PageSize = 5`).

## 2. Target behaviour (agreed)

- The Inbox is the source of truth. The triage queue = the set of un-triaged Inbox emails.
- When a `NeedsTriage` email disappears from the Inbox, it drops out of triage automatically.
- Such an email — it never matched a request and was never assigned — should end up in a
  **No-action-required** folder, so it is invisible in both the Inbox and the triage screen.
  (This is the same destination we already use for Discarded / "Not relevant" mail.)
- Pagination is done **server-side**.

## 3. The one trap that makes the naive fix wrong

The app **itself** moves emails out of the Inbox during triage (Claimed → "In progress",
Linked → request folder, Discarded → "Not relevant" — see `MailboxActionWorker` /
`OutcomeFolders`). Those app-initiated moves **also** appear as `@removed` in the Inbox delta
feed. A blunt "delete any row whose email left the Inbox" would wipe legitimately-triaged rows.

Clean discriminator: **`NeedsTriage` emails are the only ones the app never moves**
(`OutcomeFolders` returns `null` for them). So:

- A `NeedsTriage` row's `GraphMessageId` still equals its Inbox id → it **will** match an
  `@removed` id. A match therefore means a human removed it outside the app.
- Claimed/Linked/Discarded rows had their `GraphMessageId` rewritten on the app's own move, so
  they won't match the old Inbox id — and we guard on `Status == NeedsTriage` regardless.

So the reconciliation rule is precise: **on an `@removed` entry, find the intake by
`GraphMessageId`; act only if its status is `NeedsTriage`.**

## 4. Change A — Reconcile removals (the core fix)

### 4.1 New terminal status
Add to `IntakeStatus` (in `contracts/Models/IntakeEmail.cs`):

```csharp
RemovedFromMailbox = 5  // vanished from the Inbox before triage; auto-filed to No-action-required
```

`Status` is stored as `int`, so **no DB migration is needed**. A distinct value (rather than
reusing `Discarded`) keeps a clean audit trail of "human dismissed in-app" vs "disappeared from
the Inbox". It is excluded from the triage queue.

### 4.2 Handle the removal during the sweep
In `IntakeIngestionService`, replace the silent `return false` with a reconcile path. Sketch:

```csharp
if (message.IsRemoved)
{
    if (string.IsNullOrEmpty(message.Id)) return false;

    var row = await _context.IntakeEmails
        .FirstOrDefaultAsync(e => e.GraphMessageId == message.Id, ct);

    // Only un-triaged rows. Claimed/Linked/Discarded left the Inbox by OUR move — leave them.
    if (row is null || row.Status != (int)IntakeStatus.NeedsTriage)
        return false;

    row.Status = (int)IntakeStatus.RemovedFromMailbox;
    await _context.SaveChangesAsync(ct);

    // Best-effort tidy: file the stray into No-action-required (section 4.3).
    await _mailbox.ScheduleFileStrayAsync(row.IntakeId, ct);
    return false; // nothing ingested; this is a removal
}
```

The DB reconciliation is the guarantee; the folder move is best-effort (consistent with the
existing "DB is source of truth, mailbox folder is a mirror" design).

### 4.3 Filing the stray to No-action-required (the part with a caveat)
The email has already left the Inbox, and **the `@removed` id is the stale Inbox id** — the
message now has a *new* id wherever the human put it (Deleted Items, or some folder). So we
cannot move it with the id we hold. Options, in order of preference:

1. **Locate-then-move (recommended).** Add
   `IGraphMailClient.FindMessageIdByInternetMessageIdAsync(string internetMessageId)` that calls
   `GET /users/{mailbox}/messages?$filter=internetMessageId eq '...'&$select=id` (we already
   store `InternetMessageId`). Then move that id into the No-action-required folder and update
   `GraphMessageId`. Add a `MailboxActionType.FileStray` handled in `MailboxActionWorker`,
   reusing the existing `EnsureFolderAsync(NotRelevantFolder, RequestsParent)` resolution.
   - **Verify before building:** confirm `/messages` returns items from Deleted Items. If a
     human emptied Deleted Items / it was hard-purged, the lookup returns nothing — in that case
     we simply skip the move (the row is already reconciled out of the queue, goal still met).
   - **Decision to confirm:** if a human *deliberately moved* the email to their own folder,
     this would yank it into No-action-required. Acceptable per "the app owns these emails," but
     flag-worthy. Gate the move behind `EnableFolderMoves` so it can be turned off.

2. **Reconcile only, no move.** Skip the folder move entirely. The email is already out of the
   Inbox, so it's invisible in both Inbox and triage — which satisfies the literal goal — but it
   won't be consolidated into No-action-required. Simplest; lowest risk.

Recommendation: ship option 2 as the guaranteed behaviour, add option 1 behind `EnableFolderMoves`.

## 5. Change B — Server-side pagination

Currently `ListOpenIntake` returns everything and the page does `Skip/Take` in memory. Move it
to the server:

- **Contract** (`contracts/Requests/ListOpenIntake.cs`): take paging params and return a paged
  result, e.g.
  `record ListOpenIntake(int Skip = 0, int Take = 25) : IQuery<PagedResult<IntakeEmail>>;`
  with `PagedResult<T>(IReadOnlyList<T> Items, int Total)`.
- **Handler** (`ListOpenIntakeHandler`): keep the `NeedsTriage`/`Claimed` filter and
  `OrderBy(ReceivedAt)`; compute `Total` with `CountAsync`, then `.Skip().Take()` in the query
  (server-side EF, not in memory).
- **Endpoint** (`ListOpenIntakeEndpoint`): read `skip`/`take` from query string, clamp `take`
  (e.g. 1–100), pass through.
- **Client** (`IIntakeQueue.ListOpenAsync` + its HTTP impl): add `skip`/`take`, return the paged
  result.
- **UI** (`TriageQueue.razor`): drop `PagedItems`/in-memory `PageCount`; fetch the current page
  from the server in `LoadAsync`, drive `Previous`/`Next` off the returned `Total`. Keep
  `PageSize` (currently 5 — consider raising to ~25 for a server round-trip per page).

Note the count shown in the header ("N emails waiting") should use `Total`, not the page length.

## 6. Edge cases & races

- **App move vs external delete ambiguity:** resolved by the `Status == NeedsTriage` guard (§3).
- **Claimed emails** are out of the Inbox but still in the queue by design — untouched, since
  they're not `NeedsTriage`.
- **Removal detected only by the sweep** (≤5 min lag). The webhook is `created`-only, so this is
  expected; acceptable. (Optional later: add `changeType = "created,deleted"`.)
- **Deletions from non-Inbox folders** (e.g. a human deletes from "In progress") are not seen —
  the delta is Inbox-scoped. That's fine: those are already triaged; the invariant we maintain is
  Inbox ↔ `NeedsTriage` queue.
- **Re-appearing email** (human moves it back to the Inbox): the sweep sees a normal entry; the
  unique index on `InternetMessageId` means the existing row is found. If it's now
  `RemovedFromMailbox`, decide whether to reactivate it to `NeedsTriage`. Recommended: yes —
  add that reactivation in the `existing is not null` branch of `IngestAsync`.

## 7. Tests

- Sweep page containing an `@removed` for a `NeedsTriage` row → row becomes `RemovedFromMailbox`
  and leaves the queue.
- `@removed` for a `Claimed`/`Linked`/`Discarded` row → **no change**.
- `@removed` for an unknown id → no-op.
- Removed-then-reappeared email → single row, reactivated to `NeedsTriage` (if §6 adopted).
- `ListOpenIntakeHandler` paging: total count correct; `Skip/Take` returns the right slice in
  `ReceivedAt` order; excludes `RemovedFromMailbox`.
- (If option 1) `FileStray` locates by `InternetMessageId` and moves; missing message → skip.

## 8. Files to touch

Core fix:
- `contracts/Models/IntakeEmail.cs` (new enum value)
- `worker/MailboxIntake/Ingestion/IntakeIngestionService.cs` (reconcile + optional reactivate)
- (option 1) `worker/MailboxIntake/Graph/IGraphMailClient.cs` + `GraphMailClient.cs` +
  `NullGraphMailClient.cs` (locate-by-InternetMessageId)
- (option 1) `api/Features/MailboxIntake/Actions/IMailboxActionScheduler.cs`,
  `worker/MailboxIntake/Actions/MailboxActionWorker.cs`, `Queue/MailboxQueues.cs` message type
  (new `FileStray` action)

Pagination:
- `contracts/Requests/ListOpenIntake.cs`
- `api/Features/Requests/Queries/ListOpenIntakeHandler.cs`
- `api/Features/Requests/Queries/ListOpenIntakeEndpoint.cs`
- `jpms/Services/IIntakeQueue.cs` + its HTTP implementation
- `jpms/Pages/TriageQueue.razor`

Tests: `tests/Jewel.JPMS.Tests/...`

No schema migration required (status is an `int`).

## 9. Open decisions for Nigel

1. **§4.3 move:** option 1 (locate-and-file to No-action-required) or option 2 (reconcile only)?
2. **§6 reactivation:** if a removed email reappears in the Inbox, put it back into triage? (rec: yes)
3. **Page size** for the server-side query (current UI uses 5; suggest ~25).
