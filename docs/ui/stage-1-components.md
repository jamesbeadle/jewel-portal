# Stage 1 shared components — the list to discuss

> **Status 2026-09-06 (evening): all twelve built and rolled out** on branch `ui/stage-1-components`
> (`dotnet build` 0 errors, Tailwind build clean). What each round actually touched is in the
> commit messages on that branch; the rules are in `CLAUDE.md` → *Shared components* and the
> lint grep in `DESIGN-SYSTEM.md` §5. Correction to item 12: `Money()` was already used 327 times
> via the static import — only the DATE helper was unused; both now go through `DateFormats`.
> Item 6 nuance: pane switches that don't navigate (Programme · Claims · Critical RFIs) keep the
> underline TabRow look — they are views, not filters.

Surveyed from `main` at `04985b1` (the Tailwind retune) on 2026-09-06: 452 razor files under
`jpms/` — 92 in `Pages`, 134 in `Components`, 223 in `Features`, 3 in `Layout`. Every number below
comes from a grep over those four folders; the commands are in the appendix so the counts can be
re-run after each round.

The test for "belongs in Stage 1" was: the pattern recurs across many views today, the Figma
already gives it a recipe (or the retune settled it), and extracting it is mostly mechanical —
no new behaviour, no data-model decisions. Each entry says what it is, what it replaces (with the
evidence), the API it would have, and the rule that ships with it. Things that are really
Stage 2 (they need a decision, an unread Figma frame, or a behaviour change) are listed at the
end so they don't get lost.

**How each one lands** (the `LoadGate` lesson — 107 files use it because it came with a rule):
component + one paragraph in `CLAUDE.md` + every current call site converted in the same round +
a grep in `DESIGN-SYSTEM.md` §5 that finds any new hand-rolled version. One component per
round, build-verified, you merge.

---

## The Stage 1 list, in build order

### 1. `Notice` — the inline message box

**What it replaces.** The same red box, hand-typed 160+ times:
`rounded-lg bg-negative/10 border border-negative/30 px-3 py-2 text-sm text-negative` appears
verbatim 75 times, plus 49 with `mb-3`, 22 with `mb-4`, 6 with `mt-2`, 4 with `mt-3`, and 6 more
with `/40` and white text. 207 files carry a `negative`-toned class; 90 files hold an
`actionError`/`loadError`/`saveError` field. The green/info version
(`bg-accent/10 border-accent/30`) recurs 6 times and the amber one uses raw Tailwind
`amber-50/200/800` — colours that are not tokens. `Components/NoticePanel.razor` exists (Positive
/ Warning tones, uppercase title) and is used once.

**API.** `<Notice Tone="Negative|Warning|Positive|Info" Title="…" Dismissible OnDismiss="…">body</Notice>`
— body optional, title optional, `Class` for margin. `rounded` (4px), `border-line-strong`-weight
border in the tone colour at /30, tone-coloured text, 14/Reg. `NoticePanel` folds into it.

**Rule.** A message ABOUT an action goes through `Notice`; never a hand-rolled box. Field-level
validation (400/409/422) stays next to the field as `<p class="text-sm text-negative">`.
This is the same component the later `NoticeService`/`NoticeBanner` round will render in the
layout — building the inline one first means that round is plumbing, not design.

**Why first.** Highest count, zero layout risk, and it removes the raw `amber-*` colours that the
lint grep should be catching.

### 2. `Pill` — the status badge

**What it replaces.** 92 `rounded-full … px-2 … text-[11px]|text-xs` spans in 47 files, in at
least ten different class strings, plus seven bespoke badge components that each carry their
own colour map: `StatusPill` (timesheets only, 4 uses), `ComplianceStatusPill` (5),
`RoleBadge` (2), `ActivityBadge` (3), `ValuationDueBadge` (2), `ProjectStageBadge` (4),
`TodoAssigneeBadge` (4) — and page-local helpers `StatusPillClass`, `RequestStatusPillClass`,
`StatusChipClass`. Sixteen files still use raw `emerald-*` colours for status.

**API.** `<Pill Tone="Positive|Negative|Info|Neutral|Muted" Dot="true|false">Approved</Pill>`.
`rounded` 4px (the retune already moved `StatusPill` and `ProjectStageBadge` off `rounded-full`),
12/Med, tone at /10 fill + tone text. The seven domain badges become one-line wrappers that map
their enum to a Tone — that's where the "one status vocabulary" lives, and it's the only place a
status colour is ever decided.

