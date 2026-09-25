<!-- project-process:begin -->
<!-- project-process kit v1.12.0 — replaced whole by bootstrap.sh; edit the kit, and write this repository's own instructions in PROJECT.md -->

# How We Work

This file is installed by the project-process kit and replaced whole every time its bootstrap runs; edit the kit, not this copy. Anything written here is lost on the next run.

**This repository's own instructions are in `PROJECT.md`** — its conventions, commands, domain notes and any standing tasks or prompts that belong to this project alone. Read it before starting work, after these rules: @PROJECT.md. The kit creates it once and never rewrites it, so the kit and the project change independently and neither can break the other. A project-specific instruction always goes there, never here. Where `PROJECT.md` and the kit disagree about this project, `PROJECT.md` wins, except that nothing in it lifts the branch rule below.

## The work is logged in Your Business Today

Every piece of work on this repository is done on a task in Your Business Today (YBT), reached through the YBT connector. YBT's `get_current_context` carries the working doctrine and is read first in every session; `describe_action` carries the doctrine of each action. In short:

1. **Start by reading what is new.** Call `read_latest_messages`. Anything addressed to the person you are working with, bring to them; post their answer on the same goal or task.
2. **Find the task before touching anything.** Call `find_tasks` on the project for the matter at hand and work on the task you find. Raise one only when nothing matches: a bug as `FIX: <what is wrong>`, a feature by its user story. Mark it in progress when the work starts — and start it on a branch named for it (next section), never on the default branch. Before the first change, `read_task` and open every attachment it lists with `read_task_attachment`: a screenshot, a spec, an export or a client's email on the task is part of the ask, not background.
3. **Leave a work log when the work stops.** One message on the task's conversation: what changed, which files or records, decisions taken and why, what is left, and the branch and pull request the work is on. Then mark it done if it is done, or leave it in progress and say what is next.
4. **Files that the work depends on go on the task, not in the chat.** When a task is raised or updated with information someone will need to do it — a screenshot of the fault, the document the story comes from, a data export, a design — attach it with `attach_file_to_task` once the task exists (creating or updating a task never carries files itself). Give `sourceUrl` for anything already at a public address, up to 25 MB; give `contentBase64` only for a small local file, under 3 MB — larger local files need a public link, or the person attaches them in YBT. Name the file for what it is (`invoice-rounding-screenshot.png`, not `image1.png`) and say in the task's details or message what it shows. Never attach secrets, credentials or personal data the task does not need. Do not guess at what a file said: if the work needs a file that is not on the task, ask for it.
5. **A question for another member goes on the task or goal,** naming them. Their Claude brings it to them through `read_latest_messages` and posts the answer back. Never relay through chat apps.

## The work happens on a branch, never on the default branch

Nothing is committed to the default branch (`main`, or whatever `origin/HEAD` points at) by a Claude — not a fix, not a feature, not a refactor round, not a one-line change. The default branch only ever moves by a pull request that a person has reviewed and merged. Every session that is about to change a file does this, in order:

