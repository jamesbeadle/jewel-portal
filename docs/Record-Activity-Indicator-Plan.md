# Record Activity Indicator — design plan

*Proposal 2026-08-04; approved and implemented the same day (steps 1–5). Phase 2 surfaces remain
open. One deviation from the plan as written: the index migration was dropped — the activity query
filters on ProjectId + OccurredAt, and `AddAuditEvents` already shipped `IX_AuditEvents_ProjectId`
and `IX_AuditEvents_OccurredAt`, so no schema change was needed at all (see §Index migration).*

## The problem

When someone triages the mailbox, emails get linked to requests, RFIs, variations and the other
record types — but nothing on those records' own surfaces says so. A PM opening the Requests
register sees the same rows in the same order whether a request had four emails filed to it this
morning or has been silent for a month. Finding "the ones with something going on" means opening
records one by one.

The feature: an **activity signal** on every record that receives communications through triage — a
score built from how much mail has been linked recently, decaying over days, rendered as a small
colour-coded badge. Hot records stand out at a glance ("probably one to click into"); quiet ones
show nothing at all.

## Where the signal comes from

The architecture rules out the obvious source. Linked emails are **not stored in the database** —
the Outlook category `JPMS/<reference>` *is* the association, and a record reads its mail back live
from Graph by that tag (`RecordEmailReader`). Computing "emails linked in the last 7 days" from the
mailbox would mean one Graph read per row of the register: a non-starter for list views.

But the moment of linking already leaves a durable, timestamped trace: `LinkMessageToRecordHandler`
(and the create-from-email path) writes an append-only `AuditEventEntity` row carrying
`OccurredAt`, `RecordType`, `RecordId`, `ProjectId` and the event type. That is precisely the shape
an activity score needs — **the audit trail is the index over the mailbox, and activity is a
read of that index**, not a new event stream.

Events that count as activity (all already declared in `AuditEventType`):

- `EmailTriaged` (0) — a thread filed under a pathway via its first link/create
- `RecordLinked` (1) — an email linked to an existing record
- `RecordCreatedFromEmail` (2) — a record created from an email at triage
- `ThreadSwept` (11) — reserved; counts automatically once it is ever written

Deliberately excluded: `DraftCreated` (outbound — the PM did that themselves, it isn't news),
`TagRemoved`/`Discarded`/`WallRejected` (not communications landing on the record), and
`SnapshotTaken`/`CostCentreRecoded` (finance lifecycle, not correspondence).

### One gap to close: the audit trail's client-only scope

Scope decision 2026-07-22: audit rows are written for **client-facing** events only. In
`LinkMessageToRecordHandler` the write is conditional on the resulting bucket being Client. That
covers Requests, Variations, VOQs, Programme correspondence and LAD claims — every record type the
feature was asked for — but links to bid packages and work orders (Subcontractor pathway) and to
cost centres / todos (neutral) leave no row.

The audit model anticipated exactly this: *"Subcontractor/internal event values are reserved …
so widening the scope later is a filter change, not a schema change."* The plan is therefore to
**write the `RecordLinked` / `EmailTriaged` rows for every successful link, whatever the
pathway** (Pathway column carries "Subcontractor" / "Internal" / "" as appropriate). No schema
change; the audit register's existing Pathway filter keeps the client-facing views client-facing.

**Alternative considered and rejected:** a dedicated `RecordActivityEvents` table. It keeps the
audit scope untouched, but duplicates rows the trail already holds for the client pathway, needs a
real migration, and creates a second, drifting record of "what was linked when". If widening the
audit scope is unacceptable for governance reasons, the dedicated table is the fallback — say so at
review and the query layer below is unchanged, only its `FROM` clause moves.

A bonus of the audit-derived approach: client-pathway history exists back to 2026-07-22, so
requests/RFIs/variations light up correctly from day one with no backfill.

## The score

Recency + volume with exponential decay — one number that is high when a lot arrived recently and
melts away on its own:

```
score(record, now) = Σ over events  2^( −ageDays / HalfLifeDays )
```

with `HalfLifeDays = 3` and a hard window of 14 days (an event 14 days old contributes ~0.04 —
nothing; the window bounds the query). So: an email linked just now contributes 1.0; three days ago
0.5; a week ago ~0.2.

Bands, from score:

| Band   | Threshold      | Meaning                                        | Rendering |
|--------|----------------|------------------------------------------------|-----------|
| Busy   | score ≥ 3.0    | several emails in the last day or two          | orange badge + count |
| Active | score ≥ 1.0    | roughly an email today, or a few this week     | amber badge + count |
| Recent | score ≥ 0.25   | something in the last week, now fading         | muted badge + count |
| None   | score < 0.25   | quiet                                          | nothing rendered |

Worked examples: 1 email today → Active. 3 today → Busy. 1 three days ago → Recent. 1 six days
ago → 0.25, the Recent floor. A single email older than ~6 days shows nothing — a one-off from
last week is not "activity".

Colour notes: **red is not used** — in this app red means overdue, and an active record is not a
problem, it's a signal. The ramp is neutral → amber → orange (orange also being the brand accent,
which reads as "warm/alive" rather than "wrong"). Exact classes follow the register's existing
chip idiom (`bg-amber-500/10 text-amber-600` etc.); hover text is mandatory, e.g.
*"4 emails linked in the last 7 days — last one 3 h ago"*.

