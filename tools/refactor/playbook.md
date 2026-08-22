# The Refactoring Playbook

A staged process for bringing any repository to the standard defined in the root `CLAUDE.md` coding rules. The stages run in order; each produces an artefact the next stage consumes. The audit scripts in `audit/` make every stage measurable, and the gate in `audit/gate.py` makes progress irreversible.

The premise: a refactor without a feature freeze is a treadmill, and a refactor without measurement is a matter of opinion. This playbook fixes both. Draw the line, snapshot the numbers, and only ever let them improve.

**The engine, in one sentence: shrink first, abstract second, let patterns arrive third.** On the backend, big files are broken into functions each with one purpose; in the UI, big pages are broken into components each with one purpose. Abstraction is never attempted against a large unit — it is only once the units are small that common functionality becomes visible, and only once enough commonality has been extracted that design patterns appear, because patterns are exactly the shapes that handle common functionality abstractly. Nothing in the stages below reverses this order: the taxonomy and lifecycle stages give extraction a *direction*, but the abstractions themselves are discovered from the shrunken code, never imposed on the large.

## Stage 0 — Baseline

Run the audit and commit the result as the baseline the gate ratchets against.

```
cd tools/refactor
python3 -m audit.run_audit <repositoryRoot> --output audit-output
cp audit-output/audit.json baseline.json
```

`audit-report.md` is the human summary; `audit.json` holds every offender list. From this commit onwards, CI runs the audit and `python3 -m audit.gate baseline.json audit-output/audit.json` fails any change that makes a ratcheted figure worse. Refactoring can now proceed in any order without the codebase regressing behind it. When a stage improves the numbers, re-copy `audit.json` over `baseline.json` to lock in the gain.

## Stage 1 — Recover the product model

Work the chain backwards, as the coding rules describe: infer the user stories from the UI, map every view and how views link into a site map, trace views to entities, entities to backend operations. The artefact is `docs/refactor/site-map.md`: every routable page, the stories it serves, and what links to it. Any page no story reaches is flagged for deletion; any story with no reachable page is flagged as a gap. Nothing is refactored yet — this stage only establishes what the code is *for*, so later extraction decisions are made against the domain rather than against the accidents of the current files.

The audit's inventory (`inventory` section of `audit.json`) is the raw material: the full page list with line counts and the widgets each page uses.

## Stage 2 — Abstract page and widget taxonomy

Classify every page from Stage 1 into a small set of **page archetypes** — the shapes that recur in any line-of-business application: the list page (filterable table of records), the detail page (one record, sectioned), the dashboard (summary widgets fanning out), the workbench (a queue plus a focused work area), the wizard (stepped flow). If a page fits no archetype, either the taxonomy is missing one or the page is conflating two and should be split.

Then classify every visual element into **widget types**: table, form, modal, panel, badge, chart, picker, toast, empty state, load state. The audit's `componentReuse` list shows which widgets already exist as shared components and which are re-implemented inline page by page — every page carrying its own `<table>` markup is a widget type waiting to be extracted.

The artefact is `docs/refactor/taxonomy.md`: the archetype of every page, the widget types it composes, and — for each widget type — the single shared component that will render it. This is the target architecture, and the sentence structure of the whole UI: *a page is an archetype composed of widgets; a widget is a shared component with one lifecycle.*

## Stage 3 — The component lifecycle contract

Before extracting anything, define the one lifecycle every widget follows, so every extraction lands in the same shape:

1. **Parameters in** — typed, named for the domain, no grab-bag objects.
2. **Load** — data arrives through one gate (here, `LoadGate`); the widget never invents its own loading flag.
3. **Render states** — loading, empty, error, ready. Every widget renders all four; the empty and error states are designed, not accidental blank space.
4. **Events out** — the widget raises named events; it never reaches back into its parent or navigates on its parent's behalf.

Also fix the **transition language**: one duration/easing token set, one skeleton/spinner idiom, one toast idiom — named tokens in the theme, never inline values. The artefact is the contract written down (`docs/refactor/component-lifecycle.md`) plus the token file. Every widget extracted in Stage 4 must satisfy it, which is what makes the widgets generic and consistent rather than merely smaller.

