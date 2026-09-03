# Open Book → JPMS: design rules and the Tailwind classes that carry them

Read out of the **Open Book** Figma (file `g7fm7dfuSYX81z1sdKAoyD`, Dashboard + Branding pages) on
2026-09-03 — every value below was taken from the file's local styles or a selected layer's
Properties panel, not eyeballed from a screenshot. Companion to `jpms/DESIGN-SYSTEM.md` (which was a
best-guess adaptation of the same file and asked for its hexes to be checked — this is that check)
and `docs/ui/component-anatomy.md` (what the views are built from).

Purpose: when the component-architecture round lands (PageHeader, RecordsTable, NoticeBanner,
PageLoadScope, ChipTabs…), each new component should be built from **these** class recipes, so the
portal matches the design by construction rather than by review.

Sections 8 (open items) lists the handful of states the walk has not yet reached — they are marked
`⚠ to confirm` wherever they appear and nothing else in this document depends on them.

---

## 1. Colour — the Figma styles, and the Tailwind token each one becomes

Figma has **twenty** colour styles in seven groups. Several carry alpha; Tailwind wants solid hexes
(the `/90` modifier only works on solid tokens), so each alpha style is given both its raw value and
its **flattened** value over the surface it actually sits on in the file.

| Figma style | Raw | Alpha | Sits on | Flattened | Proposed token | Current jpms | Δ |
|---|---|---|---|---|---|---|---|
| BG/Dark | `#101111` | — | — | `#101111` | `canvas` | `#0B0B0C` | retune |
| Panels/Dark | `#1A1A1D` | 90% | canvas | `#19191C` | `surface` | `#161719` | retune |
| Panels/Table Highlight | `#2E323A` | 70% | surface | `#282A31` | `surface-field` (inputs) **and** `surface-raised` (hover/selected) | `#2A2D31` / `#1F2024` | retune / merge |
| Boarders/Outline | `#2E323A` | — | — | `#2E323A` | `line` (structural: nav, top bar, cards, small buttons) | `#232427` | retune |
| Boarders/Table Seperator | `#454B56` | 80% | surface | `#3C414A` | `line-strong` (tables, inputs, modal, large buttons) | `#34373B` | retune |
| Boarders/Inline | `#37393C` | — | — | `#37393C` | *(not used on any walked screen — keep as `line-inline` only if a use turns up)* | — | — |
| Text/White | `#FFFFFF` | — | — | | `content` | `#FFFFFF` | ✓ |
| Text/G4 | `#DDDDDD` | — | — | | `content-muted` (table body text) | `#C4C8CE` | retune |
| Text/G5 | `#D1D1D1` | — | — | | `content-subtle` (labels, captions, ids) | `#8A9099` | retune — much lighter |
| Text/G6 | `#8C8C8C` | — | — | | `content-faint` (inactive nav, placeholders, muted buttons, "+" tile) | `#5A5F68` | retune |
| Text/G2 | `#F3F3F3` | — | — | | `content-strong`? → **use `content`** — G2 is the nav hover/active colour and is visually white | — | fold into `content` |
| Text/G1, G3, G7 | `#F9F9F9`, `#E8E8E8`, `#000000` | | | | G3 is the card icon colour; G1/G7 unused on walked screens | — | fold G3 into `content-muted` |
| Status/Positive | `#66E094` | — | — | | `accent` **and** `positive` — one green, the file uses the same style for the primary button fill and positive deltas | `#57E08A` / `#4ED07D` | retune + unify |
| Status/Negative | `#FF403C` | — | — | | `negative` | `#FF4D4D` | retune |
| Status/Neutral | `#3CA1FF` | — | — | | `info` — also the **link colour inside tables** | `#4691F6` | retune |
| Brand/Main | `#4CDBEE` | — | — | | `brand` — logo / brand mark only; it is **not** the action colour | — | new (icon use only) |
| Modal BG/Dark | `#161819` | 60% | canvas | | `modal-overlay` → `bg-canvas/70` is already close; exact = `#161819/60` | `bg-canvas/70` | keep |
| Modal BG/Light | `#161819` | 20% | | | lighter overlay (nested/secondary dialogs) | — | optional |

