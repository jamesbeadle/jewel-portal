# The Refactoring Playbook

A staged process for bringing any repository to the standard defined in the coding rules at the top of the root `CLAUDE.md`. The stages run in order; each produces an artefact the next stage consumes. The audit scripts in `audit/` make every stage measurable, and the gate in `audit/gate.py` makes progress irreversible.

The premise: a refactor without a feature freeze is a treadmill, and a refactor without measurement is a matter of opinion. This playbook fixes both. Draw the line, snapshot the numbers, and only ever let them improve.

**The engine, in one sentence: shrink first, abstract second, let patterns arrive third.** On the backend, big files are broken into functions each with one purpose; in the UI, big pages are broken into components each with one purpose. Abstraction is never attempted against a large unit — it is only once the units are small that common functionality becomes visible, and only once enough commonality has been extracted that design patterns appear, because patterns are exactly the shapes that handle common functionality abstractly. Nothing in the stages below reverses this order: the taxonomy and lifecycle stages give extraction a *direction*, but the abstractions themselves are discovered from the shrunken code, never imposed on the large. Once a pattern has formed it is a prediction, and holding the rest of the codebase to it is not imposing it — that is Pass 4 of the round.

Stages 0 to 3 are done once per repository. Stage 4 is the **round** — the unit of work run when the person says *Refactor the repo*, and every N commits on the default branch (see `.claude/skills/refactor-round/SKILL.md`; `deploys_since_baseline.sh` says when). Stages 5 to 8 are rounds of a different shape, taken when the round has made them visible. Stage 9 never ends.

Two things are written down for every repository, and both are measured rather than remembered. The **code quality score** is one percentage made from every figure the audit takes, shown in a box at the bottom of the repository's `README.md` with its breakdown beneath it. The **refactoring plan**, `tools/refactor/refactor-plan.md`, is the steps a refactor of this repository follows, in order, from the same measurement — what a round goes off. *Run the code quality check* writes both (`.claude/skills/code-quality-check/SKILL.md`); a round ends by writing both again.

## Stage 0 — Baseline