## Stage 4 — The extraction loop

The mechanical heart of the refactor. Worst file first, from the `fileLength` offender list:

1. Pick the largest file over the limit.
2. Identify the sections that are widgets in the Stage 2 taxonomy; extract each into the shared component for that widget type, conforming to the Stage 3 lifecycle. Extract page-specific logic into named partials/classes beside the page.
3. Behaviour must not change — same rendered output, same events, tests still green.
4. Re-run the audit; the file count over the limit must fall. Lock the baseline in.
5. Repeat.

The loop is deliberately boring. All design thinking happened in Stages 2 and 3; this stage only applies it. A page refuses to shrink below the limit only when the taxonomy is missing a widget type or the page is two pages — go up a stage, never force it.

This is the "shrink first" half of the engine. Extraction here does not chase abstraction — a section becomes a component because it has one nameable purpose, not because it resembles something elsewhere. The resemblances are Stage 5's job, and they can only be seen once this stage has made the units small.

## Stage 5 — DRY consolidation

Take the `duplication` hotspot list and merge each clone family into one named concept. Judge each family with the rules' own test: is this the same *concept*, or coincidentally similar code? Same concept → extract under a name that captures the concept; coincidence → leave it. Typical families this audit surfaces: endpoint gate boilerplate (resolve user → authorise → validate → handle, repeated per endpoint — extract a gate runner so an endpoint states only what varies), document renderers sharing layout scaffolding, and near-identical page sections that Stage 4 already turned into widgets. Re-run the audit; the duplication percentage ratchets down.

This is the "abstract second" half of the engine, and it is where design patterns are allowed to appear — not chosen from a catalogue, but recognised: once enough common functionality has been pulled into named units, the structures that handle commonality abstractly (a factory, a strategy, a pipeline) surface on their own. Name a pattern only after it has already formed.

## Stage 6 — Backend articulation check

Verify the CQRS chain end to end: every user story from Stage 1 has exactly one command or query; every endpoint passes through all three gates — authentication (signed-in user resolved), **authorisation** (an explicit rule allowing this user this operation, including on queries), validation — before any domain logic. Grep-level checks make this concrete: count endpoints lacking an authorisation gate and drive that number to zero or to a documented "deliberately public" list. DTOs derive from what handlers need; delete any that nothing demands.

## Stage 7 — Security sweep

Stack-specific, checklist-driven, in its own session with fresh eyes. For this stack: Static Web Apps auth boundary and route rules; every Functions endpoint's gate order (Stage 6 feeds this); secrets — nothing in source or infra scripts, everything in Key Vault/app settings, admin SQL connection strings out of setup scripts; third-party token handling (Graph, Xero, AI keys) — storage, refresh, scope minimisation; SQL access via EF only; upload/attachment handling; infra scripts idempotent and free of embedded credentials; dependency and framework patch level. Each finding becomes a fix commit or an accepted-risk note in `docs/refactor/security-review.md`.

## Stage 8 — Design conformance

With the widget set consolidated, compare against the source designs (Figma exports of components and tokens). One pass per widget type, not per page — that is the payoff of Stage 2: fixing the table component's spacing fixes every table in the product. Extract the design tokens (colour, type scale, spacing, radii, elevation, motion) into the theme; the `magicValues` audit figures (inline hex colours, inline style attributes) ratchet to zero. Then one pass per page archetype for layout and flow: entry, load choreography, transitions between states, exits.

## Stage 9 — Hold the line

The gate stays in CI permanently. New code obeys the rules from birth; the ratchet means the numbers only travel one way. Revisit the baseline quarterly: tighten any figure that has headroom (for instance, lower `maxFunctionLines` once the worst offenders are gone).

## Adapting this to another repository

Everything repo-specific lives in `rules.json`: source globs, page/component globs, widget markers, style-token globs. Point them at the new codebase, run Stage 0, and the same nine stages apply. The taxonomy and lifecycle contracts are re-derived per product (Stages 1–3), but their *form* — archetypes, widget types, one lifecycle, ratcheted audit — is the generic method.