Notes that change how the tokens are *used*, not just their values:

- **Accent text colour on green is `#101111`** (the canvas), i.e. `accent-ink` = `canvas`. Already so.
- **There is no separate "hover" green in the file's styles.** `accent-hover` (`#4ECF7E`) is a jpms
  invention; keep it, but it is an assumption (`⚠ to confirm` — the Button component has a State
  property whose Hover variant has not been read yet).
- The two border styles split by *role*: **Outline** (`line`) frames chrome and cards; **Table
  Seperator** (`line-strong`) frames anything with rows or fields — table cells, inputs, radios,
  checkboxes, the modal, and *large* buttons. Small buttons use Outline. Today jpms uses `line`
  everywhere and `line-strong` only for hover; the mapping above keeps both tokens but gives
  `line-strong` a real job.
- `surface-raised` and `surface-field` collapse to one Figma style (Table Highlight). Keep both
  names for readability, give them the same hex.

### Proposed `tailwind.config.js` colours

```js
colors: {
  canvas: '#101111',
  surface: { DEFAULT: '#19191C', raised: '#282A31', field: '#282A31' },
  line:    { DEFAULT: '#2E323A', strong: '#3C414A' },
  content: { DEFAULT: '#FFFFFF', muted: '#DDDDDD', subtle: '#D1D1D1', faint: '#8C8C8C' },
  accent:  { DEFAULT: '#66E094', hover: '#5ACF85' /* assumption */, ink: '#101111' },
  positive: '#66E094',
  negative: { DEFAULT: '#FF403C', strong: '#8E1616', ink: '#FFF1F1' }, // strong/ink stay: toast fill, not in Figma
  info:  '#3CA1FF',
  brand: '#4CDBEE'
}
```

Every existing class keeps working — this is a value change, not a rename — so the whole portal
moves to the Figma palette in one commit with no markup churn. The one visible behavioural change is
`content-subtle` becoming much lighter (labels stop reading as disabled).

---

## 2. Typography

**Poppins only**, weights 400 / 500 / 600 / 700 (already loaded in `index.html`). Letter-spacing is
0% on every style. Eight sizes, each in Reg / Med / Semi / Bold, with **fixed line-heights that are
tighter than Tailwind's defaults**:

| Figma style | size/leading | Tailwind today | Proposed override | Used for (from the walk) |
|---|---|---|---|---|
| 12px | 12 / 14 | `text-xs` 12/16 | `text-xs` → 12/14 | captions ("Last 30 Days"), pagination numbers |
| 14px | 14 / 16 | `text-sm` 14/20 | `text-sm` → 14/16 | **the workhorse**: table cells, labels, small-button labels, form labels |
| 16px | 16 / 20 | `text-base` 16/24 | `text-base` → 16/20 | nav items, input values, radio labels, large-button labels, org name |
| 18px | 18 / 22 | `text-lg` 18/28 | `text-lg` → 18/22 | section titles ("Account History", "Aged Debtors"), modal section titles |
| 20px | 20 / 20 | `text-xl` 20/28 | `text-xl` → 20/20 | page title in the top bar ("Banking"), card value |
| 24px | 24 / 24 | `text-2xl` 24/32 | `text-2xl` → 24/24 | headline stat value, modal title |
| 40px | 40 / 40 | `text-4xl` 36/40 | `text-4xl` → 40/40 | *(hero figures — not on walked screens)* |
| 48px | 48 / 48 | `text-5xl` 48/48 | keep | *(hero — not on walked screens)* |

Proposed `theme.extend.fontSize` so existing `text-sm` etc. pick up the Figma leading automatically:

