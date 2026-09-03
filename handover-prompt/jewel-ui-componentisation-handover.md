# Jewel portal — UI componentisation handover (paste this whole file as the first message of the fresh chat)

You are picking up work on my C#/Blazor WASM construction portal, **jewel-portal**
(`api/` Azure Functions, `jpms/` Blazor WebAssembly client, `contracts/`, `worker/`, `tests/`).
A previous session (3 September 2026, on a different licence that ran out of budget) mapped the
whole front end and removed three dead routes. This file carries everything that session
established so you can continue without re-deriving any of it. Read it all before touching
anything, then follow **§1**, then wait for me to tell you which job to start.

This file is **not part of the repository** — never commit it. It sits in `handover-prompt/`
alongside the existing `jewel-refactor-handover-prompt.md` (that one governs the numbered
refactor rounds; this one is the UI/design-system thread, which is separate work).

---

## 1. Setup

**My machine.** The project folder is connected to your session through the device bridge at
`/Users/james/Documents/Claude/Projects/jewel-portal`. Work on the files there with `device_bash`
— read, grep, edit in place. Two things to know:

- **There is no `dotnet` on my Mac.** You cannot build or test there. To compile-check, clone
  `https://github.com/jamesbeadle/jewel-portal.git` into your cloud workspace and use the
  stand-in verification project described in `handover-prompt/jewel-refactor-handover-prompt.md`
  §1 (`jpms/Jewel.JPMS.Verify.csproj` — the real client project does not build in the cloud
  because of the Tailwind/npm static assets). That file also has the nuget list.
- **Deleting files needs my approval.** `rm` inside the connected folder fails with "Operation not
  permitted" until you request delete permission; hidden dotfiles seem to stay refused even then,
  so move those to `_to_delete/` instead.

**Repo state as at this handover.** Branch `main`, tip `7e0b768` ("Merge pull request #23 from
jamesbeadle/refactor/round-18", baseline v19). The refactor programme is at **round 18 merged**.

**Uncommitted working tree** — the previous session's orphan removal, left staged-but-uncommitted
deliberately so you can build it first:

```
 M .claude/skills/jpms-operator/references/site-map.md
 M contracts/Ai/PageGuides/OfficePageGuides.cs
 M contracts/Ai/PageGuides/RequestPageGuides.cs
 M jpms/Program.cs
 M jpms/README.md
 M jpms/Services/StoreChangeHub.cs
 D jpms/Components/LeadStageBadge.razor
 D jpms/Components/LeadsBySourceTable.razor
 D jpms/Components/LeadsTable.razor
 D jpms/Features/Leads/LeadPipelineReadModel.cs
 D jpms/Features/Leads/LeadsRouteRegistration.cs
 D jpms/Pages/EstimatingQueue.razor
 D jpms/Pages/Nurture.razor
 D jpms/Pages/SalesAnalytics.razor
 D jpms/Services/HttpLeadStore.cs
 D jpms/Services/ILeadStore.cs
?? docs/ui/
```

Also `_to_delete/` at the repo root holds two scratch files from that session — bin it.

**The doctrine, in reading order.** `CLAUDE.md` at the root is authoritative for house
conventions and every component you extract must respect it — in particular its sections on
**Terminology**, **Record tabs & the in-view toolbar**, **Loading states**, **Error reporting**,
**Front-end data-loading convention** and **Project ordering**. Then `jpms/DESIGN-SYSTEM.md`
for the tokens (dark palette: canvas `#0B0B0C`, surface `#161719`, accent green `#57E08A`,
Poppins; and the button-colour rule — *every button that does something is green*). Then the two
reference documents the previous session produced:

- `docs/ui/component-anatomy.md` — every one of the 89 views broken into its parts.
- `docs/ui/navigation-map.md` — every route and how it is reached.

(HTML versions of both are in `handover-prompt/` if you want to read them in a browser:
`jpms-component-anatomy.html`, `jpms-navigation-map.html`.)

---

## 2. What was established

### 2a. Three orphaned routes were removed

`/estimating-queue`, `/nurture` and `/sales-analytics` were the CRM front end: routed, rendered,
and unreachable — no rail row, no inbound link anywhere in the app. Deleted along with their whole
front-end tail (the two lead tables, the stage badge, `ILeadStore`/`HttpLeadStore`,
`jpms/Features/Leads`, the DI and route registrations in `Program.cs`, the `ILeadStore`
subscription in `StoreChangeHub`, three AI page guides in `contracts/Ai/PageGuides`, and the
`jpms/README.md` route rows). 648 lines.

**Kept on purpose:** the Leads API (`api/Features/Leads`, ~20 endpoints) and the `list_leads`
connector tool. The leads pipeline therefore has a live backend and no UI. That is a decision to
revisit, not an oversight — if a CRM surface is wanted again it gets designed properly and wired
into the rail.

Nothing was compile-checked (no `dotnet` on my Mac). **Job zero for you: build it, then commit.**

### 2b. The front end has 344 components and almost no shared vocabulary

Verified by grep across `jpms/Pages` (89 views):

| Finding | Number |
|---|---|
| Components defined across `Components/`, `Features/`, `Layout/` | 344 |
| Of those, used on five or more pages | 9 |
| Pages hand-rolling the auth preamble instead of `ApprovedSessionGate` | 52 of 89 (24 use it) |
| Pages hand-rolling their own header row (no `PageHeader` exists) | 68 of 89 |
| Pages hand-rolling a status span while `Components/StatusPill.razor` goes unused | 24 (0 use it) |
| Pages hand-rolling a `<table>` while `SortableColumnHeader` goes unused | 29 (0 use it) |
| Pages using `LoadGate` — the one component with real adoption | 70 |

The lesson from `LoadGate`: one component plus a written rule in `CLAUDE.md` got near-universal
adoption. Everything below should ship the same way — component *and* rule, together.

### 2c. The proposed component spine

Names taken from what the views already do. **have** = exists and is used · **idle** = exists and
pages ignore it · **build** = nothing owns the pattern. The number is how many of the 89 views
need it — build in that order.

**1 · Shell** — `ApprovedSessionGate` (idle, 24/76) · `ProjectPageShell` (have, 33) ·
`WorkspaceSectionNav` (have, 9) · `RecordTabBar` (have, 2)

**2 · Page chrome** — `PageHeader` (build, 68) · `ActionCluster` (build, ~40) ·
`Toolbar`/`ToolbarButton` (have, 10) · `SectionPanel` (idle — `Panel` exists, 13)

**3 · In-view navigation** — `ChipTabs` (build, ~30) · `SegmentedTabs` (build, ~12) ·
`SearchInput` (build, ~15) · `FilterBar` (build, ~14)

**4 · Data display** — `RecordsTable` (build, 29) · `StatusPill` (idle, 0/24) ·
`StatTileGrid`/`Stat` (idle, 1/~8) · `LedgerStatementRow` (build, ~6) · `KeyValueList` (build, ~12) ·
`EmptyState` (idle — `EmptyMessage` exists, 2/~35)

**5 · Feedback** — `AlertBanner` (build, ~60 — the single most duplicated element in the app) ·
`LoadGate` (have, 70 — leave it alone) · `ErrorToast` (have, layouts)

**6 · Forms, dialogs, destructive acts** — `Modal` (have, 26) · `FormField` (idle, 10/26) ·
`InlineEditPanel` (build, ~8) · `InlineConfirm` — the two-click armed delete (build, ~6) ·
`StatusTransitionMenu` — a status pill that is also the menu of valid transitions (build, ~4)

### 2d. Two rules the codebase has never written down

Both need my decision before the components that depend on them are built. Ask me.

1. **Modal vs inline edit panel.** Roughly eight views (Inventory, Policies, Defects,
   Subcontractor detail, AI skills…) open add/edit forms in place; the rest use `Modal` for
   equivalent CRUD. There is no rule saying which is correct when.
2. **Chip rows: links or buttons?** The RFI register's type chips are `<a href>` (they change the
   route); the status chips beside them are `<button>` (they change local state). They look
   identical. Decide whether that difference should be visible, or whether one wins.

