# Communication buckets — Client / Subcontractor / Internal

Status: **Proposed, decisions confirmed except the mapping defaults marked ⚠.** Date: 2026-07-22.

## What's changing and why

Every triaged email currently carries record tags (`JPMS/JBB-2026-001-RFI-012`, `JPMS/CC-…`,
`JPMS/BPI-…`) but no notion of **who the correspondence is with**. We are splitting all
communications into three buckets — **Client**, **Subcontractor**, **Internal** — so that:

- triage, the Tagged view and the project Communications tab can be filtered/segmented by bucket;
- the sidebar and registers can speak the language of the split ("Requests" → **"Client Requests"**,
  bid packages live on the subcontractor side);
- the already-triaged backlog is migrated into the split by an automatic backfill.

## Decisions (agreed)

1. **Bucket = a category tag, auto-derived from the record type.** Three new categories —
   `JPMS/Client`, `JPMS/Subcontractor`, `JPMS/Internal` — stamped automatically when an email is
   linked to (or a record is created from) a record whose type implies the bucket. No extra triage
   click. One cheap exact-match Graph filter per bucket (same trick as the `JPMS` marker), and the
   bucket is visible in Outlook as a coloured label.
2. **One bucket per thread is the invariant.** The normal flow can't violate it (first tag sets the
   bucket; thread sweep propagates it to every reply). The two paths that *could* — "add another
   tag" on the Tagged view, and a conversation that spans counterparties — are guarded: the UI
   warns and blocks any action that would introduce a second bucket, and a conflict report lists
   any thread that nevertheless carries two so it can be resolved by hand.
3. **Backlog: automatic backfill sweep.** A one-off admin endpoint walks every marker-tagged email,
   derives the bucket from its record tags via the mapping below, and stamps it thread-wide.
   Unresolvable or conflicting threads are reported, not guessed.

## Record type → bucket mapping

| Record type (tag shape) | Bucket | Notes |
|---|---|---|
| Request — RFI/RFA/RFC/RFQ/RFP/NOD/EOT/REQ (`JPMS/<proj>-RFI-012`) | **Client** | Agreed. The register becomes "Client Requests". |
| VariationQuote — VOQ (`JPMS/<proj>-VOQ-…`) | **Client** | Agreed. |
| Variation — VO (`JPMS/<proj>-V-…`) | **Client** | Agreed. |
| BidPackageInvite (`JPMS/BPI-…`) | **Subcontractor** | Agreed. |
| CostCentre (`JPMS/CC-<proj>-<code>`) | **Subcontractor** ⚠ | Default: subcontracts are line-by-line cost centres, so cost-centre mail is treated as subcontract-side. Flip to Client if cost-centre mail is mostly valuation-side. |
| Scheduling / Programme (`JPMS/SCH-…`) | **Client** ⚠ | Default: programme correspondence (delays, EoT context, progress) is client/architect-facing; NOD/EOT notices are already Client via the Request family. |
| Lad (`JPMS/LAD-…`) | **Client** ⚠ | LAD claims sit between Jewel and the client. |
| Todo (`JPMS/TODO-…`) | **Neutral** ⚠ | A to-do link never sets or changes a bucket — a to-do raised from a client email stays Client. A thread whose *only* link is a to-do defaults to **Internal**. |
| Discarded (`JPMS/Discarded`) | none | Discarded mail is bucket-less; restoring returns it to the queue as today. |

⚠ = default I've assumed; confirm or flip during review of slice 1 (it's one line in the mapping).

## Mechanics

### Tag plumbing (`TriageCategories`)

- New constants `Client = "JPMS/Client"`, `Subcontractor = "JPMS/Subcontractor"`,
  `Internal = "JPMS/Internal"`; `IsBucketTag(category)`; `BucketFor(RecordType) → string?`
  (null for Todo = neutral).
- **Bucket tags are not workflow tags for queue-membership purposes.** Today "any `JPMS/…` tag ⇒
  out of triage" and "remove the last workflow tag ⇒ marker removed ⇒ back to triage". Bucket tags
  share the `JPMS/` prefix, so every place that counts workflow tags must exclude them
  (`IsWorkflowTag` callers, the marker recompute in RemoveTag, `MailboxMessage.Categories`
  chip surfaces, the Tagged tab's `knownTags`, `ListProjectCommunications`' unresolved-tag chips).
  Rule: **removing the last record tag also removes the bucket tag and the marker** — an email can
  never sit outside the queue carrying only a bucket.
- Worker parity: `TriageCategories` is compiled into the worker via the linked include, so outbound
  drafts (bid-package invites, request replies) stamp the same bucket as their record tag —
  covered by updating the one shared file.

### Stamping at link/create time

- `LinkMessageToRecordHandler` (the single link path — Assign is an adapter over it): after the
  record tag is applied, resolve `BucketFor(record.Type)`; if non-null and the thread has no
  bucket yet, apply it with the same `RecordThreadTagger.TagThreadAsync` thread-wide mechanics.
  If the thread already carries the *same* bucket, no-op. If it carries a **different** bucket,
  reject with a clear message (see conflict handling).
- Create paths stamp identically: `CreateRequestFromMessage`, reply-in-thread (creates a General
  request → Client), scheduling link, to-do creation (neutral — Internal only when the thread has
  no bucket and no other record tag).