```js
fontSize: {
  xs:   ['12px', { lineHeight: '14px' }],
  sm:   ['14px', { lineHeight: '16px' }],
  base: ['16px', { lineHeight: '20px' }],
  lg:   ['18px', { lineHeight: '22px' }],
  xl:   ['20px', { lineHeight: '20px' }],
  '2xl':['24px', { lineHeight: '24px' }],
  '4xl':['40px', { lineHeight: '40px' }],
  '5xl':['48px', { lineHeight: '48px' }],
}
```

Rules that fall out of the type scale:

- **Nothing is smaller than 12px.** jpms's `text-[11px]` (eyebrow, StatusPill, MetricStat caption,
  FigureTile label, ProjectStageBadge) all become `text-xs`.
- **There are no uppercase/tracked eyebrows in the design.** Labels are `14/Med` sentence case in
  `content-subtle` (G5). The `eyebrow` class (11px uppercase tracking-wider) is a jpms idiom with no
  Figma equivalent — replace with `text-sm font-medium text-content-subtle`. Same for `FormField`'s
  uppercase label and `SortableColumnHeader`'s `uppercase tracking-wider`.
- **Weight carries hierarchy, not size.** Table header vs body is 14/Semi vs 14/Reg at the same size;
  label vs value in a stat is 14/Med G5 vs 24/Med white. Keep to the four weights.
- Numbers are the same Poppins — no tabular-nums or mono in the file.

---

## 3. Shape, space, elevation

- **4px grid**: every padding/gap in the file is 4, 8, 10, 12, 16, 24, 32, 40, 56 or 80.
- **Radius is small and rare**: `4px` on buttons, inputs, checkboxes (`rounded`); `8px` on the modal
  (`rounded-lg`); `2px` on the pagination current-page chip (`rounded-sm`); `44px`/full on radios
  (`rounded-full`). **Cards, panels, side nav, top bar and table cells have no radius at all.**
  jpms's `panel` (`rounded-xl` = 12px), `modal-panel` (`rounded-xl`), `btn` (`rounded-md` = 6px) and
  `field` (`rounded-md`) all overshoot — see §7.
- **Borders are 1px, always a token, never a Tailwind default grey.**
- **One shadow in the whole file**: the modal, `0 4px 16px 0 #000000 @80%` → `shadow-[0_4px_16px_rgba(0,0,0,0.8)]`.
  Nothing else is elevated; hierarchy is done with fill (canvas → surface) and borders.
- **Page geometry (desktop frame 1728 × 1117)**: side nav `w-[240px]` fixed, top bar `h-[88px]`
  spanning the rest, content starts at x = 272 → a **32px gutter** (`px-8`) inside the shell; content
  width 1424 at that frame size. Section titles sit 24px above their table (`mb-6`). Headline stat
  row sits at y = 112 (directly under the top bar, `pt-6`).

---

## 4. Component recipes (Figma → Tailwind)

Every recipe below is the exact Figma spec followed by the class string that reproduces it with the
tokens from §1–2. Where jpms already has a class/primitive for the role it is named, with what has
to change.

### 4.1 App shell

**Side navigation** (component `Side Navigation/Banking/Open`)
`w-[240px] h-full flex flex-col gap-20 px-8 py-6 bg-surface border-r border-line`
- Top bar inside it: logo (white mark, 28×40) + collapse icon 24, `flex justify-between h-10`.
- Menu items: `flex flex-col gap-6`; each item `flex items-center gap-4 h-6 text-base font-medium`
  with a 24px icon. **Default** `text-content-faint` (G6, icon and label). **Hover / active**
  `text-content` (G2 → white). No background pill, no left rule — colour only.
- Foot: user avatar 32 + org name `text-base font-medium text-content-faint`, `gap-2`.
- The 80px gap between the logo row and the menu is real (`gap-20`).

