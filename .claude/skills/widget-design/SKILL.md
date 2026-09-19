---
name: widget-design
description: The brand and the widgets' designs — set up the design index, extract the brand sheet (palette with roles, type, spacing, radii, elevation, motion) and a widget's design sheet from images or any reference into the repository's own token names, and check the site against them. Use when the person says "Set up the widget designs", "Extract the brand from <references>", "Extract the design for <Widget> from <images>", "Check the widgets against their designs", "Check the site against the brand", "are the brand colours used appropriately", or asks whether a widget matches its design — a question gets the reading and changes nothing; a "Check" fixes the widget on a branch. Not for finding where the widgets are written by hand (that is "Run the widget identification") and not for adopting them (Pass 1 of "Refactor the repo"). Optional per repository — only a site with a widget catalogue (rules.json siteDefinition.catalogue) has designs to keep.
---

# The brand and the widgets' designs

Two layers, one structure. The **brand sheet** is the base: every colour, type size, spacing step, radius, elevation and motion the site may use, each with its *role* — what it is for and what it is never for — written in the theme's token names. A widget's **design sheet** is an override on top of it for one catalogue widget. Nothing can be checked against a design until the brand is in this shape, so the brand sheet comes first.

A design is a reference a person supplies for one catalogue widget — usually images (a screenshot of a Figma frame, a mock-up, a photograph of a sketch), sometimes a link or a page. The kit turns it into a **design sheet**, a short file in a fixed shape written in the repository's own token names, and the sheet is what a Claude reads before building or changing that widget. A widget with no sheet is designed by the brand guide alone, which is the normal case; a sheet is an override, given only where the person has a design thought. Nothing here is scored or gated: the audit reports the standing (how many sheets, when the site was last checked against them) in `tools/refactor/site-definition.md` and the README box, and the checking is a judgement made on request.

**A question gets the reading, not the fix.** *Are the brand colours used appropriately?*, *does RecordsTable match its design?* are answered by making the comparison below and reporting what it found — where a token sits outside its role, where a widget departs from its sheet — with nothing changed, no branch and no pull request. The fix is the **Check**, run when the person says *"Check the site against the brand"* or *"Check the widgets against their designs"*; it changes widgets, so it goes on a branch and ends as a pull request. Where a widget is written by hand in a view rather than composed from the catalogue, that is the widget identification's finding and the round's adoption pass, not a design matter.

Everything lives under the index the audit reads, `docs/design/widgets.json` unless `rules.json` → `siteDefinition.designIndex` says otherwise:

```json
{
  "brand": "docs/design/brand.md",
  "brandSources": ["docs/ui/brand-guidelines.pdf", "jpms/tailwind.config.js"],
  "brandCheckedAt": "2026-09-19",
  "widgets": {
    "RecordsTable": {
      "design": ["docs/design/widgets/RecordsTable/default.png", "docs/design/widgets/RecordsTable/empty.png"],
      "sheet": "docs/design/widgets/RecordsTable.md",
      "checkedAt": "2026-09-19",
      "checkedAgainst": "3bf97854"
    }
  }
}
```