---

## 3. The work, in order

Each of these is one round: a branch, a build, a patch delivered the way
`jewel-refactor-handover-prompt.md` §8 describes. Do not batch them.

1. **Build and commit the orphan removal** (§2a). Nothing else in the same commit.
2. **`ApprovedSessionGate` adoption.** Pure mechanical win: 52 files, ~15 lines each, no design
   decisions. `Pages/ProjectRequests.razor` and `Pages/RfiDashboard.razor` are the stragglers
   worth doing first — RfiDashboard also skips `LoadGate` entirely for a bare "Loading RFIs…"
   string, which breaks the loading convention in `CLAUDE.md`.
3. **`AlertBanner`.** Highest-frequency new component, no layout risk. Write the `CLAUDE.md` rule
   for it in the same round: which variant means what, and that a dismissible error carries its
   JPMS reference.
4. **`PageHeader` + `ActionCluster`.** 68 views. Do a handful first, show me, then roll out.
   It must enforce the existing house rule: one green primary action per view, everything else in
   the Toolbar.
5. **`StatusPill` adoption + `StatusTransitionMenu`.** Needs a single status vocabulary and colour
   ramp agreed with me first — the same status currently reads differently from register to
   register.
6. **`RecordsTable`.** The big one. `SortableColumnHeader` already exists and is unused; the seven
   extracted domain tables (`RequestTable`, `ProjectsTable`, `DrawingsTable`, `SubcontractorTable`,
   `RateTable`, `FinancialsTable`, `ValuationReportTable`) should end up thin wrappers over it.
7. **`ChipTabs` / `SegmentedTabs` / `SearchInput` / `FilterBar`** — after the rule in §2d.2.
8. **The rest of tier 4 and 6** as the views come up in normal work.

Keep `docs/ui/component-anatomy.md` current as you go: when a ⚠️ becomes a real component, the
tree entry changes to ✅. That file is the scoreboard.

---

## 4. How I want you to work

Same as the refactor rounds: read before you write, one concern per round, build before you claim
anything works, and tell me plainly when something cannot be verified. Do not push or open PRs
unless I say so — I merge each round myself. If you are unsure whether a pattern is genuinely
shared or just looks similar, say so rather than extracting it; a wrong abstraction here is more
expensive than a duplicated div.
