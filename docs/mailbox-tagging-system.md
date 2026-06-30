# Mailbox tagging system — design

Status: **Agreed in principle, ready to build in slices.** Date: 2026-06-30.

## The model

Email **categories are the source of truth** for which workflows an email belongs to. Nothing is
moved or copied; triage only tags. The database holds workflow *records* (requests/RFIs/…), never the
mail — a record references its emails by tag and reads them live when needed.

- **Untagged email → it sits in the triage queue.**
- **One or more `JPMS/…` tags → it's out of triage and feeding that many workflows.** An email can
  carry several tags at once (it can belong to several processes).
- Removing the **last** tag returns it to triage.

### The marker (a constraint, not a choice)

Graph only supports **exact** category filters — `categories/any(c:c eq 'X')`. It rejected a
"starts-with `JPMS/`" filter (`ErrorInvalidUrlQueryFilter`). So "has *any* JPMS tag" can't be a single
query. We express it with one **marker** category, present whenever an email has any JPMS tag:

- Queue = `not categories/any(c:c eq 'JPMS')` (no marker).
- Tagged = `categories/any(c:c eq 'JPMS')` (has marker).
- Adding any tag also ensures the marker. Removing a tag recomputes it: if no `JPMS/…` workflow tags
  remain, the marker is removed too → the email is back in triage.

The marker shows in Outlook as one extra category; optionally we register the JPMS categories in the
mailbox's master list so they render as tidy coloured labels.

## Tags

- Workflow tag = the record's reference: **`JPMS/RFI-001`**, `JPMS/RFQ-014`, etc. Stable + unique.
- Discarded is just a tag: `JPMS/Discarded`.

## The two screens

**Triage queue** (existing) — untagged Inbox mail. Select an email and add its *first* tag by:
assign to an existing request, create a new request (makes the record), or discard. That's the only
way a tag is created — every tag maps to a real record/outcome (no free-text tags).

**Tagged tab** (replaces "Discarded") — every tagged email. This is the management surface:

- **Search / filter** to find an email: full-text (Graph `$search` over subject/sender/body),
  filter by tag (a specific workflow), and by sender/date. (`$search` support on the shared mailbox
  is the one thing I'll verify with a quick probe when we build this.)
- Each email shows its tags as chips. From here you can **add another tag** (assign it to a second
  request, etc.) or **remove a tag** (unlink it from that record). Remove the last tag → back to triage.

## Per-record email reader

Any process — "Prepare RFI notification", LLM context generation — pulls its emails on demand with
`categories/any(c:c eq 'JPMS/RFI-001')`, reads their content live, and uses it. No stored copies.

## Build slices (each verified against the mailbox before the next)

1. **Tag plumbing.** Marker recompute on removal; `AddTag` / `RemoveTag` / `GetTags(message)` /
   `ListByTag(tag)` / `ListTagged(search, page)`; workflow tag = record reference.
2. **Stop snapshotting.** Assign/create only tag the email (+ create the record for "create"); request
   views and processes read their emails live by tag. (Touches the request/RFI screens — its own slice.)
3. **Tagged tab UI.** List + chips + add/remove + search/filter.
4. **Process readers.** Wire "emails for this record" into the RFI/notification/LLM-context paths.

## Parallel quick win

Register the JPMS categories in the mailbox's master category list (coloured) so the tagging is
clearly visible to anyone in Outlook.