**Rule.** A status is a `Pill`. A domain badge is a `Pill` wrapper, never its own span.

**Open.** The Figma has no status pill on any desktop frame walked so far (§8 of the rules doc);
Positive/Negative/Neutral on `surface-raised` is the expected reading. Build now with that,
restyle the one component if the Financing frames say otherwise.

### 3. `PageHeader` + `Page` — the page's chrome, once

**What it replaces.** 51 files render their own `<h1>` in 16 different class strings
(`text-2xl md:text-3xl font-semibold … leading-tight` ×30, `text-2xl font-semibold … mb-2` ×19,
`text-4xl` ×2…), 13 of them inside a hand-rolled
`<header class="flex items-end justify-between">` with the action cluster on the right, 4 with
a "JPMS" eyebrow above. Under that, 60 pages open with a gutter `<section>` in one of 13
padding variants (`px-6 md:px-8 py-12` ×59 occurrences, `px-6 md:px-8 py-8 md:py-12` ×24,
`px-4 md:px-8 py-12` ×20…).
And 55 pages still hand-roll the `RequestAccessView` auth preamble that `ApprovedSessionGate`
(22 uses) already owns.

**API.** Two components that are usually used together:
`<Page>` = `ApprovedSessionGate` + the one gutter (`px-8 py-6`, Figma's 32px content gutter)
+ optional `MaxWidth`; and
`<PageHeader Title="…" Subtitle="…" Count="…">` with `<Primary>` (the ONE green button) and
`<Actions>` (a `Toolbar`) slots. Title 20/Med per the Figma top bar, subtitle 14/Reg
`content-muted`, count strapline rendered only when the value is known (loading convention).
`ProjectPageShell` (30 uses) keeps its API and renders `PageHeader` inside.

**Rule.** Every routed view except the landing page is `<Page>`; every view has exactly one
`PageHeader`; the only labelled green button on a page lives in its `Primary` slot. Section
paddings and `h1` classes disappear from pages.

**Open — needs your call.** The layout's top bar already shows the page title (`PageHeading`,
driven by `PageContext`), and the Figma has the title *only* there. Today most pages show the
title twice — top bar and in-content `h1`. Options: (a) `PageHeader` drops the title and carries
subtitle/count/actions only, matching the Figma; (b) keep the in-content title and make the top
bar show the section/project instead. I'd recommend (a) — it's what the design does — but it
changes every page's first line, so it's yours to decide.

### 4. `FormField` (generalised) + `Checkbox`

**What it replaces.** The label string
`block text-xs uppercase tracking-wider text-content-subtle font-semibold mb-1` is typed out
**230 times**, another 20 with `tracking-wide`, 38 as `eyebrow block mb-1`, 35 as
`block text-sm text-content-muted` — 129 files carry a raw `<label>`. `FormField` exists (32
uses) but only wraps a text `<input>`, so every `<select>` (72 files), `<textarea>` (52), date
input (26) and `SearchSelect` (53) hand-rolls its label. Checkboxes (53 files) have eight
different class strings and none of them is the Figma checkbox.

**API.** `<FormField Label="…" Hint="…" Error="…" Required>` with `ChildContent` — any control
goes inside and picks up the `field` class; the existing `Value`/`OnChange` shortcut stays for
the plain text case. Plus `<Checkbox @bind-Value Label="…">` (16×16, `rounded border-line-strong
bg-surface-field`) and `<Radio>` when a group comes up. Label per Figma: 14/Med white, 4px gap.
The 230 uppercase labels are already wrong against the retune (rule: no uppercase, nothing
under 12px) — this is the round that fixes them.

**Rule.** No bare `<label>` in a page or feature; a control's label comes from `FormField`.
Add/edit forms open in a `Modal` (your decision of 2026-09-03), and the modal footer's Large
buttons are already automatic.

### 5. `RecordsTable` shell + `SortableColumnHeader` adoption

**What it replaces.** 94 files render a `<table>`; only 6 use `data-table`. The header cell is
hand-rolled in 94 `<thead>`s across **128 distinct** `<th>` class strings, the commonest being
(`sticky top-0 z-20 bg-surface-raised px-4 py-3` ×33, `px-4 py-2.5 text-xs uppercase …` ×22,
`px-2 py-3` ×19…). 37 files re-implement the sticky header, 17 have a `<tfoot>` totals row,
21 make rows clickable in their own way, 5 own their sort state, and `SortableColumnHeader`
(exists) is used by 2. 358 cells right-align money by hand.