1. **Start from a fresh default branch.** `git fetch origin` and `git switch <default>` followed by `git pull --ff-only`; if the working tree already holds uncommitted changes, stop and ask the person what they belong to before going further.
2. **Branch before the first change.** `git switch -c fix/<slug>` for a `FIX:` task, `git switch -c feature/<slug>` for a story. The slug is the task's title in lower-case words joined by hyphens, with noise words dropped: `FIX: invoice total is rounded up` becomes `fix/invoice-total-rounding`; the story *A quantity surveyor sees the weekly cashflow grid* becomes `feature/weekly-cashflow-grid`. If a branch for that task already exists (`git branch --list 'fix/*' 'feature/*'`, and `origin`), switch to it and continue there rather than starting another.
3. **Commit on the branch as the work goes**, one commit per verified step, the message a sentence about the domain (`Round the invoice total once, at the line total`), never a list of files. Run the repository's checks before each commit.
4. **Push the branch and open the pull request when the work stops** — done or not. `git push -u origin <branch>`, then a pull request into the default branch whose title is the task's title (`FIX: invoice total is rounded up`) and whose body is the work log in short: what changed, why, what is left. With the GitHub CLI: `gh pr create --base <default> --title "<task title>" --body "<the log>"`; without it, push and give the person the compare link GitHub prints. A pull request that already exists for the branch is updated by the push — do not open a second one.
5. **The person merges.** Never merge, rebase onto or force-push the default branch, and never `git switch <default>` to commit there. When the task's work needs something already on another open branch, say so on the task rather than merging branches yourself.

The repository carries a guard as well as this rule: `tools/branch_guard/`, installed as a Claude Code `PreToolUse` hook in `.claude/settings.json`, refuses any `git commit`, `git merge` or `git push` that would land on the default branch and says why. When it refuses, do what it says — branch — rather than looking for a way round it.

A refactor round follows the same shape on a `refactor/round-N` branch, and a code quality check on a `quality/check-<date>` branch (their skills say so); they are the two branches that are neither a fix nor a feature. A process that only reads — the widget identification, the input validation check, the audit and the gate, the deploy count — writes nothing but its own output, so it needs no branch at all and commits nothing (next section).

## Every process is asked for by name

The kit offers the processes below and no others, and each one is also a tool on the repository's `project-process` server (`.mcp.json`, served by `tools/mcp/server.py`): `run_code_quality_check`, `run_widget_identification`, `run_input_validation_check`, `run_refactor_round`, `run_widget_design`, `run_end_of_day`, `read_code_quality_score`, `read_refactor_round_due`. Calling a tool runs that process and no other — there is no nearest match for it to fall into — and a name the kit does not carry is refused with the list of the ones it does. Each is also run by the sentences beside it — say one of them, in these words or close to them, and the session does what the named skill in `.claude/skills/` says: it reads that `SKILL.md` and follows it; it does not improvise the process. The sentences are the names, and a word they share is not a name: three of them say *widget*, so the whole sentence is what is matched, never the word.

- **"Run the code quality check"** / **"score the repo"** → `code-quality-check`. Measures the repository, and on a `quality/check-<date>` branch writes a box at the bottom of `README.md` holding the code quality score (one percentage), with three things expandable beneath it: how the score is made up, the count of every file in the repository split by area, and the refactoring plan. It also writes `tools/refactor/refactor-plan.md` — the steps a refactor of this repository follows, in order — and refreshes `tools/refactor/site-definition.md` on the way. It changes no source code, and it ends as a pull request because it changes the README. One command does the measuring: `python3 -m tools.refactor.audit.quality_check .`
- **"Run the widget identification"** / **"identify the widgets"** / **"where are the widgets written by hand"** / **"show me the site definition"** → `widget-identification`. Reads every view and writes `tools/refactor/site-definition.md` alone: what a user sees at each route, in the widget notation from the views themselves, with every place a view writes by hand the markup a catalogue widget should own, and the table of those places widget by widget. Read-only: no branch, no commit, no pull request, no score box, no plan. It identifies the widgets; it never adopts them.
- **"Run the input validation check"** / **"check the inputs against the schema"** / **"can anything write data that does not fit its column"** / **"is the MCP server validating its inputs"** → `input-validation-check`. An ad hoc reading, run when asked: sets every entry point that writes — API routes, server actions, command handlers, MCP tool and action handlers — against the column types and constraints it writes to, and writes `tools/refactor/input-validation.md`, each column guarded, mismatched, unguarded, form only or repaired, with whether the form mirrors it. Read-only: no branch, no commit, no pull request, no score box. It finds the gaps; fixing them changes behaviour, so it is a `fix/` branch the person asks for, never a refactor round.
- **"Refactor the repo"** / **"run a refactor round"** → `refactor-round`. One measured round on a `refactor/round-N` branch that takes the next steps from the top of `tools/refactor/refactor-plan.md`, in order: widget adoption (replacing the hand-rolled markup the identification found with the widget), then component breakout, then utility function identification, then design pattern identification and modification, then the sweep to zero. Behaviour never changes. It ends by running the code quality check, so the pull request carries the new score and the plan for the round after. Also due, unasked, when `tools/refactor/deploys_since_baseline.sh` says so, or when `REFACTOR: round N` is open in Your Business Today.
- **"Run end of day"** / **"close the day"** → `end-of-day`. The round if one is due, the check that the connector covers what changed today, the plain-English summary on the day's tasks, and the day's branch pushed and its pull request open.
- **"Set up the widget designs"** / **"Extract the brand from <references>"** / **"Extract the design for <Widget> from <images>"** / **"Check the site against the brand"** / **"Check the widgets against their designs"** → `widget-design`. Optional, for a site big enough to have a widget catalogue. The brand sheet (`docs/design/brand.md`: every colour, type, spacing, radius, elevation and motion token with its role) and a design sheet per widget that has one (`docs/design/widgets/<Widget>.md`, extracted from images or any reference, in token names) are the design; the index is `docs/design/widgets.json`. A question — *are the brand colours used appropriately*, *does RecordsTable match its design* — gets the reading and changes nothing; a **Check** fixes the widget, on a branch, as a pull request. Before building or changing a catalogue widget, or a view that needs a look no widget gives, read the index: a widget with a sheet is built to the sheet, and everything else to the brand sheet. The audit reports when each check last ran; it never runs them, because a design is a judgement.

Two readings are commands rather than skills, and a person asks for them as questions. **"What is the code quality score?"** / **"run the audit"** / **"does the gate pass?"** is `python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast` then `python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json`: the score is printed and the gate says which ratcheted figures are worse than the baseline — read-only, no branch, and not the code quality check, which publishes. **"Is a refactor round due?"** is `tools/refactor/deploys_since_baseline.sh`: the commits on the default branch since the baseline was last committed, against the rhythm (ten unless the project says otherwise).

A process that changes a tracked file — the check's README box, a round's code, a design check's widget — runs on its own branch and ends as a pull request for the person to merge, never as a commit on the default branch. A process that only reads — the widget identification, the input validation check, the audit and the gate, the deploy count — needs no branch and commits nothing; asking for one of these never produces a pull request.

**When no name matches, say so.** A request that names a process this list does not carry — *the widget pass*, *the site survey*, *the cleanup*, *the process I added* — is not routed to the nearest process that shares a word with it. Say plainly that the kit has no process of that name, name the nearest entries by the sentences above with one line each on what they write and whether they change code, and ask which is meant. Put the choice in the kit's words, never in words of your own: a session once asked *"the widget pass (refactor steps 227–232), or the quality check only?"* when the person wanted the read-only site definition, and the option carrying the word *widget* was a five-commit refactor round — the question inherited the very gap it should have named. Guesses are not the menu; the list above is. When the person's request could be one of two entries here, ask with both sentences quoted; when it is plainly one, run it.

Behind all of them the repository carries `tools/refactor/` — an audit that measures the code against the rules below, a score made from every figure it takes, and a gate that fails when a ratcheted figure is worse than the committed baseline. Nothing runs on GitHub: the measuring is run by the skills. Your Business Today can also raise `REFACTOR: round N` on the project as the reminder when its own deploy count reaches N. Refactor rounds never add behaviour or change the schema.

The score is the measure of the rules below, so new code is written to score 100%: every rule in *How I Write Code* is one the audit counts. Before saying any piece of work is done, run the fast reading and the gate — `python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast` then `python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json` — and fix what the work introduced.

## The rules travel with the repository

The coding rules that follow are the whole standard. They live here, in the repository, because a machine-level `~/.claude/CLAUDE.md` does not reach cloud sessions or anyone else's machine. Repository-specific conventions belong in `PROJECT.md`; decisions worth keeping belong in `docs/`.

# How I Write Code

Read this first. Everything below is how I think, not just what I want. If you understand the principle, the rules follow naturally. If you only follow the rules without the principle, you'll satisfy the letter and miss the point.

## The Core Idea

**Code is prose.** A file should read like a story about the domain. When someone reads it, they should understand what is happening without running it, without comments, and without holding much in their head. The language of the code — names, structure, flow — is how the domain reveals itself.

This is the lens. Every other rule below is a consequence of it.

**The diagnostic test:** if a line of code doesn't make sense as a sentence, something is wrong. Not with the line — with the structure around it. Bad code reads badly because it reflects a flawed understanding of the problem. When you feel friction reading, stop and restructure. Don't paper over it with a comment.

**Language is also how the code is judged.** We work with language models, and a language model reads code the way it reads anything else: as language. Code that reads as articulate prose is code a model can extend, analyse and refactor correctly; code that doesn't is where it guesses. So legibility is not a courtesy to the next reader — it is the property that makes the codebase workable at all, and it is measured: the repository's code quality score counts every rule below.

### A line reads as a sentence

`if (is(getApple(1).colour == "RED"))` is three ideas tangled into one line: fetching an apple, reading its colour, and knowing what red is. `if (apple.colour == Colours.red)` is a sentence. Get there by naming things before the statement that uses them — `var apple = getApple(1);` on the line above is not waste, it is the subject of the sentence being introduced before the verb. An extra local that makes the next line legible is always worth its line.

- **No calls tangled inside calls in a condition.** A condition states a fact about named things. Do the fetching and computing above it, give the results names, and let the condition read.
- **Never compare against a raw literal.** `== "RED"`, `=== 'paid'`, `> 5` say what the value is, not what it means. Compare against a named value: `Colours.red`, `InvoiceStatus.paid`, `Limits.maximumAttempts`.
- **Never read a member off the result of a call in the same breath.** `getApple(1).colour` hides the apple. Name the apple.
- **If the code is written with the right prose, comments are not needed anywhere.** The need for a comment is the proof that the prose has failed.

## How to Approach a Codebase

Before writing any code, the work has to flow through five stages in order. Each stage is derived from the one above it. Nothing exists in a lower stage that isn't demanded by a higher one.

**1. User stories.** A software project is the sum of its user stories. Every story has the shape *as X user, I want Y feature, for Z benefit*. If a feature can't be expressed this way, it shouldn't exist yet. The full set of stories defines the scope of the project — nothing more, nothing less.

**2. User experience through UI.** Each story is delivered through a view. Imagine the wireframe: what screens, what components, what flows. A table here, a form there, a modal for confirmation, a graph for the summary. The UI is the concrete realisation of the story. Industry-standard, high-quality UX — known patterns, known components, no invention for invention's sake.

**3. Site map.** Stories don't live in isolation and neither do their views. Map every story to a view, then map the views to each other — what links to what, what nests inside what, where the user enters, where they go next. A projects dashboard isn't a single-story view; it's a hub that fans out to every project-level feature. A project detail view serves dozens of stories at once. The site map consolidates the per-story wireframes into one coherent application.

What this reveals:
- **Shared views.** The same view appears in many stories. Design it once with full knowledge of every demand placed on it, not as a side-effect of one story at a time.
- **The navigation hierarchy.** Dashboard → project → task → comment isn't decoration, it's the user's mental model of the domain. If the navigation feels awkward, the domain hierarchy is wrong — fix the hierarchy, not the navigation.
- **Speculative views.** Any view not reached by a user story shouldn't exist. If you find one, either it's missing a story (go back to stage 1) or it's not needed (delete it).
- **Missing entry points.** Any story whose view isn't reachable through navigation has a gap — the user can never trigger it. Surface the gap before building anything.

**4. Data structure.** With a consolidated site map you now know every view and everything each view demands. The UI reveals the domain. A table's columns *are* an entity's properties. A form's fields *are* the inputs that entity accepts. A flag in the UI *is* a property on the model. Rarely does a domain property exist that doesn't surface somewhere in the UI — and when it does, it's derived from properties that do. Build the entity diagram from what the *unified* set of views demands across the whole site map. Single source of truth. Derive what's derivable; store what isn't.

**5. Backend.** By this point the hard decisions are made. Entities are known. Operations on them are known (because the UI demands them). The backend becomes a translation layer between the data structure and the UI, not a design problem. It articulates cleanly through CQRS — see below.

### The backend articulates through CQRS

CQRS is the default skeleton for the backend, chosen for the same reason everything else here is chosen: language. Every command and every query is a named intention that reads as a sentence — `CreateProject`, `ArchiveProject`, `GetProjectsForUser`. A command or query *is* a user story made executable. This maps the backend one-to-one onto the stories from stage 1, which is exactly the articulation the rest of these rules demand.

The flow within the backend:

1. **Every user story becomes a command or a query.** Commands change state; queries read it. If a story doesn't map cleanly to one or the other, the story isn't fully understood — go back up the chain. The full set of commands and queries should account for every story, nothing more.
2. **Implement the entry points with their gates.** Each command/query entry point handles authorisation, authentication, and validation *first* — before any domain logic runs. These are the gates the request passes through, and they read as exactly that.
3. **Identify the design pattern from expected usage.** Only now, knowing what the operations are and how heavily each will be used, does a scalable pattern reveal itself. This is emergent, not imposed — the same rule as everywhere else. Don't pick a pattern from a catalogue; let expected usage show you the shape.
4. **Wire the pattern implementations to the entry points.** The entry points (with their gates) delegate to the implementations the pattern produced.
5. **Derive types and DTOs from the implementations.** The data-transfer objects fall out of what the implementations actually need — they're discovered, not designed upfront. This is the same principle as entities falling out of the UI: the lower artefact is shaped by the demand above it, never speculated.

The discipline is that CQRS is the *framework for articulation*, not a constraint to fight. The design patterns that emerge inside it (step 3) are still discovered, never forced. CQRS gives the backend its language; the patterns give it its structure; the rules below give it its prose.

### Working in reverse

When approaching an *existing* codebase, walk the chain backwards. Infer the user stories from the UI. Find the views and how they connect — that's the site map. Trace them to the entities. Then look at the backend. If any layer doesn't trace cleanly to the one above, that's where the codebase has drifted from its purpose — and that's usually where the bugs and confusion live.

### Ambiguity means you don't understand the domain

If a user story isn't clear, you haven't understood the domain. Don't proceed. Don't fill the gap with a guess. Don't ask "how should I implement this" — go back and ask "what does this user actually need, and why". Every domain *can* be visualised once it's understood. If you can't picture the UI, the domain isn't yet clear. Resolve that before doing anything else.

The same applies further down the chain. If the UI is unclear, the story isn't fully understood. If the site map is unclear, the views aren't fully understood. If the entity is unclear, the site map isn't fully understood. Always go *up* a layer to resolve confusion, never sideways or downwards.

### What this means in practice

- Don't start coding because a request sounds clear. Ask which user story it serves and what the UI looks like.
- Don't invent entities or properties that no UI demands. If you find yourself adding a field "just in case", stop — it's speculation, and speculation is the enemy of clean code.
- Don't design the backend first and bend the UI to fit. The UI defines the shape of the data, not the other way around.
- If asked to add something that doesn't trace back to a user story, flag it. It might be valid (infrastructure, tooling, refactor) but it deserves to be named as such, not smuggled in as a feature.

## Naming

Names are the most important thing in the codebase. Get them right and most other problems disappear.

- **Use the full word.** `pageNumber`, not `page`. `buffer`, not `buf`. `request`, not `req`. No abbreviations, ever, except for genuinely universal ones like `i`/`j` in tight loops or `id`.
- **A name should be exactly what the thing is.** If you can't name it precisely, you don't understand it yet — stop and think before continuing.
- **Booleans are questions.** `isAdmin`, `hasAccess`, `shouldRetry`, `canEdit`. Never `admin` (ambiguous — is it a flag or an ID?), never `access`, never `retry`. The name must make the call site read like English: `if (user.isAdmin)`, not `if (user.admin)`.
- **Infer the type from the name.** A reader should know roughly what they're dealing with from the variable alone. `users` is a collection. `user` is one. `userCount` is a number. `getUserById` returns a user.
- **If a name needs a comment to clarify it, the name is wrong.** Rename instead of commenting.
- **When a name feels awkward, the abstraction is probably wrong.** Awkward names are a signal, not a problem to work around.
- **A function name that glues nouns together wants to be an object.** `getAppleColour()` is a lazy output: it exists because nobody modelled an apple with a colour. `apple.colour` is the same fact expressed by a proper object with properties, and unlike the glued function it extends — the next property is a property, not another function. Accessor functions named for a type and one of its properties (`getInvoiceStatus`, `getProjectOwnerName`) are a modelling failure that compounds over time; model the object. A name that finds a thing (`getUserById`, `getProjectsForUser`) is a lookup, not a glued accessor, and is fine.
- **A long function name is a missing type.** More than five words, or more than forty characters, means the name is carrying context that belongs to a class or module: `calculateInvoiceLineTotalIncludingTax` wants to be `InvoiceLine.totalIncludingTax`.

## No Comments

If the code needs a comment to be understood, the code has failed to articulate the domain. Fix the code instead.

Exceptions, narrow:
- Public API documentation (docstrings on exported functions/types) where tooling consumes them.
- Genuinely non-obvious *why* — e.g. "this works around a bug in library X version Y" or "this ordering matters for legal compliance reasons". The *what* and *how* should always be in the code itself.

Never write comments that restate what the code does. Never leave `// TODO` comments without an owner and a reason.

## Magic Values

Never inline a raw literal that has meaning beyond its value.

- `circumference * 3.142` is wrong. `circumference * MathematicalConstants.Pi` is right.
- Hex colour values inline are wrong. Use a theme/config (e.g. Tailwind tokens, a `colours` module).
- Repeated string literals that represent the same concept get a constant.
- Group constants meaningfully (`MathematicalConstants`, `HttpStatus`, `ErrorCodes`) so the call site reads as a sentence: `if (response.status === HttpStatus.NotFound)`.

The test: can a reader tell what the value *means*, not just what it is? If not, name it.

## File Size

**Hard target: no file longer than 100 lines.** This is a forcing function, not an aesthetic. Long files are a symptom — of missing abstractions, of missing components, of not using the framework properly, of conflating concerns.

When a file grows past 100 lines, the question is never "how do I make this fit" — it's "what have I failed to extract?" Almost always there is a helper, a sub-component, a utility, or a separate concern hiding inside.

Exceptions, real but rare:
- Framework-imposed "god files" (e.g. a routing manifest, a barrel export, a generated types file).
- A coherent set of constants or types where splitting would scatter related things.

If you're about to exceed 100 lines, default to splitting. Justify keeping it long, not splitting it.

The figures that measure this are not independent of each other. You cannot keep reducing the longest file and still have files over 100 lines: the number of files over the limit and the length of the worst of them both go to zero, together, and everything else the audit counts goes with them. Dividing a long file pushes its contents somewhere — more files, more functions, a new near-duplicate — and recognising the patterns and duplications in that overflow, and putting them in order, is the work; it is not done when the file is merely short.

## Components Own Their Functions

A long frontend file is several components that have not been separated yet. Find the chunks of markup that are clearly one thing — a table, a form, a dialog, a panel, a row — and break each out into a component **with the functions that belong to it**. A function used only inside a chunk moves with the chunk, and so does the state only it touches; a component that leaves its functions behind in the parent is half extracted, and the parent stays long.

- Hook the component up through the framework's own mechanism and nothing else: typed parameters in, named events out. It never reaches back into its parent, and it is never handed a grab-bag object to avoid deciding what it needs.
- It must work exactly as before. Breaking out a component is a move, not a rewrite.
- A component that needs a long list of parameters was cut at the wrong seam. Take the larger chunk around it or the smaller ones inside it.

## Every Function Has a Home and a Reason

Ask two questions of every function, when writing it and when reading it:

**Is this the right home for it?** If another component or file within the same design pattern could use the function, it is a utility, and it lives in a named, focused module — abstracted as far as that module's purpose requires and no further — where it can be reused. The same function declared in two files is one utility that has not been given its home yet. Same means the same body or the same concept, never just the same name: every form having its own `onSubmit` is each component doing its own job, and folding those together would be abstraction for its own sake. This is not premature abstraction: "just in case" is speculation about a user nobody can name; a utility is justified when the design pattern itself names who else will use it.

**Should it exist at all?** A function's existence has to be justified. Would a reader expect this function in the standard implementation of this kind of view, handler or module? If not, it is usually masking a bad implementation of something the framework should be handling — hand-rolled loading flags, binding, routing, validation, formatting, state synchronisation. Remove it by doing the thing the framework's way, not by tidying the workaround.

**Nothing is left uncalled.** A component or function that nothing calls is deleted, not kept for later. Version control is where old code lives.

## Inputs Are Checked Where They Enter

A value that will be stored is checked against what its store accepts, at every door it can come in by, before it is written. The store's own shape is the contract — a column's type, length, range, precision and scale, nullability, format and allowed values — and the check reads that contract rather than inventing a looser or a different one.

- **The service is the guard.** Every entry point that leads to a write — an API route, a server action, a command handler, a message consumer, an import, an MCP tool or action handler — checks every value itself, at its gate, before any domain logic runs. Anything can call it without passing through a form, so nothing in front of it counts toward its safety. An agent calling an MCP tool never sees the frontend; the handler has to be enough on its own.
- **The form mirrors it for the user.** The same limits sit on the input the user types into — length, range, step, required, pattern, the list of choices — so the problem is shown before submitting. That is usability, never the guard: a form check with no service check behind it is a finding.
- **Reject, never repair.** A value that does not fit is refused with an error naming the field and the limit. It is never truncated, rounded, clamped or coerced quietly, and the store is never the first thing to complain.
- **The limits are stated once.** They are derived from the schema, or declared once beside it, and the service and the form both read that statement. The same `200` typed into a validator, a form and a migration is three limits waiting to drift apart.

## Function Size and Shape

**A long function is a contradiction in terms.** The entire point of a function is to break long content into small, named, understandable pieces — so a massive function is a function refusing to do its own job. There is no real reason for one to exist. **Soft limit: ~30 lines.** As with files, when a function approaches the limit the question is never "how do I make this fit" — it's "what have I failed to extract?" Almost always there's a smaller function, a utility, or a separately named step hiding inside. Extract until each function does one thing and its name says exactly what that thing is — then the parent function becomes a short sequence of named steps that reads like prose, which is the whole goal.

- **Functions should be short.** If a function is long, it's doing too much. The extracted pieces don't need to be reused anywhere else to justify existing — a function whose only purpose is to give a name to one step of its caller has already earned its place.
- **One hop per line (Law of Demeter, informally).** A line may take one step into an object and no further. `apple.colour` is the whole allowance; `apple.colour.hexCode` is already too far, and `order.customer.address.postcode.format()` is a walk through three objects' insides to reach a fourth. The depth is the measure, not the dot count: a fluent pipeline of calls — `invoices.Where(…).Select(…).ToList()`, `items.filter(…).map(…)` — hands back something new at each step and is not a walk into anything, while `invoice.project.client.name` is, wherever it appears, including inside a lambda or in markup.

  **The fix is to model the object, not to name the hop.** A chain is evidence that a type is missing a property or a method, so the first move is to give it one and let the caller ask: `apple.colour.hexCode` becomes `Colours.getHexCode(apple.colour)`, and `invoice.project.client.name` wants to be `invoice.clientName`. The caller holds an apple and asks the thing that knows about colours, rather than reaching through the apple to get at it. Only when the type is genuinely not yours to change — a framework object, a third-party model, generated code — does the chain become a named local instead: `var client = invoice.project.client;` above the line, then `client.name`, which is the same rule as naming the apple before the sentence that uses it. The local is the fallback and it is the weaker answer: two short lines satisfy the audit without the model getting any better, so a round that reaches for it everywhere has moved the problem rather than solved it.
- **No arrow code.** Deep indentation is a visual smell — if the code is marching right across the page, the function is doing too much branching. It means a function should already have been called inside that block: the indented body is a named step that was never named. Extract, invert conditions, return early.
- **Idempotent where possible.** A function called twice with the same input should behave the same way. Side effects should be obvious from the name (`saveUser`, not `processUser`).

## Avoid `else`

Big branching blocks destroy the prose-like flow. Most of the time, `else` is avoidable:

- Return early. Guard clauses at the top of a function eliminate the need for `else` in the body.
- Extract the branches into separately named functions.
- Use a lookup, map, or polymorphism when there are many branches.

`else` isn't banned — sometimes the alternative is genuinely worse (longer, more verbose, less clear). But the default is to avoid it. If you find yourself writing `else`, pause and ask whether early return or extraction would read better.

## Don't Repeat Yourself — But Carefully

Duplication is a signal that something wants to be extracted. When you see the same logic twice, ask:
- Is this *actually* the same concept, or coincidentally similar code? (Coincidental duplication is fine — premature abstraction is worse than duplication.)
- If it's the same concept: extract to a helper, utility, or shared module with a name that captures *the concept*, not the mechanics.

**DRY is a smell-detector, not a law.** Don't contort code to eliminate duplication if doing so makes the code less readable.

## When Rules Conflict

The ranking, when forced to choose:

1. **Readability — does it read like prose?**
2. Short, focused functions and files.
3. DRY.
4. Everything else.

If avoiding `else` would require duplicating five lines, duplicate them. If extracting a helper would require a clumsy name, leave the code inline and rename later when the right abstraction reveals itself. Readability wins.

## Design Patterns Emerge, They Aren't Imposed

I don't reach for Gang of Four patterns by name. I follow the rules above, and when a pattern naturally appears — a factory, a strategy, a decorator — great. But I don't force code into a pattern because it has a name.

The right structure is discovered through writing clean prose-like code, not chosen upfront from a catalogue. Don't suggest "let's use the Observer pattern here" — suggest "this part of the code wants to notify other parts when X changes" and let the shape emerge.

### Once a pattern has emerged, it is a prediction

A design pattern turns the backend and the API into understandable, predictable units — and predictable is the point. Once the codebase shows a pattern (every command has a handler and a validator; every entity has its list view, its detail view and its form), the pattern tells you what *should* exist for every other subject. Work backwards from it: predict the file, find the code that is doing that job somewhere else — inline in a handler, in an endpoint, in a catch-all service — and move it to where the pattern says it lives, pre-emptively, rather than waiting until it hurts.

- **New code lands in the pattern's shape from birth.** Adding an entity or an operation means adding the files the pattern predicts for it, named the way its siblings are named. If you are adding a file with no sibling anywhere in the codebase, say so.
- **File counts should be consistent.** An entity's properties are its complexity, and its complexity dictates how many views it needs and how large the files the patterns force on it are. Across a consistent codebase the number of files per entity sits within a rough, acceptable range of that complexity. An entity far below the range has its work piled into too few files; one far above has a pattern being repeated by hand. Either is a finding.
- **An exception is written down.** A subject that truly has no need of a file its pattern predicts is recorded as an accepted gap, not left to look like an oversight, and never satisfied with an empty file.

## Architecture Bias

- **Loose coupling.** Modules should know as little about each other as possible. Prefer composition over inheritance, interfaces over concrete dependencies.
- **Microservice-style thinking even within a monolith.** Each module has a clear responsibility and a small surface. Other modules talk to it through that surface, not its internals.
- **Extensibility through structure, not through configuration.** Adding a feature should mean adding a file or a module, not adding a flag to an existing tangle.

## Anti-Patterns to Avoid

Things I never want to see in code you write for me:

- Abbreviations in identifiers (`usr`, `btn`, `cfg`, `tmp`, `req`, `res`, `ctx` — write them out).
- Booleans without `is`/`has`/`should`/`can` prefix.
- Magic numbers or magic strings inline.
- Functions longer than ~30 lines (soft limit) or files longer than 100 lines (hard limit).
- Deep nesting / arrow code.
- `else` blocks where an early return would do.
- Comments explaining *what* the code does.
- Code duplicated across files when the concept is the same.
- Member chains deeper than one hop (`a.b.c`), and splitting one into a local when the type was yours to model.
- Conditions with calls tangled inside calls (`if (is(getApple(1).colour == "RED"))`), and comparisons against raw literals.
- Accessor functions that glue a type to its property (`getAppleColour()` instead of `apple.colour`), and function names over five words or forty characters.
- Components that leave their functions behind in the parent, or reach back into it.
- Functions that mask something the framework should be doing, and functions or components nothing calls.
- The same function — the same body, not merely the same name — declared in more than one file.
- A subject missing a file its design pattern predicts, or an empty file created to satisfy one.
- An entry point that writes a value without checking it against its column, a check that lives only on the form, and an input quietly truncated or coerced to fit.
- Premature abstraction — extracting "just in case" before the second use exists or the design pattern names it.
- Catch-all utility files (`utils.js`, `helpers.js`) — utilities go in named, focused modules.

## Before You Finish Any Task

Run through this checklist mentally:

1. Does every name say exactly what the thing is?
2. Could a reader understand this file without running it?
3. Is every file under 100 lines, and every function around 30 or fewer?
4. Are there any `else` blocks I could remove with early returns?
5. Are there any inline literals that should be named constants?
6. Did I add any comments? If so, can I rename or restructure instead?
7. Did I introduce duplication? Did I introduce premature abstraction?
8. Does each function do one thing its name describes?
9. Does every condition read as a sentence — nothing fetched or computed inside it, nothing compared to a raw literal?
10. Does every line take at most one hop into an object, and did I model the type rather than reach for a local?
11. Is every function in the right home, and is its existence justified — not a utility stranded in a view, not a workaround for the framework, not uncalled?
12. Does every new file sit where the codebase's design patterns predict it, named as its siblings are, and did I add every file the pattern predicts for what I added?
13. Is every value I write checked against its column at every entry point that writes it — the API, the handler, the MCP tool — rejected rather than repaired, with the form mirroring the same limits?
14. Would the code quality score fall because of this change? Run the fast audit and the gate if the repository carries them.

If any answer is "no" or "I'm not sure", fix it before saying you're done.

## How to Work With Me

- **Match the patterns in the existing codebase** if it follows these rules. If existing code violates them, ask before propagating the violation.
- **Don't add things I didn't ask for** — extra config, extra abstraction, extra files. Minimal change that solves the problem.
- **If you think a rule above is wrong for a specific case, say so explicitly** rather than quietly breaking it. I'd rather have the conversation.
- **When in doubt, choose the boring, readable option** over the clever one.

<!-- project-process:end -->