**Top bar** (`Top Menu Bar`)
`h-[88px] flex items-center justify-between pl-14 pr-8 py-6 bg-surface border-b border-line`
- Left: page title `text-xl font-medium text-content` (20/Med — *not* semibold, *not* 2xl).
- Right: avatar 40 (`w-10 h-10 rounded-full`).
- jpms `PageHeading` is `text-xl font-semibold` — drop to `font-medium`.

### 4.2 Buttons (component `Button`, props Size / Type / Icon / State)

| Variant | Figma | Tailwind | jpms class |
|---|---|---|---|
| **Small · Minimal** (every in-view action: Export, Filters, Add Transaction, Bulk Action) | h32, px16 py8, gap4, radius 4, border 1 Outline, no fill, icon 16 + label 14/Med white | `inline-flex items-center gap-1 h-8 px-4 py-2 rounded border border-line text-sm font-medium text-content` | `btn-secondary` / `btn-neutral` — today green-outlined; see §6 |
| **Small · Minimal, muted** ("Bulk Action" with nothing selected) | as above, icon + label G6 | add `text-content-faint` (or `disabled:` → same) | `disabled:opacity-50` → prefer colour swap |
| **Large · Secondary** (modal Cancel) | h52, px32 py16, gap4, radius 4, border 1 Table Seperator, label 16/Med white | `inline-flex items-center gap-1 h-[52px] px-8 py-4 rounded border border-line-strong text-base font-medium text-content` | `btn-neutral` (+ size) |
| **Large · Primary** (modal Add / Save) | h52, px32 py16, radius 4, **fill Status/Positive**, label 16/Med `#101111`, no border | `inline-flex items-center gap-1 h-[52px] px-8 py-4 rounded bg-accent text-base font-medium text-accent-ink` | `btn-primary` (+ size) |
| Icon inside a button | 16px glyph, `currentColor` | `w-4 h-4` | `ActionIcon` ✓ |

Rules:
- **Two sizes only.** Small (h-32, text-sm) for in-view actions; Large (h-52, text-base) for dialog
  and form submits. jpms `btn` is h≈36 (`py-2 text-sm` + `px-3.5`) — between the two; move to the
  Figma pair.
- **Primary = solid green, once per view.** Matches the existing jpms rule exactly.
- **Everything else is a bordered, unfilled button in white text.** The Figma "Minimal" type has
  *no green* on it — the jpms green-outline rule is retired (§6.1).
- Radius `rounded` (4px), not `rounded-md`.

### 4.3 Stats

**Headline stat** (`Asset Metric`, the un-boxed one under the top bar: "Pending / $34,723.55")
`flex flex-col gap-6 py-6 w-[255px]` on canvas, no border —
label `text-sm font-medium text-content-subtle` · value `text-2xl font-medium text-content` ·
info block right-aligned: delta `text-sm font-medium text-positive|text-negative` over caption
`text-xs font-medium text-content-subtle`.
→ jpms `MetricStat`: value is `text-2xl md:text-3xl font-semibold tracking-tight` → `text-2xl font-medium`
(no tracking); label `text-sm text-content-subtle` ✓ add `font-medium`; caption `text-[11px]` → `text-xs`.

**Stat card** (`Asset Metric` component, State Default, Size Desktop — the account cards)
`w-[220px] flex flex-col gap-6 p-6 bg-surface border border-line` (**square corners**) —
top row `flex items-center gap-2`: 24px icon `text-content-muted` (G3) + id `text-sm text-content-subtle`;
a 96×2 rule `bg-line-strong`; then `flex flex-col gap-4`: label `text-sm font-medium text-content-subtle`,
value `text-xl font-medium text-content`.
→ jpms `Stat` / `FigureTile`: `panel p-4` + eyebrow → `p-6 gap-6`, sentence-case label, `text-xl` value.

**"Add" tile** (`Plus`): `w-14 h-14 p-2 border border-line flex items-center justify-center` with a
40px `+` in `text-content-faint`.

### 4.4 Records table (`Table Wrapper`)

The table is drawn as columns of cells; translated to `<table>`:

