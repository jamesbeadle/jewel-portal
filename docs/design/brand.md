# Brand

Sources: `docs/ui/open-book-design-rules.md` (the Open Book Figma read on 2026-09-03, local styles and Properties panels, not screenshots), `jpms/tailwind.config.js` (the theme those values became), `jpms/DESIGN-SYSTEM.md`; read 2026-09-19. Where this sheet and an earlier jpms rule disagree, the Figma wins (James, 2026-09-03). Every value here is a token name; the hexes live in the Tailwind config and nowhere else.

## Palette

| Token | Brand name | Role — use for | Never for |
| --- | --- | --- | --- |
| `canvas` | BG/Dark | The page. Also a table **header** row (it drops back to the canvas), the modal panel, and the text on a green control (`accent-ink`). | A card or panel fill. |
| `surface` | Panels/Dark | Chrome, cards, panels, table bodies — the second fill in depth order. | Anything that should read as raised or editable. |
| `surface-raised` | Panels/Table Highlight | A hovered or selected row, a highlighted cell. | A resting fill; decoration. |
| `surface-field` | Panels/Table Highlight | The fill of an input, select, textarea, checkbox, radio. Same hex as `surface-raised` on purpose. | Anything that is not a control. |
| `line` | Boarders/Outline | Structural borders: the side nav, the top bar, cards, panels, **small** buttons. | Table cells, inputs, the modal. |
| `line-strong` | Boarders/Table Seperator | Anything row- or field-shaped: table cells, inputs, radios, checkboxes, the modal, **large** buttons; the hover border of a bordered button. | Card and chrome edges. |
| `content` | Text/White | Titles, values, button labels, header cells, nav hover/active. | Body text in a table (too loud). |
| `content-muted` | Text/G4 | Table body text; card icons. | Labels. |
| `content-subtle` | Text/G5 | Labels, captions, ids, eyebrows (sentence case, 14/Med). | Disabled or placeholder text — it is content, not a hint. |
| `content-faint` | Text/G6 | Inactive nav items, placeholders, muted actions, the "+" tile. | Anything the user must read. |
| `accent` | Status/Positive | **The one solid-green primary control per view or dialog footer.** | A second control in the same view; text; borders on secondary buttons (retired 2026-09-03); decoration. |
| `accent-hover` | *(not a Figma style)* | The pressed/hovered state of the primary control only. | Anything at rest. |
| `accent-ink` | BG/Dark | Text and glyphs on an `accent` fill. | Anything else. |
| `positive` | Status/Positive | A positive outcome or delta: a figure, a `Tone.Positive` pill or notice. Same green as `accent`. | A button fill (that is `accent`, once); decoration. |
| `negative` | Status/Negative | A negative figure, an error, a `Tone.Negative` pill or notice, destructive text on a secondary button (`text-negative`). | A button fill; an outline; anything not a failure or a loss. |
| `negative-strong` / `negative-ink` | *(jpms's own)* | The error toast fill and its text only. | Anywhere else. |
| `info` | Status/Neutral | Links inside data (tables, records) and neutral status (`Tone.Info`). | Actions; headings. |
| `warning` | *(jpms's own — the file has no amber)* | "Needs a look" (`Tone.Warning`): an expiring document, a figure that fits another order, an overdue-but-not-failed state. | Errors (that is `negative`); success; decoration. |
| `brand` | Brand/Main | The logo and brand mark; the Sales pathway's pane tone (`Tone.Accent`, a category, not a verdict). | Actions, links, status. |

Tones are the only way a view picks a status colour: `Tone.Negative / Warning / Positive / Info / Muted / Accent` through `StatusTones.cs` → `Pill`, `Notice`. A view never names a colour token for a status.

## Type

Poppins only, weights 400 / 500 / 600 / 700, letter-spacing 0 everywhere. Size says the role; **weight carries hierarchy**. Nothing below `text-xs`; no uppercase, no tracking.

| Class | Size · line | Role |
| --- | --- | --- |
| `text-xs` (12/14) | font-medium | Captions ("Last 30 days"), pagination numbers, the stat's caption. |
| `text-sm` (14/16) | Reg | Table body text, body copy. |
| `text-sm` (14/16) | font-medium | Labels, form labels, small-button labels, eyebrows, deltas. |
| `text-sm` (14/16) | font-semibold | Table header cells. |
| `text-base` (16/20) | font-medium | Nav items, input values, radio labels, large-button labels, the org name. |
| `text-lg` (18/22) | font-semibold | Section titles (`SectionHeader`, `Panel` title, a modal's section title). |
| `text-xl` (20/20) | font-medium | The page title in the top bar; a card's figure. |
| `text-2xl` (24/24) | font-semibold | A modal's title. |
| `text-2xl` (24/24) | font-medium | The headline stat figure. |
| `text-4xl` / `text-5xl` | — | Hero figures — not on any walked screen; reserved. |

## Spacing

The 4px grid: 4, 8, 10, 12, 16, 24, 32, 40, 56, 80. Roles: content gutter 32 (`px-8`); section title above its table 24 (`mb-6`); card padding 24 (`p-6`); modal padding 32 (`p-8`) with a 40 stack (`gap-10`); table cell padding 16 tall / 24 wide (`px-6 py-4`), header `py-3`; row height 48 (`h-12`), header 40 (`h-10`); small button 8/16 (`px-4 py-2`, `h-8`), large 16/32 (`px-8 py-4`, `h-[52px]`); label to control 4 (`gap-1`); inputs side by side 16 (`gap-4`), groups 32 (`gap-8`); nav 24/32 (`px-8 py-6`), nav items 24 apart (`gap-6`).

## Radius · Elevation · Motion

- **Radius**: `rounded` (4px) on buttons, inputs, checkboxes and every control; `rounded-lg` (8px) on the modal only; `rounded-sm` on the pagination's current-page chip; `rounded-full` on radios, avatars and pills. **Cards, panels, the side nav, the top bar and table cells have no radius.** `rounded-xl` and `rounded-md` are not in the brand.
- **Elevation**: one shadow in the whole site, the modal's (`shadow-[0_4px_16px_rgba(0,0,0,0.8)]`). Nothing else is elevated: depth is fill (`canvas` → `surface` → `surface-raised`) and borders. No gradients, no translucency on solid surfaces (alphas are flattened into the tokens).
- **Motion**: `animate-jewel-pulse` (1.6s ease-in-out, white ↔ G6) is the loading mark, drawn only by `LoadGate`; `transition` on hover fills and borders. Nothing else animates; nothing moves on load.

## Unmapped

- `accent-hover` — not a Figma style; the Button component's Hover variant has not been read (open item in `open-book-design-rules.md` §8). Kept as an assumption.
- `warning` — not a Figma style; jpms's own tone for "needs a look". Kept: the site needs a third verdict colour and the file has none.
- `negative-strong` / `negative-ink` — jpms's own, the error toast fill; kept for WCAG AA on the toast.
- `Boarders/Inline` (`#37393C` in the Figma) — no theme token and no use on any walked screen; not added.
- Text/G1, G7, `#F5F5F5` (a pagination library style) — folded into `content`; no tokens.
- States the Figma walk has not reached: checked checkbox/radio, input focus ring, selected/hovered table row, status pills on the Financing screens — each marked `⚠ to confirm` in the design rules and built on the assumption named there.