**API.** `<RecordsTable Sticky Dense IsLoading="…" EmptyMessage="…">` owning the `data-table`
skin (already the Figma recipe after the retune), the sticky header, the loading gate, the
empty row, and `RowClick`. Inside it the callers keep writing their own `<th>`/`<td>` for now —
the win in Stage 1 is that the *skin and the shell* stop being retyped, and every table gets the
Figma header/borders/row height at once. `SortableColumnHeader` becomes the header cell for any
sortable column; a `MoneyCell`/`text-right` convention covers the 358 right-aligned cells. The
seven domain tables (`RequestTable`, `ProjectsTable`, `DrawingsTable`, `SubcontractorTable`,
`RateTable`, `FinancialsTable`, `ValuationReportTable`) are converted first — they're already
components, so converting them converts their pages.

**Rule.** A list of records is a `RecordsTable`. A sortable column uses `SortableColumnHeader`.
Money is right-aligned via the convention, never a hand-typed `text-right whitespace-nowrap`.

**Scope note.** This is the biggest item and deliberately the *shell* only. A fully generic
column-definition table (`Columns="…" Rows="…"`) is Stage 2 — the domain tables have enough
bespoke cells (percent editing, roll-ups, expand/collapse) that forcing them through a column
model now would be the "wrong abstraction is more expensive than a duplicated div" case.

### 6. `TabRow` + `FilterChips` — in-view navigation, two looks

**What it replaces.** 29 files carry a private class-builder with one of 25 names
(`TabClass` ×4, `ViewTabClass` ×4, `ChipClass` ×3, `FilterClass` ×2, `BucketTabClass`,
`PathwayFilterChipClass`, `ScopeChipClass`, `GroupChipClass`…) producing at least four visual
dialects: accent underline (`border-b-2 border-accent`), solid inverted pill
(`bg-content text-surface`), tinted pill (`bg-accent/10 text-accent`), and a solid rounded-md
block (`bg-accent text-accent-ink`). `WorkspaceSectionNav` (10 uses) already has the right
route-tab look.

**API.** `<TabRow Items="…" Active="…">` — links that change the route, underline style, the
`WorkspaceSectionNav`/`RecordTabBar` look generalised; and
`<FilterChips Items="…" Selected="…" OnSelect="…" Counts="…">` — buttons that change local state,
pill style, optional count badge, `AllowMultiple`. Route-tabs vs filter-chips is the decision
you already made (2026-09-03); this is the pair of components that makes the difference visible.

**Rule.** Route change = `TabRow`. Local filter = `FilterChips`. No page defines a `*TabClass`
or `*ChipClass` helper.

**Open.** The exact chip/tab treatment is in the P&L frames still to be read from the Figma
(§8). The underline/pill split doesn't depend on it; the paddings and the active colour might.

### 7. `SearchInput`

**What it replaces.** 16 `type="search"` inputs and 19 "Search…" placeholders, each with its own
width/icon/clear handling.

**API.** `<SearchInput @bind-Value Placeholder="Search" Debounce="250" Width="w-[430px]">` —
the Figma `_Input` with the search glyph trailing, `content-faint` placeholder, top-right of the
content area by default. Small, and it usually sits beside `FilterChips`, so build it in the same
round as 6.

### 8. `StatTile` (merge) + `MetricStat` adoption

**What it replaces.** Three overlapping tile components — `Stat` (3 uses), `FigureTile` (3),
`MetricStat` (2), `AdminStatsRow` (1) — while 55 files hand-roll an `eyebrow` label over a
figure. After the retune `Stat` and `FigureTile` render identically.

**API.** One `<StatTile Label Value IsLoading Icon Id Caption>` = the Figma "Asset Metric" card
(`p-6`, 14/Med label, 20/Med value, optional 96×2 rule and 24px icon). `MetricStat` stays as the
un-boxed headline figure with delta. `FigureTile` and `Stat` are deleted; `AdminStatsRow` becomes
a grid of `StatTile`.

**Rule.** A figure with a label is a `StatTile` or a `MetricStat`, and it takes `IsLoading` —
never renders a zero before the store lands (this is already the loading-states rule; the
component is what enforces it).

### 9. `EmptyState`