`brand` names the brand sheet every widget falls back to (until it is extracted, the repository's existing brand guide or design-system file). A widget absent from `widgets`, or present with no `sheet`, is brand only. All of it is committed on a branch and reaches the default branch by a pull request, like everything else.

## "Set up the widget designs"

Create the index with `brand` pointing at whatever the repository already has (a design-rules or design-system document under `docs/`, a `DESIGN-SYSTEM.md`, a Figma export; ask if there is none), `brandSources` listing it, `widgets` empty, and `docs/design/widgets/` for the sheets. Then run the quality check so the site definition and the README box show the standing. That is all set-up is; the brand sheet is the first extraction, and designs arrive one widget at a time after it.

## "Extract the brand from <references>"

The references are whatever holds the brand: guideline documents, images of the palette or type, a Figma export, an existing design-rules file, and always the theme itself (the Tailwind config, the CSS custom properties). Read them all, then write `docs/design/brand.md` in this shape:

```
# Brand
Sources: <listed>, read <date>.

## Palette
| Token | Brand name | Role — use for | Never for |
One row per colour token in the theme. The role is the sentence that decides a use: "primary action, one per view", "positive outcome, never decoration", "surface behind a panel".
## Type
| Token / class | Size · weight · line | Role |
## Spacing
The scale, and what each step is for (gutter, gap between panels, inside a control).
## Radius · Elevation · Motion
Each token with its role; motion as duration/easing tokens with what animates and what never does.
## Unmapped
Theme tokens with no brand role · brand colours with no theme token · raw values found in the theme that are not tokens.
```

The role column is the whole point: "the brand colours are used appropriately" is not checkable until each colour says what appropriate means. Show the sheet and the Unmapped list to the person and wait for a yes; then write `brand` and `brandSources` in the index, run the quality check, commit on `feature/brand-sheet`.

## "Extract the design for <Widget> from <images>"

1. **Read the reference whole.** Every image the person gives (they can attach them to the chat, or point at files); a link or a page is read the same way. Say what each image shows — which state, which variant — before writing anything.
2. **Read the widget and the theme.** The widget's own file (it is in `siteDefinition.catalogue`, so it has one), and the theme's tokens: the Tailwind config, the CSS custom properties, the design-system file — wherever colours, type, spacing, radii and elevation are named.
3. **Write the sheet** at `docs/design/widgets/<Widget>.md` in this shape, every section present even if it says "as the brand":

   ```
   # <Widget> — design
   Source: <the images or link, listed>, read <date>. Read from images: values are readings, mapped to the nearest token.

   ## Anatomy
   The parts a user sees, top to bottom / left to right, each named.
   ## Tokens
   Colour, type, spacing, radius, elevation — one line per part, in the theme's token names only.
   ## States
   default · hover · active/selected · disabled · loading · empty · error — what changes in each; "not shown" where the reference has none.
   ## Variants
   Dense, with selection, with totals… — what differs.
   ## Behaviour
   What the widget does that the picture implies: sticky header, sort on header press, scroll box, dismiss on outside press.
   ## Unmapped
   Every value in the reference that sits near no token (a colour, a size, a radius), with the reading and the nearest token it is not.
   ```

   The rule that matters: **the sheet speaks in token names, never raw values.** A hex colour or a pixel size in the sheet is the magic-value rule broken one step early. A reading that fits no token goes under *Unmapped* — that is a decision for the person (add the token, or the design is off-brand), never something resolved silently.
4. **Copy the images in** under `docs/design/widgets/<Widget>/`, named for the state they show, and write the index entry: `design`, `sheet`, `checkedAt` empty (the sheet is new; the widget has not been checked against it).
5. **Show the person the sheet and the Unmapped list and wait for a yes** before committing — a sheet is a design decision recorded, so a person confirms it, as they confirm a house model before it is stored.
6. Run the quality check so the standing updates; commit on a `feature/widget-design-<widget>` branch.

## "Check the widgets against their designs"

For every widget in the index that has a sheet (or the ones the person names):

1. Read the sheet and the widget's file side by side. Compare part by part — anatomy, tokens, states, variants, behaviour. A difference is one of three things: the widget is wrong (fix it, on this branch, behaviour of the *site* unchanged — this is the widget's look, and every view that composes it changes with it, which is the point of the catalogue); the sheet is wrong (the design moved on — say so and stop on that widget until the person re-extracts or amends); or an agreed exception (record it under a `## Exceptions` heading in the sheet, with the reason).
2. Never touch a view: a widget's design is met in the widget's own file. If the difference can only be fixed in the views that use it, the widget is missing a parameter or a variant — add it to the widget, then the views adopt it in the ordinary way.
3. Stamp the index: `checkedAt` today, `checkedAgainst` the commit the check was made on.
4. Run the quality check (the standing updates, and the ratchet gate must still pass — a design fix never adds a hand-rolled element), then commit on `feature/widget-design-check-<date>`, push, and open the pull request. Say in it which widgets changed, which sheets were found stale, and which exceptions were recorded.

## "Check the site against the brand"

With a brand sheet in place: list every use of a colour, type, radius or elevation token across the views and widgets (search the class names and custom properties the theme defines) and read each against its role. A use outside its role — an accent colour on a decoration, a negative tone on something that is not a failure, a second primary action in a view — is fixed in the widget where the catalogue owns the look, or in the view where the view chose the tone; a token with no role is an Unmapped line for the person; an agreed exception is recorded under `## Exceptions` in the brand sheet with its reason. Raw values in views are the audit's business already (inline magic values) and are not repeated here. Stamp `brandCheckedAt` in the index, run the quality check, commit on `feature/brand-check-<date>`, push, open the pull request, and say what moved.

The refactor round does not run this — it reports the standing at its start (the `refactor-round` skill says so) and leaves the check to the person, because a design is a judgement and a round changes no behaviour. The check is worth running after a batch of widget adoption steps, when a sheet is added or changed, and before a design review.