### One shared reading of the maths

Following the `RequestDates` / `ProjectOrdering` pattern ("kept out of the record itself so a
second, drifting copy can't exist"), the formula, half-life, window and band thresholds live once
in **`contracts/Models/RecordActivity.cs`**:

```csharp
public enum ActivityBand { None = 0, Recent = 1, Active = 2, Busy = 3 }

public static class ActivityScore
{
    public const double HalfLifeDays = 3;
    public const int WindowDays = 14;

    public static double For(IEnumerable<DateTimeOffset> eventTimes, DateTimeOffset now);
    public static ActivityBand BandFor(double score);
}
```

The api computes the score at query time (it owns "now" for the aggregate); the UI derives the
band from the score through the same class. Neither side re-implements the other's half.

## API surface

New query in the record-links feature (it is record-agnostic, like the link layer itself):

```csharp
// contracts/RecordLinks/ListRecordActivity.cs
public sealed record ListRecordActivity(string ProjectId)
    : IQuery<IReadOnlyList<RecordActivitySummary>>;

// contracts/Models/RecordActivity.cs (alongside the maths)
public sealed record RecordActivitySummary(
    RecordType Type,
    string RecordId,
    string Reference,       // denormalised from the audit rows — renders without joins
    int CountLast7Days,     // for the hover text
    DateTimeOffset LastAt,  // when the most recent email was linked
    double Score);          // band derived client-side via ActivityScore.BandFor
```

`ListRecordActivityHandler` reads `AuditEvents` filtered to the project, the four activity event
types and `OccurredAt >= now − 14d`, groups by `(RecordType, RecordId)`, and computes count / last
/ score in one pass. One request per project page view, however many rows the register shows.

Variation identity: after the 2026-07-23 `UnifyVariationOrders` migration a variation is **one
document with one number**, but audit rows may carry `RecordType.VariationQuote` (the persisted
identifier) from links made pre-approval and `RecordType.Variation` from links made after. The
handler coalesces both onto the single variation record's identity, so V72's badge counts its
whole correspondence, not the post-approval slice.

### Index migration — dropped at implementation

The plan proposed a composite index, but `AddAuditEvents` already ships
`IX_AuditEvents_ProjectId` and `IX_AuditEvents_OccurredAt`, and the activity query's shape — seek
one project's rows, keep 14 days of four event types — is fully served by the ProjectId seek at
any realistic audit volume. **No migration ships with this feature; there is no schema change and
nothing to apply against prod.** If a project's audit trail ever grows enough to feel it, the
composite index (`ProjectId, OccurredAt` INCLUDE `EventType, RecordType, RecordId,
RecordReference`) is a small additive follow-up.

## Front-end

### Store

`RecordActivityReadModel` + store method on the pattern every project tab already follows:
fetch-at-most-once per project key for render-time reads, `Refresh(projectId)` called once from
`OnInitializedAsync` (never from render) for stale-while-revalidate, `LoadedFor(projectId)`
exposed, backing field nullable (null = not fetched, empty dictionary = a real "no activity"
answer). Lookup shape: `ForProject(projectId)` → summaries keyed by `(RecordType, RecordId)`.

### `ActivityBadge` component

One small component, used everywhere the signal renders:

- Input: a `RecordActivitySummary?` (null → renders nothing).
- Band `None` → renders nothing. No zero-count chips, no grey placeholders.
- Otherwise a compact pill in the register-chip idiom — count of the last 7 days plus the band
  colour — with the mandatory hover text ("4 emails linked in the last 7 days — last one 3 h
  ago").
- **Never gated.** Per the loading conventions, a single inline mark must not pulse: until the
  activity store has loaded, the badge simply isn't there, and it appears when the data lands.
  A badge can never mislead the way a `0` can — absence during load and absence after load mean
  the same thing ("nothing to show"), which is exactly the conditional-panel rule.

### Placement — on the record, wherever the record renders

- **Requests & RFIs table** (`RequestTable`): trailing badge in the Subject cell, next to where
  the Critical path / Merged chips already sit. No new column — the table is wide already, and the
  chip idiom is established there.
- **Variations table** (the register's Variations view in `ProjectRequests.razor`): same badge on
  the title cell of each variation row.
- **Record detail pages** (`ProjectRequestDetail`, `ProjectVariationDetail`): the `RecordTabBar`
  grows an optional activity lookup, so each tab in the chain — Request, official stage, Variation,
  bid packages — carries a small dot in the tab when its own record is Active/Busy. Moving along
  the chain shows where the correspondence is landing.
- The register page passes the lookup down; pages that don't opt in render exactly as today.

Phase 2 surfaces (same query, no new writes): the cross-project **RFI dashboard** (needs an
across-projects variant scoped like `ListRfisAcrossProjects`), and a "most active records" roll-up
on the project overview.

### What deliberately does not change

- **Ordering.** The register keeps its current sort; activity is a highlight, not a re-ordering.
  (A "sort by activity" toggle is a cheap follow-up if the badge proves itself — noted, not built.)
- **Dates.** `LastAt` is a system fact shown only in hover text — it is never presented as the
  record's Issued date, and nothing about the Issued/Created split moves.
- **Triage itself.** No triage flow changes; the only server-side behaviour change is the audit
  write widening.

## Build order

1. **Widen the audit writes** (`LinkMessageToRecordHandler`: write `RecordLinked`/`EmailTriaged`
   for non-client and neutral pathways too). Server-only, no schema change, starts accumulating
   the missing history immediately — worth shipping first for that reason alone.
2. **Index migration** + apply commands in the same reply.
3. **Contracts + query + handler** (`ActivityScore`, `RecordActivitySummary`,
   `ListRecordActivity`, route registration).
4. **Store + `ActivityBadge` + register placement** (RequestTable, variations view).
5. **`RecordTabBar` dots + detail pages.**
6. Phase 2: RFI dashboard, project overview roll-up, optional activity sort.

Steps 1–5 are one deployable increment; nothing user-visible until step 4, and step 1's widening
is invisible outside the audit register (where new rows appear under their own pathway labels).

## Open questions for review

1. **Audit widening vs dedicated table** — the plan widens the audit scope along its documented
   path. If the client-facing framing of the trail should stay exactly as scoped on 2026-07-22,
   the fallback is the dedicated `RecordActivityEvents` table (same query shape, plus a migration,
   minus the free history).
2. **Tuning** — half-life 3 days / window 14 days / bands at 0.25 / 1 / 3 are proposed starting
   values; they live in one place (`ActivityScore`) and are trivial to adjust after a week of real
   use.
3. **Per-user "new since I last looked"** — out of scope here by choice (recency + volume decay
   was the selected model). The audit trail + a small per-user last-viewed stamp would support it
   later without reworking any of this.