**What it replaces.** 80 hand-rolled `<p class="… text-content-subtle">No …</p>` lines and
no component (the anatomy doc's "EmptyMessage" is a `RenderFragment` parameter on one list,
not a component).

**API.** `<EmptyState Message="No work orders yet">` with optional `<Action>` slot for the one
thing to do about it. 14/Reg `content-subtle`, centred in the region, `py-10`. Renders nothing
while its region is loading (the gate above it decides). `RecordsTable`'s `EmptyMessage`
renders this.

### 10. `Panel` adoption + `SectionHeader`

**What it replaces.** `Panel` (32 uses) vs 130 hand-rolled `<div class="panel …">` in 89 files,
and 34 files with a `<h2 class="text-lg font-semibold">` + actions row above an un-boxed
section, in five `flex justify-between` variants.

**API.** `Panel` already has `Title`/`HeaderActions`/`IsLoading`; this is adoption, not build.
Add `<SectionHeader Title="…">` with an `Actions` slot for the un-boxed case (Figma: 18/Semi,
`mb-6` above its table).

**Rule.** A bordered region is a `Panel`; a titled un-boxed region starts with `SectionHeader`.
`class="panel"` is not written in pages.

### 11. `ConfirmDialog` + `InlineConfirm`

**What it replaces.** 39 files carry a two-step confirm flag under twelve different names
(`confirmingDelete` ×4, `deleteArmed` ×2, `armed`, `confirmingRemove`, `confirmingClose`,
`discardArmed`…), and destructive buttons are styled five different ways
(`btn-neutral px-2 py-1 text-xs text-negative border-negative/40` ×6, `btn-ghost text-negative`
×3…). The Control Centre's "Discard did nothing" confusion (2026-09-03) was this pattern with no
shared behaviour.

**API.** `<ConfirmDialog Title Message ConfirmLabel Danger OnConfirm>` — a `Modal` preset with
the Large footer and the destructive label; and `<InlineConfirm Label="Delete" Confirm="Delete
work order?" Danger OnConfirm>` — the two-click armed button with the second state visibly
different (label changes, `border-negative`) and a timeout back to armed-off.

**Rule.** Anything irreversible confirms through one of these two; a page never holds its own
`confirming*` bool.

### 12. `Date` / `Money` display helpers

**What it replaces.** Not a component, but the same consistency problem: `ToString("d MMM
yyyy")` is typed 100 times, `"dd MMM yyyy"` 24, `"HH:mm"` 15; money is `"N2"` ×19, `"C2"` ×14,
`"C0"` ×6, `"N0"` ×4 — while `DateFormats.cs` and `MoneyFormats.cs` exist with **zero** call
sites.

**API.** `<Date Value="…" />` / `<Money Value="…" />` render components (so a table cell is
`<td><Money Value="@row.Total" /></td>` and gets the right-align + `font-medium text-content`
Figma treatment for free), backed by the two existing helpers. Mechanical find-and-replace;
fold it into the `RecordsTable` round.

---

## Deferred to Stage 2 (and why)

- **`NoticeService` + layout `NoticeBanner`** — your decision stands; it needs `Notice` (item 1)
  built first, then it's plumbing.
- **`PageLoadScope`** (one jewel + skeleton per page) — the loading round; depends on
  `Page`/`PageHeader` existing so there is a shell to hang it on.
- **Fully generic `RecordsTable` with column definitions** — after the shell (item 5) has
  converted the seven domain tables and shown which cell types actually recur.
- **`StatusTransitionMenu`** — `DropdownMenu` exists (24 uses) and 8 files already drive
  status changes through it; a pill-that-is-a-menu needs the status vocabulary from item 2 first.
- **`InlineEditPanel`** — not a component: the 10 in-place add/edit forms convert to `Modal`
  per your CRUD rule. That's a migration list, done page by page in normal work.
- **Numbered `Pagination`** — Figma item, no page uses `Pagination` today (0 uses), so there's
  nothing to unify yet.
- **`KeyValueList`** — 7 `<dl>`s, `MetaRow` 0 / `MetaCell` 1 / `SummaryRow` 2 uses. Real but
  small; carries the "Issued vs Created" two-dates rule. Do it when a detail page is next touched.
- **Toggle switch, date picker, dropdown recipe, login layout** — all §8 Figma frames not yet
  read.
- **Mobile `/site/*`** — separate pass, mobile frames unread.