- **Sweep inheritance is free:** the queue sweep (`SweepQueuePageAsync`) inherits every non-Discarded
  `JPMS/…` tag onto new replies — bucket tags ride along automatically. Keep buckets excluded from
  the sweep's "does this thread have a triage decision" test (a bucket alone ≠ triaged), but
  included in what gets copied to siblings.

### Conflict handling (the invariant's guard rails)

- **Link/create guard (server-side):** introducing a second bucket on a thread is a validation
  failure with an actionable message ("This thread is filed under Client; BPI-0004 would file it
  under Subcontractor. Remove the Client link first, or start a new thread."). The Tagged tab's
  "add another tag" picker shows each record's bucket so the conflict is visible before clicking.
- **Conflict report:** a small admin view (or a section on the Tagged tab) listing conversations
  carrying two bucket tags — populated only by the backfill or by mail flows we haven't met yet.
  Resolution = remove the wrong link from the thread; the bucket recomputes.

### Backfill (the migration)

`POST mailbox/backfill-buckets` (triage-gated, mirrors the existing `retag-requests` migration
endpoint pattern):

1. Page through every marker-tagged conversation (`ListTaggedAsync` → group by `ConversationId`).
2. Collect the thread's record tags; map each to a bucket by **tag prefix** (CC-→Sub, BPI-→Sub,
   SCH-→Client, LAD-→Client, TODO-→neutral, `<proj>-RFI/RFA/RFC/RFQ/RFP/NOD/EOT/REQ/VOQ/V-`→Client),
   resolving via the provider registry's `ReferencePrefixes` so the mapping can't drift from the
   providers.
3. Exactly one bucket derived → stamp it thread-wide (idempotent; re-runnable).
   Zero (to-do-only threads) → Internal. Discarded-only → skip.
   More than one → **don't stamp**; emit to the report for manual resolution.
4. `dryRun=true` first: returns the counts and the full per-conversation outcome without writing,
   so we can eyeball the conflict list before any category is touched. (Same probe-first discipline
   as `scripts/probe-*.sh`.)

Also: register the three bucket categories in the mailbox master category list with colours
(parallel quick win, same as the original tagging plan) so the split is visible in Outlook.

### API surface

- `GET mailbox/tagged` already accepts a `tags` filter — bucket filtering needs nothing new
  (`tags=JPMS/Client`). Add the bucket to each message row server-side (a `Bucket` field on
  `MailboxMessage` derived from its categories) so clients never parse tag strings.
- `ListProjectCommunications` gains an optional `Bucket` filter alongside `Type` — implemented by
  intersecting the bucket category into the existing Graph read (one extra AND clause; cheap).

## UI changes

**Triage space (this phase):**

- Tagged view: three bucket filter chips (Client / Subcontractor / Internal) above the existing
  tag filter; a bucket badge on every row; "add another tag" shows each candidate record's bucket
  and blocks cross-bucket picks with the explanatory message.
- Link/Create panel: a quiet line stating the consequence — "This thread will be filed under
  **Client**" — driven by the selected record type. No new inputs.
- Queue is unchanged (untagged mail has no bucket yet, by definition).

**Follow-on phase (the renames and bucketed views):**

- `ProjectSections`: "Requests" label → **"Client Requests"** (slug `requests` unchanged so links
  and bookmarks survive — same convention as previous renames). `ProjectRequests.razor` header
  "Document register" → "Client requests register" (exact copy TBD); its scope is already exactly
  the client set (Requests, RFIs, VOQs, VOs), so no data changes.
- Communications tab: segmented control Client / Subcontractor / Internal driving the new bucket
  filter, with the existing type filter nested beneath.
- Subcontractor side gathers its existing homes (Bid Package Invites, Work Orders) under the
  bucket language; no route changes in this phase.
- Per CLAUDE.md conventions: "Programme" never "Schedule" in copy; persisted identifiers
  (`RecordType.Scheduling`, `JPMS/SCH-`) stay as they are.

## Build slices (each verified against the live mailbox before the next)

1. **Tag plumbing + mapping.** `TriageCategories` bucket constants, `BucketFor`, `IsBucketTag`,
   and the queue-membership exclusions (marker recompute, sweep decision test, chip surfaces).
   Confirm the ⚠ defaults here — they're data, one line each.
2. **Stamp on link/create + conflict guard.** All link/create paths stamp thread-wide; second-bucket
   attempts rejected; verified with probe scripts against real threads (including a to-do-on-client
   thread and an add-another-tag cross-bucket attempt).
3. **Backfill.** Dry-run against the live mailbox → review counts + conflict list → run. Register
   the coloured categories in the mailbox master list.
4. **Triage UI.** Bucket chips/filter/badges on Tagged, consequence line on Link/Create, conflict
   surfacing.
5. **Renames + bucketed views.** "Client Requests" in nav and header; Communications segmented by
   bucket; subcontractor-side grouping.

## Edge cases noted for implementation

- **Thread spanning two projects** — already possible today (the Communications tab shows foreign
  tags as raw chips); buckets are project-agnostic so this doesn't interact.
- **Bucket-only email** — prevented by the removal rule (bucket goes when the last record tag goes).
- **Outbound drafts** — worker stamps bucket + record tag together, so sent invites/replies enter
  Sent Items already bucketed and the sweep propagates correctly.
- **Graph's exact-match-only category filters** — the whole design leans on it: every bucket view
  is one exact clause, never a prefix scan.
- **Re-running the backfill** — idempotent by construction (stamp = add-if-missing), safe after
  any interruption.