Run the audit and commit the result as the baseline the gate ratchets against.

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
cp tools/refactor/audit-output/audit.json tools/refactor/baseline.json
```

`audit-report.md` is the human summary, led by the code quality score and its breakdown; `audit.json` holds every offender list. From this commit onwards the `end-of-day` script runs the audit and `gate.py` fails the day that made a ratcheted figure worse, so it is fixed while it is small. Refactoring can now proceed in any order without the codebase regressing behind it. When a stage improves the numbers, re-copy `audit.json` over `baseline.json` to lock in the gain. The bootstrap does Stage 0 for a repository that has no baseline yet.

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
2. **Load** — data arrives through one gate; the widget never invents its own loading flag.
3. **Render states** — loading, empty, error, ready. Every widget renders all four; the empty and error states are designed, not accidental blank space.
4. **Events out** — the widget raises named events; it never reaches back into its parent or navigates on its parent's behalf.

Also fix the **transition language**: one duration/easing token set, one skeleton/spinner idiom, one toast idiom — named tokens in the theme, never inline values. The artefact is the contract written down (`docs/refactor/component-lifecycle.md`) plus the token file. Every widget extracted in Stage 4 must satisfy it, which is what makes the widgets generic and consistent rather than merely smaller.

## Stage 4 — The round: four passes and a sweep

The mechanical heart of the refactor. A round does not start by reading the codebase to decide what to do: the plan has already been measured (`python3 -m tools.refactor.audit.plan .` writes `tools/refactor/refactor-plan.md`), and the round takes the next steps from the top of it. The passes run in this order because each makes the next one visible — the engine again: shrink, then abstract, then let the patterns arrive.

**Pass 1 — Widget adoption.** The site definition (`tools/refactor/site-definition.md`, written from the views themselves by *Run the widget identification* and by every quality check) says what a user sees at each route in the widget notation, and marks every place a view writes by hand the markup one of the catalogue widgets exists to own — a `<table>` outside `RecordsTable`, an `<input>` outside `FormField`, an `<h2>` where `SectionHeader` should be. File-sized steps, the fullest first, each adopting one widget in a few files: adopt it everywhere its markup is written by hand, and where the widget the finding names is not built yet, build it once from the best hand-rolled copy and then adopt it. This is Stage 2's taxonomy made mechanical — the same kind of data uses the same widget everywhere — and it is measured: the "markup written by hand where a widget should be" figure ratchets, so a view cannot slip back to a raw table once the widget owns it.

**Pass 2 — Component breakout.** A long view is several components that have not been separated yet. For each view over the limit, worst first, the plan names the blocks of markup that are component-sized, their line ranges, and the functions that are used nowhere but inside each block. Take a block, name it for its one purpose, and move it into a component *together with the functions that belong to it* — a component that leaves its functions behind in the parent is only half extracted, and the parent stays long. Hook it up through the framework's own mechanism and nothing else: parameters in, named events out (Stage 3's lifecycle), no reaching back into the parent, no shared mutable object passed down to avoid deciding what the component needs. Where the block is a widget in the Stage 2 taxonomy it becomes, or joins, the shared component for that widget type; where it is specific to the page it becomes a named component beside the page. The view is done when it is under the limit and reads as a short composition of named components.

**Pass 3 — Utility function identification.** Every function left in the target files, and every function the plan lists, answers three questions, and its existence is justified by the answers:

1. *Is this the right home?* A function that another component or file in the same design pattern could use — or already does: the plan lists the functions other files import from a view and the functions whose body is repeated word for word in more than one file; a name that merely recurs, like each form's own `onSubmit`, is not one function and is left where it is, as is a short handler the code shows the framework owns — bound from its own markup, or invoked by an attribute or an override; where the evidence does not settle it the plan asks for the judgement and the answer is recorded in `frameworkHandlers.names` so it is made once — is a utility. It moves to a named, focused module (never `utils` or `helpers`), is abstracted only as far as that module's purpose needs, and every copy is replaced by the one.
2. *Is it masking a bad implementation of something the framework should be doing?* A function that tracks loading by hand, re-implements binding, routing, validation, formatting or state the framework provides, or exists to work around the way the view was wired, is removed by doing it the framework's way — not moved.
3. *Would a reader expect this function in the standard implementation of this kind of view?* If not, and it is neither a utility nor a named step of its caller, it is an accident: inline it, replace it, or delete it. A function nothing calls is deleted once every caller across the repository is confirmed gone.

**Pass 4 — Design pattern identification and modification.** The backend and API files become understandable and predictable units by reading the patterns that are already there and then holding the whole codebase to them. The audit reads the patterns off the file names — a file name is a subject and a role, `CreateProjectHandler` is the `Handler` of `CreateProject` — and when nearly every subject with one role also has another (*every handler has a validator*), that is a pattern, and the pattern is a prediction: the subjects that lack the file are listed, by the path the file would have. Work backwards from the prediction. The job that file would do is being done somewhere — inline in the handler, in an endpoint, in a catch-all service — so find that code and move it to where the pattern says it lives, pre-emptively, before it is next needed. A subject that truly has no such job goes in `rules.json` under `designPatterns.acceptedGaps`, which is the pattern's documented exception. Then the entities: an entity's properties are its complexity, and across a consistent codebase the number of files named for an entity keeps a steady ratio to it. An entity with far fewer files than its complexity predicts has its work piled into too few; one with far more has a pattern being repeated by hand. Then the remaining long backend files, worst first, divided into the units their pattern names. Name a pattern in `docs/refactor/design-patterns.md` the first time it is relied on.

**The sweep to zero.** What the passes did not already remove: else blocks, conditions that do not read as a sentence, comparisons to raw literals, accessor names that want to be a property, magic values, comments, member chains deeper than one hop, deep indentation, long functions — the lowest-scoring element first. The standard figures are not independent targets: the worst file cannot keep falling while files remain over the limit, and in the end both reach zero together. What overflows from one figure into another while that happens — the file count rising as one long file becomes eight short ones — is the round's to recognise and put in order, not the audit's.

Every step, whatever the pass: the code moves first and is renamed second, never rewritten in the same step; behaviour does not change — same rendered output, same events, same refusals, the repository's checks still green; the step is re-measured (`run_audit --fast`) and committed before the next begins. A step that will not come right in two attempts is put back and named in the report rather than forced. A file refuses to shrink below the limit only when the taxonomy is missing a widget type, the page is two pages, or the pattern is missing a role — go up a stage, never force it.

## Stage 5 — DRY consolidation

Take the `duplication` hotspot list and merge each clone family into one named concept. Judge each family with the rules' own test: is this the same *concept*, or coincidentally similar code? Same concept → extract under a name that captures the concept; coincidence → leave it. Typical families this audit surfaces: endpoint gate boilerplate (resolve user → authorise → validate → handle, repeated per endpoint — extract a gate runner so an endpoint states only what varies), document renderers sharing layout scaffolding, and near-identical page sections that Stage 4 already turned into widgets. Re-run the audit; the duplication percentage ratchets down.

This is the "abstract second" half of the engine, and it is where design patterns are allowed to appear — not chosen from a catalogue, but recognised: once enough common functionality has been pulled into named units, the structures that handle commonality abstractly (a factory, a strategy, a pipeline) surface on their own. Name a pattern only after it has already formed, in `docs/refactor/design-patterns.md`. A pattern is discovered once and predicted ever after: once it has formed, Pass 4 of the round holds every subject to it.

## Stage 6 — Backend articulation check

Verify the CQRS chain end to end: every user story from Stage 1 has exactly one command or query; every endpoint passes through all three gates — authentication (signed-in user resolved), **authorisation** (an explicit rule allowing this user this operation, including on queries), validation — before any domain logic. Grep-level checks make this concrete: count endpoints lacking an authorisation gate and drive that number to zero or to a documented "deliberately public" list. DTOs derive from what handlers need; delete any that nothing demands.

## Stage 7 — Security sweep

Stack-specific, checklist-driven, in its own session with fresh eyes. Whatever the stack, the list has the same headings: the authentication boundary and route rules; every endpoint's gate order (Stage 6 feeds this); secrets — nothing in source or infrastructure scripts, everything in the platform's secret store; third-party token handling — storage, refresh, scope minimisation; database access through the data layer only, never raw strings assembled from input; upload and attachment handling; infrastructure scripts idempotent and free of embedded credentials; dependency and framework patch level. Each finding becomes a fix commit or an accepted-risk note in `docs/refactor/security-review.md`.

## Stage 8 — Design conformance

With the widget set consolidated, compare against the source designs (design-tool exports of components and tokens). One pass per widget type, not per page — that is the payoff of Stage 2: fixing the table component's spacing fixes every table in the product. Extract the design tokens (colour, type scale, spacing, radii, elevation, motion) into the theme; the `magicValues` audit figures (inline hex colours, inline style attributes) ratchet to zero. Then one pass per page archetype for layout and flow: entry, load choreography, transitions between states, exits.

## Stage 9 — Hold the line

The gate runs at the end of every working day. New code obeys the rules from birth; the ratchet means the numbers only travel one way, and the score in the README says where they have got to. The deploy count keeps Stage 4 coming round. Revisit the baseline quarterly: tighten any figure that has headroom (for instance, lower `maxFunctionLines` once the worst offenders are gone).

## Adapting this to another repository

Everything repository-specific lives in `rules.json`: source globs, page/component globs, widget markers, style-token globs, the entity globs the design pattern check reads, and any weight of the score the repository wants to set for itself. Point them at the new codebase, run Stage 0, and the same nine stages apply. The taxonomy and lifecycle contracts are re-derived per product (Stages 1–3), but their *form* — archetypes, widget types, one lifecycle, ratcheted audit — is the generic method.
