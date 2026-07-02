# RFI Numbering Refactor — Project-Scoped References

**Date:** 2026-07-02 · **Status:** Approved, implemented alongside this doc

## Problem

RFI references (e.g. `RFI-012`) are project-scoped by design — every project runs its own
`RFI-001, RFI-002…` sequence, and the client only ever sees the project-local number. Today that
contract is held together entirely in application code:

1. **No database backstop.** The `Requests` table has *no* unique index on `(ProjectId, Reference)`
   — in fact no index at all beyond the PK. Uniqueness is enforced only by
   `RequestReferenceGuard` (a check-then-insert `AnyAsync`), which races under concurrent writes:
   two users saving `RFI-013` at the same moment both pass the guard and both insert.
2. **Race-prone minting.** Both numbering paths compute `MAX + 1` and save without any conflict
   handling: `RaiseRequestHandler` (global `MAX(Number) + 1` for REQ-####) and
   `PromoteRequestToRfiHandler` (per-project highest `RFI-nnn` + 1). Concurrent creates can mint
   the same number.
3. **No auto-selection on create.** `RequestReference.SuggestNext()` exists in `contracts` but is
   dead code — nothing calls it. The only way an RFI gets a reference automatically is via
   *promotion* of a General request. A directly-created (back-filled) RFI requires the user to
   type the reference by hand with no suggestion of the project's next number.
4. **Cross-project mailbox tag collisions.** The Outlook workflow tag is derived flat from the
   reference (`RFI-012` → category `JPMS/RFI-012`) in a single shared projects@ mailbox. Once two
   projects each have an `RFI-012`, their correspondence cross-links. The cost-centre link
   provider already solved this exact problem with project-qualified tags
   (`CC-{projectRef}-{code}`); requests never got the same treatment.

## Current architecture (for reference)

| Concern | Where | Behaviour |
|---|---|---|
| Entity | `api/Data/Entities/ProcurementEntities.cs` → `RequestEntity` | `Reference` nvarchar(64); `Number` int (global REQ-#### sequence, mailbox folder name); computed `TagReference` |
| Create | `api/Features/Requests/Commands/RaiseRequestHandler.cs` | Reference honoured as typed; blank → `REQ-{Number:0000}` from global `MAX(Number)+1` |
| Promote | `PromoteRequestToRfiHandler.cs` | Mints `RFI-{n:000}` from per-project highest existing RFI number |
| Manual edit | `UpdateRequestDetailsHandler.cs` | Free edit via `RequestReferenceGuard`, retags mailbox emails on change |
| Uniqueness | `RequestReferenceGuard.cs` | App-level, per-project, case-insensitive — no DB constraint |
| Next-number logic | `contracts/Models/RequestReference.cs` | `SuggestNext` / `HighestNumber` — highest-used+1 (gap-safe), currently unused |
| Mailbox tag | `TriageCategories.ForRecord` + `RequestEntity.TagReference` | `JPMS/{Reference}` — flat, collides across projects |
| UI create | `jpms/Components/RaiseRequestDialog.razor` | General-only, no reference field; RFIs arise via promote or API back-fill |
| UI edit | `jpms/Pages/ProjectRequestDetail.razor` | Reference is an editable form field (legacy renumbering path) |

Decisions taken (2026-07-02, Nigel): per-project unique index on `Reference`; `Number` stays a
**global** sequence (mailbox folder names `REQ-0001` live in one shared mailbox and must not
collide); request mailbox tags become **project-qualified** with a retag pass over existing mail.

## Changes

### 1. Database — unique index (the source of truth)

New migration `20260702120000_AddRequestReferenceUniqueIndex`:

- Defensive pre-step: suffix any existing per-project duplicate references (`-DUP1`, `-DUP2`…,
  keeping the earliest-raised row untouched) so the index can always be created.
- `CREATE UNIQUE INDEX UX_Requests_Project_Reference ON Requests (ProjectId, Reference) WHERE Reference <> N''`
  — filtered so blank references (guard leaves those to field validation) can't trip it. SQL
  Server's default case-insensitive collation matches the guard's semantics, so `rfi-012` vs
  `RFI-012` also clashes at the DB.
- Doubles as the register's missing `(ProjectId, …)` covering index for lookups.

`RequestReferenceGuard` stays as the friendly-error fast path; the index is the correctness
guarantee underneath it.

### 2. Auto-selection of the next project RFI number on create

- `RaiseRequestHandler`: when `Reference` is blank, mint by kind — `General` keeps
  `REQ-{Number:0000}` (global, unchanged); any other kind (RFI, RFA…) gets
  `RequestReference.SuggestNext(kind, <project's references>)`, i.e. the project's next free
  number (`RFI-048` → `RFI-049`, gap-safe, suffix-tolerant).
- `RaiseRequestValidation`: drop "Reference is required" for non-General kinds — the server now
  mints it.
- **Conflict handling** (shared `RequestReferenceConflict` helper): catch the unique-index
  violation on save. Auto-minted reference → re-mint and retry (up to 3 attempts). Manually typed
  reference → surface the same human message the guard produces. Applied to Raise, Promote and
  Update handlers, closing the check-then-act race.

### 3. Manual override kept (legacy back-fill)

- `UpdateRequestDetailsHandler` already lets the reference be edited and retags mail on change —
  unchanged, now DB-backed.
- `RaiseRequestDialog` gains a *"Raise as official RFI"* option: shows a Reference field
  **pre-filled with the project's next RFI number** (computed client-side from the register via
  the same shared `RequestReference.SuggestNext`), freely editable for legacy numbers. If the user
  leaves the suggestion untouched it is sent blank so the server mints authoritatively (no
  stale-register clashes). General requests keep today's no-reference flow.

### 4. Project-qualified mailbox tags

- Tag stem becomes `{projectRef}-{reference}` → category `JPMS/JBB-2026-001-RFI-012`, mirroring
  the cost-centre provider's pattern (falls back to `ProjectId` when the project has no human
  reference, so the stem is always project-unique).
- New `RequestTags` helper owns the stem; all direct `entity.TagReference` tag call sites switch
  to it (`RequestLinkProvider`, `RequestEmailReader`, `ListRequestMessagesHandler`,
  `ReturnRequestToTriageHandler`, mailbox command handlers, `UpdateRequestDetailsHandler`).
  `RequestEntity.TagReference` remains the *unqualified* stem used for display fallbacks.
- **Retag pass:** admin endpoint `POST mailbox/retag-requests` walks every request and moves mail
  from the old flat tag (`JPMS/RFI-012`) to the qualified one via the existing verified
  `graph.RetagAsync`. Idempotent (second run finds nothing under the old tags); best-effort per
  request with a logged summary, same failure posture as the reference-edit retag.

### Explicitly out of scope

- `Number` / REQ-#### stays global (decision above).
- Other reference families (BPI, VO, CC) — already project-safe or separately owned.
- Seed scripts remain valid: they insert explicit references with NOT-EXISTS guards.

## Test & verification

- Extend `RequestReferenceTests`: next-number suggestion across kinds, gaps, suffixed legacy refs
  (`RFI-049A`), empty registers; new tests for the qualified tag stem and duplicate-conflict
  classification.
- Build API + UI; run the suite.
- Migration is safe to run against the live By France register (references already unique; the
  dedupe pre-step is a no-op there).

## Rollout order

1. Deploy migration + API (auto-mint, conflict retry, qualified tags for *new* activity).
2. Run `POST mailbox/retag-requests` once to migrate historic mail onto qualified tags.
3. UI ships with the same release (dialog changes are additive).