- **Wrapper**: no radius, outer border comes from the cells (`border-l` on first column, `border-r`
  on last) → `table` `border border-line-strong` is the simplest equivalent.
- **Header cell** (`Account`, `Date`…): `h-10 px-6 py-3 bg-canvas border-y border-line-strong text-sm font-semibold text-content`
  — header is on the **canvas** colour, white, SemiBold, sentence case.
- **Body cell**: `h-12 px-6 py-4 bg-surface border-b border-line-strong text-sm text-content-muted`
  (14/Reg, G4). Money in the trailing column is `font-medium text-content`. Link cells (customer
  name) are `font-medium text-info`.
- **Row height 48, header 40, cell padding 16/24** — jpms `data-table` is `px-4 py-3` (16/12) with
  a `text-content-subtle` header and no cell borders; the Figma table is denser horizontally and
  more separated vertically.
- Checkbox cell: 16×16 checkbox (`rounded border border-line-strong bg-surface-field`) + `gap-2`.
- Selected / hovered row: `⚠ to confirm` — expected `bg-surface-raised` (the Table Highlight style
  exists for exactly this; the "Cell Highlight - Select" frame is in the open list).
- Section title above the table: `text-lg font-semibold text-content mb-6`.

Proposed `data-table` rewrite:

```css
.data-table            { @apply w-full text-sm text-left border border-line-strong; }
.data-table th         { @apply h-10 px-6 py-3 bg-canvas border-b border-line-strong font-semibold text-content; }
.data-table td         { @apply h-12 px-6 py-4 bg-surface border-b border-line-strong text-content-muted; }
.data-table tbody tr:last-child td { @apply border-b-0; }
.data-table tbody tr.is-clickable:hover td { @apply bg-surface-raised cursor-pointer; }
```

### 4.5 Pagination

`flex items-center justify-between h-6 w-[313px] text-xs font-medium` — page numbers `text-content`
(`#F5F5F5`, a library style), current page a chip `h-6 px-2.5 py-0.5 rounded-sm bg-accent text-accent-ink`,
arrows 24px `text-surface-raised`(muted) / white, `…` ellipsis.
→ jpms `Pagination` (prev/next `btn-secondary` + "Page x of y") is a different pattern; the design
wants numbered pages. Keep the range label, add numbers.

### 4.6 Inputs

**Text / select** (`_Input`): `h-11 w-full flex items-center justify-between px-4 py-3 rounded border border-line-strong bg-surface-field text-base text-content placeholder:text-content-faint`
— trailing 16px icon (`Arrow-Dwn` for selects, search glyph for search).
**Label** (`Chain*`): `text-sm font-medium text-content` above the control, **`gap-1`** (4px).
Required mark is a `*` in the same style.
**Two inputs side by side**: `flex gap-4`; **groups** stack with `gap-8`.
**Search** (Debtors): same `_Input`, `w-[430px]`, placeholder "Search" in `text-content-faint`, top-right of the content area.
→ jpms `field`: `rounded-md … px-3 py-2 text-sm` → `rounded px-4 py-3 text-base h-11`; `FormField`
label: uppercase eyebrow → `text-sm font-medium text-content mb-1`; focus ring `⚠ to confirm`
(no focus state on walked screens — keep `focus:border-accent`).

**Radio**: `w-6 h-6 rounded-full border border-line-strong bg-surface-field p-1.5` + label
`text-base font-medium text-content`, `gap-2`; options stacked `gap-2`.
**Checkbox**: `w-4 h-4 rounded border border-line-strong bg-surface-field`.
Checked states `⚠ to confirm` (expect an accent dot / accent fill).

### 4.7 Modal (`_ Simple Modal Base`)

`w-[600px] flex flex-col gap-10 p-8 rounded-lg bg-canvas border border-line-strong shadow-[0_4px_16px_rgba(0,0,0,0.8)]`
- **Header**: `flex items-center justify-between h-6` — title `text-2xl font-semibold text-content`,
  close icon 24 white. (The header has a 1px bottom border listed but, at h-6 inside a gap-10 stack,
  reads as a hairline under the title.)