## Existing components this list retires or merges

`NoticePanel` → `Notice` · `StatusPill`, `ComplianceStatusPill`, `RoleBadge`, `ActivityBadge`,
`ValuationDueBadge`, `ProjectStageBadge`, `TodoAssigneeBadge` → `Pill` wrappers ·
`Stat` + `FigureTile` → `StatTile` · `AdminStatsRow` → grid of `StatTile` · `PageHeading` stays
(it's the top bar) · `WorkspaceSectionNav` and `RecordTabBar` stay and share `TabRow`'s look.

## What I need from you before starting

1. Item 3: does the in-content page title go (Figma: title only in the top bar), or stay?
2. Confirm the order — I've put the three zero-risk, highest-count ones first (`Notice`, `Pill`,
   `PageHeader`) so the portal visibly tightens before the table round.
3. Whether you want the §8 Figma frames read (you sign in, browser pane) before items 2 and 6,
   or built on the expected reading and restyled after.

---

## Appendix — the greps behind the numbers

Run from `jpms/` with `S="Pages Components Features Layout"`.

```
# 1 Notice
grep -rhoE 'class="[^"]*rounded[^"]* [^"]*(border-negative|bg-negative|border-amber|bg-amber|border-accent/|bg-accent/10)[^"]*"' $S | sort | uniq -c | sort -rn
grep -rlE 'actionError|errorMessage|saveError|loadError' $S | wc -l
# 2 Pill
grep -rlE 'class="[^"]*rounded(-full)? [^"]*px-2[^"]*(text-\[11px\]|text-xs)' $S | wc -l
for c in StatusPill ComplianceStatusPill RoleBadge ActivityBadge ValuationDueBadge ProjectStageBadge TodoAssigneeBadge; do grep -rl "<$c\b" $S | wc -l; done
# 3 PageHeader / Page
grep -rhoE '<h1 class="[^"]*"' $S | sort | uniq -c | sort -rn
grep -rhoE '<section class="px-[0-9]+ md:px-[0-9]+ py-[0-9]+[^"]*"' $S | sort | uniq -c | sort -rn
grep -rl '<ApprovedSessionGate' Pages | wc -l ; grep -rlE 'RequestAccessView' Pages | wc -l
# 4 FormField
grep -rhoE '<label class="[^"]*"' $S | sort | uniq -c | sort -rn
grep -rl '<select' $S | wc -l ; grep -rl '<textarea' $S | wc -l ; grep -rl 'type="date"' $S | wc -l ; grep -rl 'type="checkbox"' $S | wc -l
# 5 RecordsTable
grep -rl '<table' $S | wc -l ; grep -rl 'data-table' $S | wc -l
grep -rhoE '<th[^>]*class="[^"]*"' $S | sort | uniq -c | sort -rn
grep -rl 'sticky top-0' $S | wc -l ; grep -rl '<tfoot' $S | wc -l ; grep -rhoE '<td class="[^"]*text-right[^"]*"' $S | wc -l
# 6 TabRow / FilterChips
grep -rhoE 'private (static )?string (\w*(Chip|Tab|Pill|Filter|Segment)\w*)\(' $S | sort | uniq -c | sort -rn
# 7 SearchInput
grep -rhoE '<input[^>]*type="search"' $S | wc -l ; grep -rlE 'placeholder="Search' $S | wc -l
# 8 StatTile
for c in Stat MetricStat FigureTile AdminStatsRow; do grep -rl "<$c\b" $S | wc -l; done
# 9 EmptyState
grep -rhoE '<p class="[^"]*text-content-(subtle|faint|muted)[^"]*">No [^<]{3,40}' $S | wc -l
# 10 Panel / SectionHeader
grep -rl '<Panel\b' $S | wc -l ; grep -rhoE '<(section|div|article) class="panel[^"]*"' $S | wc -l
grep -rlE -B1 -A1 '<h2 class="text-lg font-semibold' $S | wc -l
# 11 Confirm
grep -rhoE 'bool (confirm\w+|armed\w*|\w+Armed)' $S | sort | uniq -c | sort -rn
# 12 Date / Money
grep -rhoE 'ToString\("(dd MMM yyyy|d MMM yyyy|HH:mm)"\)' $S | sort | uniq -c
grep -rl 'DateFormats\.' $S | wc -l ; grep -rl 'MoneyFormats\.' $S | wc -l
```