- **Section title** inside the body: `text-lg font-semibold text-content`, section content `gap-4`.
- **Footer**: `flex gap-2.5` — Large Secondary (Cancel) then Large Primary.
- Overlay: `#161819 @60%` ≈ `bg-canvas/70` ✓.
→ jpms `modal-panel`: `max-w-md rounded-xl border-line bg-surface shadow-2xl` and a `px-5 py-4
text-base` header. Figma is wider (600), on **canvas** not surface, radius 8, 32px padding, 24px
title. `Modal.razor` keeps its API; only `modal-panel`/header/footer classes change.

### 4.8 Links and status colour

- In-table links: `font-medium text-info` (`#3CA1FF`).
- Positive / negative figures and deltas: `text-positive` / `text-negative`, same weight as the
  surrounding text (14/Med in the stat's info block).
- Status pills: `⚠ to confirm` (no pill appears on walked desktop screens; if the Financing screens
  carry one it will be Positive / Negative / Neutral on `surface-raised`).

---

## 5. Usage rules (the doctrine to ship with each component)

1. **Three fills, in order of depth**: page = `canvas`; chrome, cards and table bodies = `surface`;
   fields and highlighted rows = `surface-field` / `surface-raised`. A table **header** drops back to
   `canvas`. Nothing is lighter than `surface-raised` except a green button.
2. **Two borders**: `line` around chrome and cards; `line-strong` around anything row- or
   field-shaped (tables, inputs, radios, checkboxes, modal, large buttons).
3. **Four text tones, monotonic**: `content` (white) for titles, values, button labels, header cells;
   `content-muted` (G4) for table body text; `content-subtle` (G5) for labels and captions;
   `content-faint` (G6) for inactive nav, placeholders and muted actions. Never a raw grey.
4. **Green means "do it", once**: one solid `accent` control per view (or per dialog footer);
   everything else is an unfilled bordered button in white. Red is for figures and errors, never
   for a button fill. Blue (`info`) is for links in data and neutral status, never for actions.
5. **Type: size by role, hierarchy by weight**: 20/Med page title · 18/Semi section title ·
   24/Semi modal title · 24/Med headline figure · 20/Med card figure · 16/Med nav & inputs ·
   14/Med labels & buttons · 14/Reg body · 12/Med captions. No 11px, no uppercase, no tracking.
6. **Corners: 4px on controls, 8px on the modal, 0 on everything else.** No `rounded-xl`.
7. **No shadows except the modal.** No gradients, no translucency on solid surfaces (flatten alphas).
8. **Spacing on the 4px grid**: cell padding 16/24, button padding 8/16 (small) 16/32 (large),
   card padding 24, modal padding 32, nav padding 24/32, content gutter 32.

---

## 6. Decisions (design vs. current portal rules)

**Standing decision (James, 2026-09-03): where this document and an earlier jpms rule disagree,
the Figma wins.** The earlier rules were stop-gaps for lower-level inconsistencies; this file
replaces them.

1. **Green-outline secondary buttons — DECIDED: follow Figma.** The jpms rule (DESIGN-SYSTEM §2,
   `btn-secondary` / `btn-icon`: *every action button is green*) is retired. Figma's "Minimal" type
   is a **grey-outlined white-text** button, and green appears only on the single primary.
   `btn-secondary` and `btn-icon` become `border-line text-content` (hover: `border-line-strong`);
   `btn-neutral` collapses into `btn-secondary` (same look — keep the name as an alias for one round
   so nothing breaks). The action/dismiss distinction is carried by position and label, as in the
   design: primary green on the right, dismiss to its left.
2. **Uppercase eyebrows.** Not in the design at all; jpms uses them for labels, form labels and
   table headers. Recommend dropping them (rule 5) — it is a find-and-replace on `eyebrow`,
   `uppercase tracking-wider`.
3. **Panel radius.** `rounded-xl` cards vs square Figma cards. Recommend square (rule 6) — one
   token change in `.panel`.
4. **Modal surface.** Figma modal is on `canvas` (darker than the page's panels) with an 8px radius
   and 600px width; jpms modal is `surface`, 12px, 448px. Recommend Figma values; `max-w-md` →
   `max-w-[600px]`.
5. **Pagination style.** Numbered pages with a green current chip vs prev/next buttons.
6. **`content-subtle` lightens a lot** (`#8A9099` → `#D1D1D1`). Labels will read as content, not as
   hints — which is what the design intends; hints move to `content-faint`.

---

## 7. Concrete change list for `jpms` (in order; each is a small, mechanical commit)

| # | File | Change | Effect |
|---|---|---|---|
| 1 | `tailwind.config.js` | colours per §1, `fontSize` per §2, add `brand` | whole portal retunes to the palette + leading; no markup change |
| 2 | `Styles/app.tailwind.css` `.panel` | `rounded-xl` → none; keep `border-line bg-surface` | square cards |
| 3 | `.btn` / `.btn-primary` / `.btn-neutral` | `rounded-md px-3.5 py-2` → `rounded h-8 px-4 py-2 text-sm font-medium`; add `.btn-lg` (`h-[52px] px-8 py-4 text-base`) for dialog footers | two Figma sizes |
| 4 | `.btn-secondary` / `.btn-icon` / `.btn-neutral` | green outline → `border-line text-content hover:border-line-strong`; `btn-neutral` = alias of `btn-secondary` | Figma Minimal button (§6.1 decided) |
| 5 | `.field` | `rounded-md px-3 py-2 text-sm border-line` → `rounded h-11 px-4 py-3 text-base border-line-strong` | Figma input |
| 6 | `.data-table` | per §4.4 block | Figma table |
| 7 | `.modal-panel` + `Modal.razor` header/footer | per §4.7 | Figma modal |
| 8 | `.eyebrow` | `text-[11px] uppercase tracking-wider … font-semibold` → `text-sm font-medium text-content-subtle` (class name can stay for the transition) | rule 5 |
| 9 | `MetricStat`, `Stat`, `FigureTile`, `StatusPill`, `ProjectStageBadge`, `FormField`, `SortableColumnHeader`, `PageHeading` | `text-[11px]` → `text-xs`; drop `uppercase tracking-wider`; weights per §4 | rule 5 |
| 10 | `Pagination` | add numbered pages, current = `rounded-sm bg-accent text-accent-ink` chip | §4.5 |
| 11 | `DESIGN-SYSTEM.md` | replace §1 token table with §1 here; add §5 rules | single source of truth |

Lint additions for the Phase-5 grep: `text-\[11px\]`, `rounded-xl`, `rounded-md`, `uppercase`,
`tracking-`, `shadow-` (other than the modal) — each is now a drift signal.

---

## 8. Open items — still to read from the file

These frames exist and are next on the walk; nothing above depends on them:

- `Desktop - Dashboard - Debtors - Cell Highlight - Select` → selected/hover row fill.
- `Desktop - Dashboard - Debtors - Menu Open` → row action dropdown (`DropdownMenu` recipe).
- `Desktop - Dashboard - Profit & Loss` (+ `Accordian open`, `Filters`, `Currency Selctor`) →
  tabs vs pills, accordion rows, filter chips.
- `Desktop - Dashboard - Balance Sheet - Edit Columns` → toggle switch.
- `… Filters Model Open` → date picker, multi-select.
- `Desktop - Dashboard - Log In` → login page layout.
- `User Profile Menu` → account menu.
- `Button` component Hover/Pressed/Disabled variants → `accent-hover`, disabled treatment.
- Any status pill on the Financing screens.
- Mobile frames (`Mobile - Dashboard - *`) → the `/site/*` field app (Phase 4) — a separate pass.
