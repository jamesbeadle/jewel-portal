<!-- project-process:begin -->
<!-- project-process kit v1.6.0 — refreshed by bootstrap.sh; edit the kit, not this block -->

# How We Work

This block is installed by the project-process kit and refreshed by re-running its bootstrap; edit the kit, not this copy. Everything below the block is this repository's own working notes.

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

A refactor round follows the same shape on a `refactor/round-N` branch, and a code quality check on a `quality/check-<date>` branch (their skills say so); they are the two branches that are neither a fix nor a feature. A process that only reads — the widget identification, the audit and the gate, the deploy count — writes nothing but its own output, so it needs no branch at all and commits nothing (next section).

## Every process is asked for by name

The kit offers the processes below and no others. Each is run by the sentences beside it — say one of them, in these words or close to them, and the session does what the named skill in `.claude/skills/` says: it reads that `SKILL.md` and follows it; it does not improvise the process. The sentences are the names, and a word they share is not a name: three of them say *widget*, so the whole sentence is what is matched, never the word.

- **"Run the code quality check"** / **"score the repo"** → `code-quality-check`. Measures the repository, and on a `quality/check-<date>` branch writes a box at the bottom of `README.md` holding the code quality score (one percentage), with three things expandable beneath it: how the score is made up, the count of every file in the repository split by area, and the refactoring plan. It also writes `tools/refactor/refactor-plan.md` — the steps a refactor of this repository follows, in order — and refreshes `tools/refactor/site-definition.md` on the way. It changes no source code, and it ends as a pull request because it changes the README. One command does the measuring: `python3 -m tools.refactor.audit.quality_check .`
- **"Run the widget identification"** / **"identify the widgets"** / **"where are the widgets written by hand"** / **"show me the site definition"** → `widget-identification`. Reads every view and writes `tools/refactor/site-definition.md` alone: what a user sees at each route, in the widget notation from the views themselves, with every place a view writes by hand the markup a catalogue widget should own, and the table of those places widget by widget. Read-only: no branch, no commit, no pull request, no score box, no plan. It identifies the widgets; it never adopts them.
- **"Refactor the repo"** / **"run a refactor round"** → `refactor-round`. One measured round on a `refactor/round-N` branch that takes the next steps from the top of `tools/refactor/refactor-plan.md`, in order: widget adoption (replacing the hand-rolled markup the identification found with the widget), then component breakout, then utility function identification, then design pattern identification and modification, then the sweep to zero. Behaviour never changes. It ends by running the code quality check, so the pull request carries the new score and the plan for the round after. Also due, unasked, when `tools/refactor/deploys_since_baseline.sh` says so, or when `REFACTOR: round N` is open in Your Business Today.
- **"Run end of day"** / **"close the day"** → `end-of-day`. The round if one is due, the check that the connector covers what changed today, the plain-English summary on the day's tasks, and the day's branch pushed and its pull request open.
- **"Set up the widget designs"** / **"Extract the brand from <references>"** / **"Extract the design for <Widget> from <images>"** / **"Check the site against the brand"** / **"Check the widgets against their designs"** → `widget-design`. Optional, for a site big enough to have a widget catalogue. The brand sheet (`docs/design/brand.md`: every colour, type, spacing, radius, elevation and motion token with its role) and a design sheet per widget that has one (`docs/design/widgets/<Widget>.md`, extracted from images or any reference, in token names) are the design; the index is `docs/design/widgets.json`. A question — *are the brand colours used appropriately*, *does RecordsTable match its design* — gets the reading and changes nothing; a **Check** fixes the widget, on a branch, as a pull request. Before building or changing a catalogue widget, or a view that needs a look no widget gives, read the index: a widget with a sheet is built to the sheet, and everything else to the brand sheet. The audit reports when each check last ran; it never runs them, because a design is a judgement.

Two readings are commands rather than skills, and a person asks for them as questions. **"What is the code quality score?"** / **"run the audit"** / **"does the gate pass?"** is `python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast` then `python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json`: the score is printed and the gate says which ratcheted figures are worse than the baseline — read-only, no branch, and not the code quality check, which publishes. **"Is a refactor round due?"** is `tools/refactor/deploys_since_baseline.sh`: the commits on the default branch since the baseline was last committed, against the rhythm (ten unless the project says otherwise).

A process that changes a tracked file — the check's README box, a round's code, a design check's widget — runs on its own branch and ends as a pull request for the person to merge, never as a commit on the default branch. A process that only reads — the widget identification, the audit and the gate, the deploy count — needs no branch and commits nothing; asking for one of these never produces a pull request.

**When no name matches, say so.** A request that names a process this list does not carry — *the widget pass*, *the site survey*, *the cleanup*, *the process I added* — is not routed to the nearest process that shares a word with it. Say plainly that the kit has no process of that name, name the nearest entries by the sentences above with one line each on what they write and whether they change code, and ask which is meant. Put the choice in the kit's words, never in words of your own: a session once asked *"the widget pass (refactor steps 227–232), or the quality check only?"* when the person wanted the read-only site definition, and the option carrying the word *widget* was a five-commit refactor round — the question inherited the very gap it should have named. Guesses are not the menu; the list above is. When the person's request could be one of two entries here, ask with both sentences quoted; when it is plainly one, run it.

Behind all of them the repository carries `tools/refactor/` — an audit that measures the code against the rules below, a score made from every figure it takes, and a gate that fails when a ratcheted figure is worse than the committed baseline. Nothing runs on GitHub: the measuring is run by the skills. Your Business Today can also raise `REFACTOR: round N` on the project as the reminder when its own deploy count reaches N. Refactor rounds never add behaviour or change the schema.

The score is the measure of the rules below, so new code is written to score 100%: every rule in *How I Write Code* is one the audit counts. Before saying any piece of work is done, run the fast reading and the gate — `python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast` then `python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json` — and fix what the work introduced.

## The rules travel with the repository

The coding rules that follow are the whole standard. They live here, in the repository, because a machine-level `~/.claude/CLAUDE.md` does not reach cloud sessions or anyone else's machine. Repository-specific conventions belong below the block, in this file's own notes; decisions worth keeping belong in `docs/`.

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

## Function Size and Shape

**A long function is a contradiction in terms.** The entire point of a function is to break long content into small, named, understandable pieces — so a massive function is a function refusing to do its own job. There is no real reason for one to exist. **Soft limit: ~30 lines.** As with files, when a function approaches the limit the question is never "how do I make this fit" — it's "what have I failed to extract?" Almost always there's a smaller function, a utility, or a separately named step hiding inside. Extract until each function does one thing and its name says exactly what that thing is — then the parent function becomes a short sequence of named steps that reads like prose, which is the whole goal.

- **Functions should be short.** If a function is long, it's doing too much. The extracted pieces don't need to be reused anywhere else to justify existing — a function whose only purpose is to give a name to one step of its caller has already earned its place.
- **One dot per line (Law of Demeter, informally).** If you find yourself writing `order.customer.address.postcode.format()`, the structure is wrong. Either the data is poorly modelled or the operation belongs somewhere closer to the data. `apple.colour.hexCode` should be `Colours.getHexCode(apple.colour)`: the caller holds an apple and asks the thing that knows about colours, rather than walking through the apple's insides. What counts is the depth of one chain of properties, not how many dots a line has: a fluent pipeline of calls — `invoices.Where(…).Select(…).ToList()`, `items.filter(…).map(…)` — hands back something new at each step and is not a walk into anything, while `invoice.project.client.name` is, wherever it appears, including inside a lambda or in markup.
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
- Long method chains (`a.b.c.d.e`).
- Conditions with calls tangled inside calls (`if (is(getApple(1).colour == "RED"))`), and comparisons against raw literals.
- Accessor functions that glue a type to its property (`getAppleColour()` instead of `apple.colour`), and function names over five words or forty characters.
- Components that leave their functions behind in the parent, or reach back into it.
- Functions that mask something the framework should be doing, and functions or components nothing calls.
- The same function — the same body, not merely the same name — declared in more than one file.
- A subject missing a file its design pattern predicts, or an empty file created to satisfy one.
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
10. Is every function in the right home, and is its existence justified — not a utility stranded in a view, not a workaround for the framework, not uncalled?
11. Does every new file sit where the codebase's design patterns predict it, named as its siblings are, and did I add every file the pattern predicts for what I added?
12. Would the code quality score fall because of this change? Run the fast audit and the gate if the repository carries them.

If any answer is "no" or "I'm not sure", fix it before saying you're done.

## How to Work With Me

- **Match the patterns in the existing codebase** if it follows these rules. If existing code violates them, ask before propagating the violation.
- **Don't add things I didn't ask for** — extra config, extra abstraction, extra files. Minimal change that solves the problem.
- **If you think a rule above is wrong for a specific case, say so explicitly** rather than quietly breaking it. I'd rather have the conversation.
- **When in doubt, choose the boring, readable option** over the clever one.

<!-- project-process:end -->

# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle: Raised — accounts' first move once the project team has valued & locked the claim; files a draft against the locked claim's statement, sends nothing — → Submitted, i.e. the claim recorded as sent to the architect/client (the portal never emails it; "Record claim sent") → Approved → Issued → Paid; one click per material stage, driven from the claim card on the valuation page, and every button either creates a portal record ("Raise …") or records an outside event ("Record …") — none says "send"; since 2026-09-09 Issue is "Raise in Xero & issue…", which creates the AUTHORISED sales invoice in Xero and issues here in one press). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.
- **Variation** is the canonical term for the priced change item, and it is **one document with one number through every stage** — its `VariationOrderStatus` (Quoting → Issued → Awaiting AI → Approved / Rejected) is what says where it has got to. Never present "VOQ" and "VO" as two records or two ladder steps: the 2026-07-23 `UnifyVariationOrders` migration folded them into one row, and the UI followed. The record lineage is **three** stages — Request → RFI → Variation. (Bid packages left the chain on 2026-08-12: a variation order sets the sales side for a cost code, a bid package groups works across cost codes by trade — they are separate records, and tendering runs entirely on the bid package. `SelectedBidPackageId` and the packages' parent `VariationOrderQuoteId` column survive as legacy data only.) A user always reads the number as `V72` (`VariationOrder.DisplayNumber`, and the `VariationRef` minted at approval, which is the same number). Anything that leaves the business — the official VO PDF's header, PDF title and file name — carries the number as `VO72` and never the VOQ reference (Nigel, 2026-09-14; `VariationsIdentifierFactory.DocumentReference`, and `VariationDocumentModel` deliberately does not carry `Reference`). "VOQ" survives only in persisted identifiers and API surface: the `VariationOrderQuotes` table and its `VariationOrderQuoteId` column, the stored `Reference` (`VOQ-0072`), the `JPMS/VOQ-…` mail tags, the `/api/…/voq(s)/…` routes, `RecordType.VariationQuote`, and command names like `CreateVoqFromRfq`. On the variation page the priced lines are **Line items** — one table at every stage (the record's own `DraftLines` before approval, the Valuation Report's lines under the V-ref after), edited by one "Edit line items…" in Actions; never show them as a separate "staged build-up" (Nigel, 2026-09-24). The page route is `/projects/{id}/variations/{id}`; the old `/voq/{id}` route is kept on the same page so links already sent out still land. **A variation added by hand is raised priced and Issued** (Nigel, 2026-09-14): `CreateManualVariationOrder` requires the build-up (`Lines`, one per cost centre, at least one) and lands the record in Issued with `IssuedAt` stamped and the lines staged (`DraftLinesJson`, estimate = their total, exactly as `StageVariationOrderBuildUp` would) — raising it manually means it has already gone to the client, so there is no Quoting pass; approval opens pre-seeded with those lines. The `ManualVariationForm` hosts `VariationApprovePanel` (with `ShowSubmitButton` off, the host's button calling `TryBuildRequest`) so the dialog, the Control Centre's staged Raise Variation Order and the connector's `create_manual_variation_order` all take the lines up front. **A rejected variation can be reinstated** (Nigel, 2026-09-24): `ReinstateVariationOrder` (POST `variation-orders/{id}/reinstate`, `AllowedToManageVariations`, connector `reinstate_variation_order`, "Reinstate…" on the status pill of the page and the register and in Actions, `ReinstateVariationDialog`) returns it to Issued when `IssuedAt` is set, else Quoting, and clears `RejectedAt`. One rejected from Approved comes back UNAPPROVED: every CVR accrual under its V-ref (the approval and the rejection's omit, which net to nothing) is removed and the V-ref, value and cost code are cleared, exactly as a return to quoting leaves them, so it is re-approved through the approve flow. Rejected is a decision on the register, not a dead end.

- **Sales strategy** and **lead** (Sales folder, 2026-09-06). A *strategy* is a methodology for
  FINDING leads written down with its justification — audience, target area, hypothesis (why these
  people, why now), evidence, channel, proposition, a Claude-drafted approach plan, a status and
  the funnel its leads make. A *lead* is a person we might convince to build with Jewel plus the
  property the work would be on; every lead lands in the one register whatever found it and
  carries its strategy's id when a strategy did. One ladder for every lead: New → Contacted →
  Engaged → Site visit → Proposal → Won / Lost, Nurture for the parked (`LeadStage`, ints
  remapped from the May prototype by `AddSalesStrategies`). **Won is `WinLead`**, never a stage
  move — it creates the Client account and the project shell in one handler. **`DeleteLead`**
  (2026-09-15; directors, `SalesRoles.Deciders`) removes a lead with everything on it — timeline,
  estimates, proposals, imagine rows — for a mistaken capture or a duplicate; a Won lead is
  refused (its client and project stay), and a real lead that went nowhere is Lost, not deleted.
  Tagged emails keep their `JPMS/LD-####` category; the newest number can be re-issued once its
  row is gone (max + 1, like every global sequence). Code lives in
  `contracts/Sales`, `api/Features/Sales`, `jpms/Features/Sales` + `jpms/Pages/Sales*.razor`;
  the prototype's satellite CRM tables (QualificationAssessments, SiteVisits, InfoChaseItems,
  BidDecisions, Proposals, LeadOutcomes) stay in the database, unread.

- **Draft programme update** (Programme tab, 2026-09-08) is the canonical term for the
  certified valuation's percentages proposed onto the programme's tasks for review — never
  "sync", "auto-update" or "programme import". One opens automatically when **Record approval**
  is taken on the claim card (the architect's certification: `ApproveValuationInvoiceHandler`,
  best effort, after the approval is committed) and by hand from the Programme tab's "Draft from
  valuation…" door against any locked claim. A task's progress is proposed as the £-weighted
  completion of the valuation lines on its **cost centres**; the mapping comes from the task's
  saved mapping (`ProgrammeTaskCostCentres`, written when a draft is applied), else the
  trade-word rulebook (`ProgrammeCostCentreRules`), else Claude (asked once per draft), else the
  reviewer. Progress only — planned dates never move. Only the newest draft on a project is
  Open; the rest are Applied / Discarded / Superseded, never deleted. Code: `contracts/Site/
  ProgrammeDrafts.cs`, `api/Features/Site/Drafts`, `jpms/Features/Site/Programme/
  ProgrammeDraftReview`; spec `docs/Programme-Draft-From-Valuation-Spec.md`.
- **Variations and EOTs on the programme** (Programme tab, 2026-09-09). The Gantt has two
  sections beneath the tasks on the same ruler (`ProgrammeTimeline`): **Variations** — every
  variation but a rejected one, approved or not, placed on the tasks its cost centres map to
  through `ProgrammeTaskCostCentres` — and **Extensions of time**. The programme READS
  variations and never writes them: what a variation does to the programme is the programme's
  OWN record, `ProgrammeVariationEffect` (variation + task + days; `RecordProgrammeVariationEffect`
  / `RemoveProgrammeVariationEffect`, Director + PM, connector actions of the same names). A
  push is an OVERLAY — a segment on the task's end, dashed until the variation is approved —
  and planned dates, cost centres and the valuation never move because of one. An EOT's days
  are the request's own facts (`Request.EotDaysClaimed` / `EotDaysGranted`, EOT only, written by
  `RaiseRequest` / `UpdateRequestDetails` under the "null means not supplied" convention) and
  the row is drawn from the completion it extends (baselined, else current). Placement is pure
  and shared with `get_programme` (`ProgrammeVariationPlacement`, `ProgrammeExtensionPlacement`,
  contracts/Models). RFIs are not on the chart: nothing links an RFI to a task yet.


## A stored skill keeps every version, and any one can be restored (contracts + api + jpms)

- **A save never destroys a version** (2026-09-24, Nigel: "we may need to audit stuff", then
  "ensure he can revert back to a skill easily too"). `SaveAiSkillHandler` copies the outgoing
  version to `SkillRevisions` — text, description, name, `IsPinned` / `IsActive`, who wrote it
  (`SavedByEmail`), when (`WrittenAt`, null on revisions kept before today) and when it was
  replaced (`SavedAt`); `SaveAiSkillReferenceHandler` does the same for a reference document into
  `SkillReferenceRevisions`, and `SkillReferences.Version` counts (migration
  `AddSkillVersionHistory`, script `add-skill-version-history.sql`). `SkillVersionTimeline`
  (contracts/Ai) is the one reading: newest first, a missing written time read off the previous
  version's replacement, and `InForceAt(moment)` for "what did it say on the 12th".
- **A restore is a new version, never a rollback**: `RestoreAiSkillVersion` (POST
  `ai/skills/restore`, `SkillRoles.ManageSkills`) saves the earlier version's name, description and
  text through the same save handlers, so the version it replaces is kept and the restore can
  itself be undone; a skill keeps its discipline, pin and active flag as they are now. Restoring
  the version in force is refused. Never delete a revision row.
- **Surfaces**: the AI Skills page's History panel under an open skill (`SkillHistoryPanel`,
  `jpms/Features/Ai/SkillVersions`: the versions per document, the text, Compare with — a line
  comparison by `SkillTextComparison` — and Restore through `ConfirmDialog`); GET
  `ai/skills/{key}/history` (`GetAiSkillHistory`); connector `list_skill_history` and
  `restore_skill_version` (same gate as the page). Pinned by `SkillHistoryTests`.

## The site note and its photographs reach the portal (contracts + api + jpms)

- **The Contractor's Report intake, changes 1–3 of the FD's 2026-09-15 spec** (`portal-change-spec-
  weekly-report_2026-09-15.md`; change 4, the nine-section Word/PDF report, followed on
  2026-09-16 — see the last bullet). The gap was one-directional:
  the portal could be READ for everything the weekly Contractor's Report needs and could not be
  WRITTEN the one thing it did not hold, the site note and its photographs.
- **`CreateProgressUpdate` is the create from words alone** (ProjectId, Title, Description
  required, WorkDate required, Weather optional, CreatedByEmail) — the connector's
  `create_progress_update`, the counterpart to `update_progress_update`, and the JSON endpoint
  `POST projects/{id}/progress-updates/note`. The Progress page's own multipart form is
  `CreateProgressUpdateWithPhotos` (renamed from the old `CreateProgressUpdate`; same route, one
  save). Two updates on the same day are two updates, never merged; an empty description is
  refused on the words-only create (a day with nothing recorded has no update, not a blank one).
- **Every photograph goes through `ProgressPhotoIntake`** (`api/Features/Progress/Photos`): the
  page's two multipart forms, the connector and the WhatsApp intake alike. JPEG, PNG and HEIC
  (Magick.NET, `ProgressPhotoPreparation`: HEIC → JPEG, EXIF auto-orient, longest edge
  `ProgressPhotoLimits.MaxEdgePixels` = 1600, metadata stripped, PNG stays PNG); up to
  `MaxImagesPerBatch` = 50 per call; **deduplicated on the SHA-256 of the file as received**
  (`ProgressPhotoEntity.ContentHash`, migration `AddProgressPhotoContentHashAndSiteNoteSenders`)
  against what the update already holds and within the batch — never on the file name; a failed
  image is its own `ProgressPhotoIntakeOutcome`, never the batch's. Photos stored before
  2026-09-16 carry an empty hash and are never deduplicated against. Both multipart endpoints and
  the connector answer a `ProgressPhotoBatchResult` (the update + one outcome per image).
- **The connector's `add_progress_photos` takes SOURCE IDS, not files** (`AiProgressPhotoTools`):
  a tool call carries words, so the images come from an email attachment tagged to a record or a
  document filed on the project, by the `source_id` `list_sources` hands out
  (`AiSourceTools.FetchBytesAsync`). Photographs dropped into the chat never reach the portal —
  the tool description says so and points at emailing them to the projects mailbox or the
  site photo pool. Never describe the connector as able to take a pasted image.
- **Import WhatsApp week is gone** (Nigel, 2026-09-22: dead code). The page, its two endpoints,
  the parser/planner, the "Site note senders" project setting and their tests were removed; the
  chat text is the assistant's to read on the person's machine and write through
  `create_progress_update`, never the portal's to parse. `ReportingWeek.EndingOn(thursday)`
  (Friday → Thursday, `api/Features/Progress/ContractorsReports`) is the one survivor — it is the
  Contractor's Report's period. `Projects.SiteNoteSenderNames` stays in the database unread so the
  schema needs no migration; `ProgressUpdates.Description` is nvarchar(max) since the same
  migration. Never bring a WhatsApp parser back into the portal.
- **Change 4 is built: the weekly Contractor's Report** (2026-09-16; `contracts/Progress/
  ContractorsReport*.cs`, `api/Features/Progress/ContractorsReports`, `jpms/Features/Progress/
  ContractorsReports` + `ProjectProgressContractorsReports` / `ProjectProgressContractorsReport`
  pages, the door "Contractor's Reports…" beside "+ Record progress"). ONE row per report
  (`ContractorsReports`, unique on project + PeriodEnd, migration `AddContractorsReports`,
  script `add-contractors-reports.sql`) holding only the ENTERED fields — number, the
  Friday-to-Thursday period (`WhatsAppWeek`), Valuation No., programme reference, prepared by,
  issued to, date of issue, Look Ahead lines (text + done), Neighbours, H&S, the Building
  Control liaison line, Section 8 attendance per work order, and which updates are selected.
  Every register-read section is composed at READ time by `ContractorsReportComposer` (the one
  read behind the page's preview, `get_contractors_report`, and both downloads): Section 1 =
  the selected updates under Friday (weekend folded in) then Monday–Thursday
  (`ContractorsReportDays`); 3 = RFIs not Closed with ResponseDue and an italic count line;
  4 = variations at Issued only, as issued Report 30 prints them — Variation / Position ("Issued
  4 August 2026 — no response received"), NO value column (Nigel, 24 Sep 2026), opened by the
  variations approved in the period (`ContractorsReportVariationWording`, contracts); 7 = the
  active `BuildingControlCase` contact, else the report's entered `BuildingControlContact` line
  (carried week to week); 8 = Subcontractor / Scope under Report 30's opening line, for the work
  orders given days on site only — the scope is the attendance line's own words for the week
  (`ContractorsReportAttendance.Scope`), else the order's title; the page's attendance table
  lists every order Released, or Complete with ScheduledCompletion in the week; 9 = the selected
  days' photographs, two-up, less the report's `ExcludedPhotoIds` (the page's per-photo ticks,
  about twelve a day), with a count line. The assistant SEES a photograph with `view_photos`
  (a pool or progress photo id; several come back as one numbered contact sheet,
  `PhotoContactSheet`). As Report 30 reads (Jeremy's review of Report 31, 24 Sep 2026): Section 1
  is headed with the programme reference, prints only days with a note, each note as bullets
  with no update title, and ends with "Instructions and confirmations received this period"
  (`ContractorsReportInstructions`: AIs received, variations approved, RFIs raised or closed);
  Section 3 leaves out RFIs at Needs action and reads RFI / Status ("Awaiting response, response
  was due …"); Section 8 names the days ticked on site (`ContractorsReportAttendance.DaysOnSite`);
  Section 9 heads each day with its count and fits six photographs a page; the footer is
  "Jewel Bespoke Build Ltd · Contractor's Report No. N · Page x of y"
  (`HouseFooterWithPageNumbers`) — the shared sentences are `ContractorsReportPrintedText`.
  **The PDF is the only output — the Word renderer was removed** (Nigel, 24 Sep 2026); the
  connector's `export_contractors_report` builds the same PDF and hands back a seven-day link
  through `IEmailFileShareStore` (audited `ContractorsReportExported`), because ten megabytes of
  photographs cannot travel in a tool call. A variation's Issued date is the day the CLIENT was
  sent it; a manager corrects it with `SetVariationIssuedDate` (the details card's Issued row,
  connector `set_variation_issued_date`). A new report opens on the current valuation — the newest not Confirmed,
  numbered as its name reads (`ContractorsReportValuations`) — dated the Friday after its week,
  with H&S and Building Control carried forward (migration
  `AddContractorsReportContactAndPhotoChoice`, script `add-contractors-report-contact-and-photo-choice.sql`).
  `CreateContractorsReport` pre-fills what a person would copy from last week (number = max+1,
  header fields carried, unstruck Look Ahead carried, Valuation No. = the highest payment
  certificate on the register — `ContractorsReportCertificates.Highest`, numeric-aware —
  Neighbours' default line, every update in the week selected) and refuses a second report for
  the same period. **The wording gate** (`ContractorsReportWording`: remedial, remedial works,
  making good, rectify, rectification, snagging, defects, rework — whole words, any case) runs
  over every printable line; a hit is a `ContractorsReportFinding` naming section + line, the
  page shows them, and `GET contractors-reports/{id}/pdf` / `/docx` answer 422 with the
  findings — never reworded silently. PDF is MigraDoc on `JewelDocumentStyle`
  (`ContractorsReportPdfRenderer`); Word is the Open XML SDK (`DocumentFormat.OpenXml` 3.1.1,
  now a direct PackageReference; `ContractorsReportWordRenderer` + `…WordParts` / `…WordTables`
  / `…WordPictures`, typed property setters so Word's schema order holds — validated clean with
  `OpenXmlValidator`). Both renderers read the same `ContractorsReportDocument` and the same
  `ContractorsReportText` wording, and PLG's Report 29 template is NOT in hand: the house style
  stands in until it arrives, and the two renderers are the one place to swap it. Jeremy's two
  open rules are answered with defaults, not decisions: Valuation No. is an entered field
  defaulting to the last certificate (the register's number is shown beside it), and Section 4
  reads the portal's variation register. The portal never emails the report — a person
  downloads Word or PDF from the page and sends it. Connector: `list_contractors_reports`,
  `get_contractors_report` (document + findings + updatesInPeriod), `create_contractors_report`,
  `update_contractors_report`, `delete_contractors_report` (confirm-first); pinned by
  `ContractorsReports_reachTheConnector`, rules by `ContractorsReportTests`. Do not extend the
  three-box `ProgressReport` into any of this.
- **The site photo pool: photographs reach the report run by FINGERPRINT, never through the
  model** (2026-09-16, James, after the connector could not take Jeremy's Downloads folder:
  "a big dumping ground for photos and any project … jeremy can do his normal weekly report mcp
  stuff and it does the matching"). A tool call carries words, so no image is ever pasted,
  base64'd or uploaded over the connector. Instead: **Site Photos** (`/site-photos`, Project
  folder, company-wide — deliberately not per project) is a drop zone (`SitePhotos` page,
  `SitePhotoDropZone` / `SitePhotoCard`, `HttpSitePhotoStore`) where a site manager drops the
  week's files BEFORE the run, from the SAME files that go in the WhatsApp export folder. ONE
  table, `SitePhotos` (migration `AddSitePhotos`, script `add-site-photos.sql`; `SitePhotoEntity`,
  **unique on `ContentHash`** — the SHA-256 of the file as dropped, exactly what `shasum -a 256`
  answers for the same file on the laptop), the files under the existing progress photo store at
  `site-photos/pool/{id}/{name}` (`SitePhotoPool`); `SitePhotoIntake` prepares and dedupes through
  the same `ProgressPhotoBatchPlan` / `ProgressPhotoPreparation` as every progress photo. The
  assistant's run: hash the folder → `match_site_photos` (one call, all hashes; a miss is a file
  nobody dropped — name it, never re-encode it) → `file_site_photos` per DAY onto that day's
  progress update (`FileSitePhotosHandler` + `SitePhotoFiler`: copies the prepared file under the
  update's own key, writes an ordinary `ProgressPhoto` carrying the pool's hash so the existing
  dedupe recognises it, stamps `FiledTo*` on the pool row; per-photo outcomes `Filed` /
  `AlreadyOnUpdate` / `AlreadyFiledElsewhere` / `NotFound` / `Failed`, never a batch failure) →
  the Contractor's Report's section 9 reads the updates' photos unchanged. A photo goes onto one
  update only; the pool row stays (Unfiled is the page's working view) and `delete_site_photo` /
  the card's Delete removes the pool copy only. Gates are the Progress page's (`Contributors`
  drop, file and delete; `Readers` read). Endpoints: `POST site-photos` (multipart), `GET
  site-photos[?unfiled=1]`, `POST site-photos/match`, `POST progress-updates/{id}/site-photos`,
  `DELETE site-photos/{id}`, `GET site-photos/{id}/file`. Connector: `list_site_photos`,
  `match_site_photos` (`AiSitePhotoTools`), `file_site_photos` (`FiledByEmail` stamped),
  `delete_site_photo` (confirm-first); pinned by `SitePhotosConnectorTests`, rules by
  `SitePhotoPoolTests`; page guide `/site-photos`; the jpms-operator skill carries the recipe.
  Never build a base64 image door on the connector — it was tried on 2026-09-16 and removed.
- **The week's dump that is not progress is ARCHIVED, never filed and never deleted** (Nigel,
  2026-09-24, for Jeremy's run: "identify images that are not going to be in the weekly report …
  archive the images … keep them in archive just in case"). The criteria are the portal's stored
  skill `jpms-contractors-report` (not progress, no value, documents/drawings, screenshots, photos
  drawn over, snags/defects the chat names) — edited over MCP with `save_skill`, never copied into
  the repo. The assistant looks at the images on the laptop, then `archive_site_photos` (batch,
  per-photo `SitePhotoArchiveReason` + note, the report's project and `PeriodEnd`, `ArchivedByEmail`
  stamped; a filed photo is refused per photo) BEFORE `file_site_photos`. Six `SitePhotos` columns
  (`ArchivedAt`, `ArchiveReason`, `ArchiveNote`, `ArchivedForProjectId`, `ArchivedForPeriodEnd`,
  `ArchivedByEmail`; migration `AddSitePhotoArchive`, script `add-site-photo-archive.sql`); the
  model's `SitePhoto.Archive` / `IsWaiting` is the one reading — Unfiled means neither filed nor
  archived. Filing an archived photo is refused (`SitePhotoFilingRefusals`) until
  `restore_site_photo` / the card's Restore. The page's Archived chip lists them with the reason.
  Pinned by `SitePhotoArchiveTests` and `SitePhotosConnectorTests`.

## The Sales pane: an enquiry tagged to its lead, and the estimate on it (api + jpms)

- **A fifth pathway, Sales** (Nigel, 2026-09-15, the estimating brief): an estimate enquiry
  forwarded to the projects mailbox is tagged in the Control Centre to the sales LEAD it is about
  — an existing lead, or one raised from the email — so the lead reads its mail live and the
  assistant reads the enquiry and its attachments through the connector before pricing it. The
  bucket is `JPMS/Sales` (`TriageCategories.Sales`, `TriagePathway.Sales`, in display order
  Client → Subcontractor → Supplier → Sales → Internal); the tag stem is `JPMS/LD-0012`
  (`LeadLinkProvider`, global number, `RecordType.Lead = 21`). The pane is
  `PathwayPaneConfig.Sales`: Tagging offers Lead, Actions offers Raise Lead, and its Tone is
  `Tone.Accent` — the brand colour, added for it because the four verdict tones were taken and a
  pathway is a category, not a verdict. Existing Control Centre roles only
  (`TriageRoles.AllowedToTriage`); the sales team works leads from the Sales pages.
- **A lead belongs to no project, and the tagging path knows it.** `LinkableRecord.ProjectId` is
  `""` on every lead — even a Won one, whose project's records own the mail from then on — and
  `RecordLinkVocabulary.IsCompanyWide(RecordType.Lead)` is the ONE rule the pane, the Tagged
  picker, the record explorer and the composer read to list a register without a project
  (`GET records?type=Lead`, the blank-project `ListLinkableRecords`; `RecordLinkSection` then
  says "in the register"). A staged lead create is `StagedRecordCreate.BelongsToProject == false`,
  which is what exempts it from `ProjectNeeds()`, `ApplyPlan.CreateReady` and Create now's
  project check. Never add a project-less record type without going through these two flags.
- **Raise Lead is `CreateLeadFromMessage`** (`POST mailbox/message/create-lead`, connector
  `create_lead_from_message`, confirm-first): the SAME `CaptureLead` handler as the Leads
  register (LD numbering, first timeline entry), landing **Engaged, Source Inbound** — an enquiry
  is already a conversation — with the owner the command names, else whoever tags it; then
  `LinkMessageToRecord(Lead, Pathway "Sales")`. The draft (`StagedLeadDraft`, fields in
  `StagedLeadFields`) pre-fills the contact from the sender and the summary from the subject.
  Tagging to an EXISTING lead is the ordinary pick — and `file_email_to_record` with type `lead`
  on the connector. `find_by_reference` resolves `LD-####` and `EST-####`; `read_record_emails`
  takes `lead`.
- **An estimate is Jewel's OWN pricing of one enquiry; a proposal is what the prospect sees.**
  `LeadEstimate` (table `LeadEstimates`, migration `AddLeadEstimates`, script
  `add-lead-estimates.sql`; `EST-####` global) hangs off the lead: scope, the architect or
  consultant, the date the price is due, the budget the prospect mentioned, the total once priced,
  notes, and `EstimateStatus` Received → Pricing → Submitted → Won / Lost (`StatusChangedAt`;
  Submitted needs a `Total` and stamps `SubmittedAt`; Won/Lost close it and refuse edits).
  **Since 536de5c (2026-09-15, Nigel: "make sure we complete the estimate as expected" — the
  Wodeland Avenue tender's shape) it carries the priced breakdown and the client-facing
  document.** The breakdown is `LeadEstimateLines` — sections in print order (Preliminaries &
  preambles, Demolition & stripping out, Structural steelwork…; a section may be a provisional
  allowance, printed as such in red), each a list of lines: cost code (optional, but a Code from
  the cost-centre master when given), description, quantity, unit, unit price, total = quantity ×
  unit price computed server-side; when the breakdown has lines `Total` IS their sum.
  `SetEstimateBreakdown` (`set_estimate_breakdown`, confirm-first) is a FULL-RECORD write — every
  section and line as supplied, anything not sent is gone. The narrative is three fields on the
  record — `ExecutiveSummary`, `BuildTime`, `Exclusions` — written by `SetEstimateNarrative`
  (`PUT sales/estimates/{id}/narrative`, `set_estimate_narrative`) and by nothing else: since
  2026-09-18 the three have their own command, so a details edit cannot blank them and a
  narrative save cannot revert the details. It is a full-record write OF THE THREE — a text not
  sent is cleared.
  The document is `GET sales/estimates/{id}/document` (`EstimateDocumentRenderer`): cover, the
  project page, the executive summary with build time and exclusions, the breakdown chart, one
  itemised table per section, the total, the contact page — **client-facing, "Estimate for
  project"**, the internal `Notes` never print; regenerated on every download, nothing stored.
  Nigel's estimating workbook is still the reference for rates and calculations when it arrives
  — the structure is built, the pricing doctrine is not. Commands `CreateEstimate` /
  `UpdateEstimateDetails` / `SetEstimateBreakdown` / `SetEstimateNarrative` /
  `MoveEstimateStatus` (`SalesRoles.SalesTeam`), `GetEstimate` (`Readers`); `GetLead` carries `Estimates` newest
  first. Pages: `LeadEstimatesPanel` on the lead (Add / Edit modal, the status pill is the move,
  Won/Lost through `ConfirmDialog`, Open + Download PDF per row), beside the lead's "Enquiry
  emails" (`RecordCorrespondenceSection`, type Lead); and the estimate's own page
  `/sales/leads/{leadId}/estimates/{estimateId}` (`SalesEstimateDetail`): the breakdown editor
  (a full-record save — what is on the page is what the estimate becomes), "What the document
  says" (the three narrative fields), Details and the status move in their modals, Download PDF.
  **Every editor on that page is a full-record save, so nothing on it is editable until the
  drafts have been seeded from the record** (2026-09-18): the page seeds when the record arrives,
  not when its refresh succeeds — arriving from the lead page with the record already cached and
  the refresh failing rendered blank boxes that saved blanks over the estimate.
  Connector: `create_estimate`, `update_estimate_details`, `set_estimate_narrative`,
  `set_estimate_breakdown`, `move_estimate_status` (each takes `estimateId`, never the
  reference), and `get_lead` lists `estimates[]` with their ids and `sections[]`. Every write is
  a `LeadActivityKind.Estimate` entry on the lead's timeline. Never call this record a
  "quote" or a "proposal" in copy — the `SalesProposal` is a different record, and it is on the
  connector too (`SalesActions.Proposals`: `save_sales_proposal` drafts, `send_sales_proposal`
  emails the prospect — confirm-first — `withdraw_sales_proposal` is `Deciders`; `get_lead`
  lists `proposals[]` with ids and `imagineLinkIssued`), so the assistant can read the enquiry
  and prepare the proposal a person then reads on the lead's page before it goes.
- **The 3D model on the lead page** (jpms, 2026-09-15, Nigel: "a 3D model of a project I could
  send to the client" — the Imagine concepts are not it; then "less interactive, more an animated
  build-up of works … a play button and then let me look around at the end"). `LeadHouseModelPanel`
  sits under Estimates on every lead that has one and builds the enquiry's house in the browser
  from the architect's drawings — massing, roofs, openings — then PLAYS the works as a build-up:
  the house as it stands drifting slowly, Play, and stage by stage the scaffold goes up, the roof
  is stripped to battens, the dormer rises and is glazed, the front rooflights drop in, the
  garage door comes out and the opening is bricked up, the garden windows and rooflights go in,
  the scaffold comes down — the camera flying to each stage's viewpoint and handed to the viewer
  only when the works are finished (orbit, the four viewpoints, Existing / Proposed chips, all
  disabled until then; `HouseModelPlaybackStrip` under the canvas holds Play → Pause / Skip →
  Replay, the stage caption and a bar per stage). The scene is `wwwroot/js/house-model.js` (the
  classic-script doorway Blazor calls; it fetches the ES module tree under `js/house-model/` only
  when a model is mounted) on a VENDORED three.js (`js/vendor/three/`, named by the import map in
  `index.html` — never a CDN on a client-facing page). A house is a plain definition
  (`js/house-model/models/`: blocks, faces, openings, infills, rooflights, the dormer profile,
  downpipes, context — and its `programme`: the scaffold and roof strip to build, and the stages,
  each naming parts and the move each makes: grow / shrink / drop / lift / appear / vanish / fade,
  `build-sequence/animators.js`). Every part is stamped with a `Phase` (existing / proposed /
  removed / construction) so the toggle is one visibility rule (`phases.js`); an animator's
  progress-1 state is exactly the finished house, so the chips find nothing out of place after
  the build. `build-sequence/timeline.js` is the clock (play / pause / skipToEnd / reset,
  `step(now)` each frame), `hooks.js` what it does to the camera and tells the page
  (`StageChanged`, `PlaybackChanged`). Metres throughout — x along the front from the party wall,
  z from the front wall to the garden, heights above the ground-floor FFL, as the drawings level.
  Verified headless (Playwright on SwiftShader), not by `dotnet build`: no SDK was reachable
  from either session, so the first `dotnet build` is the compile check.
- **The model belongs to the estimate** (2026-09-16, the "3D model per estimate" task, with
  Jeremy's trade-and-phase element list carried in the SAME definition — one dataset, never
  two formats). `LeadEstimate.HouseModelJson` / `HouseModelSource` /
  `HouseModelSetAt` (migration `AddEstimateHouseModel`, script `add-estimate-house-model.sql`)
  hold the definition as JSON, stored whole and never queried; `SetEstimateHouseModel`
  (`PUT sales/estimates/{id}/house-model`, connector `set_house_model`, confirm-first,
  `SalesRoles.SalesTeam`, `ChangedByEmail` stamped; the command's `Model` is a `JsonElement`,
  so the action schema is a plain object and the model never sends an escaped string) replaces
  it whole with a note of the sheets and revision it was read from and writes an Estimate
  activity; `get_lead` returns `estimates[].houseModel` (source, setAt, definition) or null.
  `LeadHouseModelPanel` takes `DefinitionJson` — the newest estimate that has one — and
  `house-model.js` parses it and hands the OBJECT to `viewer.mount`; an estimate with no model
  shows "No model drafted … ask the assistant". `js/house-model/models/` keeps
  `ravens-dene-16.json` as the worked example and the render-check fixture only; nothing looks a
  model up by key any more. The definition gained `phases` (the ordered list, data),
  `trades` (key, name, colour) and `elements[]` — footprint polygons extruded `base`→`top`,
  trade, `phaseIn` / `phaseOut`, `costCode`, `variationRef`, `note` (`builders/elements.js`;
  `phases.phaseOfElement` reads them into existing / proposed / removed / construction so the
  toggle needs no new rule). Extruded footprints cannot be a pitched roof, gable or dormer —
  those stay house parts. The build-up keeps its Existing / Proposed chips and never draws the
  elements — they are stored for the estimator to read off the JSON. A separate "Works by
  trade" viewer over the elements was shipped in a5423c8 and taken out again the same day
  (its script was never loaded by `index.html`, so mounting it threw `jpmsWorksModel was
  undefined` and stopped the lead page's renderer); if it comes back it comes back as its own
  task. `LeadHouseModelSection` renders the build-up for the newest estimate with a model.
  How a definition is read off a set of drawings is the
  `jpms-house-model` skill (`docs/ai/skills/jpms/`, mirrored in `.claude/skills/jpms-house-model/`,
  saved to the portal's skills store with its `definition-schema` reference). Not built yet: the
  standalone client page of the model (`GET /api/sales/estimates/{id}/model` is reserved for
  it) and the estimate email carrying the link.

## H&S site audits and the register they mint onto (contracts + api + jpms)

- **A site audit is the officer's inspection workbook, item for item** (Nigel, 2026-09-15, from
  Katy-Louise's emailed "H&S Inspection Report Framework 27 aug 26.xlsm" and his reply "how we
  can make the attached work in the portal on the H&S phase"). `HsAudit` (per-project `HSA-####`,
  `HsAuditStatus` Draft → Issued → Closed, the front-sheet fields, `Score`, `PreviousScore`,
  `TemplateVersion`) and `HsAuditItem` (one row per framework item: `HsAuditComment`,
  `HsAuditRate` 0 / 5 / 10, `HsAuditClass` A–E, `Minus` (stored, no longer scored — see below),
  `HsAuditTimeScale`, findings, `OwnerName`, `DateRectified`, `HsRecordId`). `HsAuditTemplate`
  (contracts/Models) IS the workbook — 11 sections; `Version` "2026-09-15" is her simplified
  framework of 15 Sep (165 items, `HsAuditTemplateItems.Current`), `FirstVersion` "2026-08-27"
  her original (182); every audit is stamped with the version it was planted from, and a change
  to the framework is a new version, never an edit of a planted audit. Only sections 2, 8 (from
  8.07), 9 and 10 renumbered between the two; `HsAuditItemLineage.CurrentCodeFor(version, code)`
  is the one map (the fire bell's 10.03 is now 10.02; Lorries & Trailers has no successor). The
  machine-readable extracts and the scoping note live in `docs/03-workflows/04-hs/`.
- **The score is her sheet's, exactly — and since 2026-09-22 the class penalties bite**
  (`HsAuditScoring` + `HsAuditClassPenalties`, the one rule, pinned by
  `HsAuditTests.Score_isTheSpreadsheets_onByFrance`): `max(0, rateAverage − penalty)` where
  `rateAverage = Σ rate ÷ (rated items × 10)` (unrated rows out of the denominator) and the
  penalty is charged ONCE PER CLASS PRESENT anywhere on the report — A 0.25, B 0.15, C 0.05,
  D 0.01 — plus 0.05 once if any item's comment is R (repeat); two D findings cost one point, not
  two (Katy-Louise, 15 Sep: "Yes please — Jeremy has since added in a column for the deductions";
  the rule is her sheet's `Analytics!H11`). Banded Poor < 70% / Fair / Good 85–94 / Very good
  95+. The hand-keyed `Minus` of the 27 Aug workbook is NOT in the score any more: the column is
  kept on the row and the form shows `HsAuditClassPenalties.PointsOff` (the sheet's own Minus
  reading) beside it. Recomputed on every `UpdateHsAuditItems` and on Issue; never computed
  client-side except as the form's live chip (`HsAuditItemDraft.LiveScore`, same rule). Under
  this rule By France's HSA-0001 reads 84% Fair (one C, four Ds).
- **Issue is the officer's declaration and mints the corrective actions** (`IssueHsAuditHandler`
  → `HsAuditCorrectiveActions`, pure): one `HsRecord` of kind `CorrectiveAction` per FINDING — an
  item with an owner named OR a rate below 10, unless marked N/A — derived from the row's own
  facts, not the time-scale (her own archive macro keyed off time-scale alone and would have
  captured nothing from the By France audit). Severity from class (A Critical, B High, C Medium,
  else Low), due date from time-scale off the issue date (I today, 1 / 3 / 7 days, 1M = 30, O
  none), `AssignedToName` = the owner. The item keeps the link (`HsRecordId`); closing that
  record (`UpdateHsRecordHandler`) stamps the item's `DateRectified`; `CloseHsAudit` (the
  manager's declaration) is refused while any minted action is still open. One save — an audit
  is never Issued with half its actions minted.
- **An H&S record's owner is a person, not a login.** `HsRecord.AssignedToName` (migration
  `AddHsAudits`, script `add-hs-audits.sql`) beside `AssignedToEmail`; `HsAssignees.IsUnassigned`
  is the one rule (one of the two is required). The site manager on the sheet's Owner column has
  no portal account and needs none — the officer tracks the action, he hears about it as he does
  today. Never require a login to name who owns a finding.
- **Surfaces.** Project folder → **H&S** (`/projects/{id}/hs`, `ProjectHs`): Audits · Actions
  (corrective actions, status changed on the row, overdue in warning) · Register (everything
  else, "Log record"); the audit form is `/projects/{id}/hs/audits/{id}` (`ProjectHsAudit`,
  sections as `HsAuditSectionPanel`s saved one at a time, Issue via `ConfirmDialog`, Close via a
  named-manager `Modal`). Gates: `HsAuditRoles.Readers` = AllInternal, `Auditors` = Admin,
  Director, PM, Site Manager, H&S officer, compliance coordinator. Connector: `list_hs_audits`,
  `get_hs_audit` (items with the `hsAuditItemId` the write takes), `list_hs_records` (the
  register was write-only over the connector until now), `create_hs_audit`,
  `update_hs_audit_details`, `update_hs_audit_items`, `issue_hs_audit` (confirm-first),
  `close_hs_audit`; page guides on both routes. Seed for the first record:
  `scripts/2026-09-15-seed-by-france-hs-audit.sql` (HSA-0001 on By France, Draft, her rows —
  a person presses Issue).
- **The officer's home and her report** (2026-09-22, Katy-Louise's reply read as an ask: for
  the portal to replace her spreadsheet she needs the screen she opens every morning and the
  document she sends on). `RoleHome.ShowHsOfficer` (H&S officer + MD) renders `HsOfficerPanel`
  (`jpms/Features/Hs/Home`): the open corrective actions across the LIVE sites, overdue first,
  and each site's standing — the last issued audit with its score, and the Draft still awaiting
  her Issue; three count tiles read the same `HsOfficerOverview` (pure), so tiles and rows never
  disagree. It reads `ListHsRecords` and the new `ListHsAuditsAcrossProjects` (`GET hs-audits`,
  `HsAuditRoles.Readers`; `list_hs_audits` with `projectId: "all"` on the connector). The
  register's readings `HsRecord.OwnerDisplayName()` / `IsOpen()` / `IsOverdue()` live on the
  model (contracts) — the project page and the home share them. **`GET hs-audits/{id}/pdf`**
  (`HsAuditPdfRenderer` + `HsAuditReportText`, `api/Features/Hs/Audits/Documents`) is the
  inspection report in the house style laid out as her sheet: front sheet, score in its band,
  the keys, the eleven sections printing only the rows she wrote on
  (`HsAuditReportText.IsWorthPrinting`), further comments, the two declarations; any status,
  never stored, never emailed — "Download PDF" in the audit page's toolbar. The register, grid
  and muted-line tables every house-style report draws are `DocumentTables`
  (`api/Features/Documents`), shared with the Contractor's Report renderer; a renderer composes
  them and never re-types a border. Download endpoints render inline with no separate handler,
  as every other download in the api does — an accepted gap in the pattern audit.
- **The thread on a corrective action, and who closes it** (2026-09-23, her 15 Sep replies, the
  YBT goal "Katy-Louise Runs Health and Safety in the Portal"). `HsRecordComments` +
  `HsRecordPhotos` (migration `AddHsActionThread`, script `add-hs-action-thread.sql`):
  `CommentOnHsRecord` (POST `hs-records/{id}/comments`, words alone — the connector's
  `comment_on_hs_record`, `AuthorEmail`/`AuthorName` stamped) and the multipart door
  `hs-records/{id}/comments/with-photos` (the page's `HsRecordThreadModal`, a row click on the
  Actions or Register pane; photographs through `HsRecordPhotoIntake`, prepared and deduplicated
  exactly as a progress photo, stored under `hs-records/{id}` in the progress photo store, served
  by `GET hs-records/photos/{id}/file`). A comment on an Open corrective action moves it to
  InProgress by itself. **`HsActionRoles`** (contracts) is the rule: `Contributors` =
  `HsAuditRoles.Auditors` write; `AllowedToClose` = Admin, MD/FD, H&S officer — a site manager
  never closes his own action. `HsRecordCloseScope` consults it on `UpdateHsRecordEndpoint` and
  in `AiActionScopes` (the connector too); the page offers Closed only to those who may
  (`HsRecordThreadHeader`, the row's select). `UpdateHsRecord` carries `ChangedByEmail`/`Name`
  (stamped by the endpoint and the action) so a status move is an event of that person's. The
  register read (`ListHsRecords`, `list_hs_records`) carries each record's `CommentCount` /
  `LastCommentAt` / `HasPhoto`; `list_hs_records` with `hsRecordId` reads one thread whole.
- **One email per project per sitting, never one per item** (`api/Features/Hs/Notifications`).
  Every door that changes a corrective action — a comment, a status move, an audit's Issue
  minting it — writes one `HsRecordEvents` row through `HsRecordEvents.Record` and sends
  nothing itself. `worker/Hs/HsNotificationWorker` (every ten minutes) runs
  `HsNotificationSweep`: `HsNotificationDigests.Plan` (pure) calls a project's sitting over when
  its newest unsent event is `SessionEnd` (30 minutes) old, then ONE email to the project's site
  manager with everything that was not his own doing and ONE to each H&S officer (the
  `Role.HealthSafetyOfficer` directory users) with everything that was not an officer's — nobody
  is told about their own change; the events are stamped `NotifiedAt` once every digest planned
  for the project went, and wait for the next run when one would not. The site manager's address
  is **`Projects.SiteManagerName` / `SiteManagerEmail`** (Project settings → Edit details; on
  `UpdateProjectDetails`, `get_project_details` / `update_project_details`) — a person, not a
  login; blank means nobody is told. The email is `HsDigestEmails` through `IFormMailer` (ACS,
  the same mailer every portal email leaves by) in the `FormEmailFrame`. Pinned by
  `HsActionThreadTests` and `HsNotificationDigestTests`.
- **Not built yet (workflow 04 keeps them):** mobilisation checklist and gate, scheduled
  inspections with overdue escalation, incident investigation, permits-to-work, temporary works,
  subcontractor RAMS/induction acceptance, `create_hs_audit_from_message` for the next audit she
  emails.

## An external login carries its identity, and every external write is scoped by it (api + jpms)

- **An architect login belongs to an architect** (2026-09-21, from the system-wide permission
  check). `DirectoryUsers.ArchitectId` (migration `AddDirectoryUserArchitectId`, script
  `add-directory-user-architect-id.sql`) is the architect twin of `ClientId` and
  `SubcontractorId`: set by the Architects page's "Invite to portal" (`InviteArchitectPortalUserHandler`,
  `POST architects/{id}/portal-invite`, the same office roles as the client invite), carried on
  `SignedInUser.ArchitectId` / `AuthenticatedUserResponse.ArchitectId` / `Auth.CurrentArchitectId`,
  and read by `Gates/ArchitectScope.OwnArchitectId`. **An architect's projects are the ones that
  name their practice as the party** — `Project.PartyKind == Architect && PartyId == theirs`
  (Nigel's decision) — **or list one of the practice's people among the project's contacts**
  (a Project settings → Contacts row linked to the practice's contact book, 2026-09-24: most
  projects correspond with the client), never Lead-stage — through
  `Features/Architects/ArchitectProjects`, the twin of `Features/Clients/ClientProjects`. A
  Role.Architect login with no link reaches nothing, and its project list is empty, not a 403.
- **Every write an external role may make consults a scope in the endpoint** — the permission
  check's rule, now at zero. `RequestScope` (raise on a project: architect → their projects,
  subcontractor → a project they hold an issued work order on, `Portal/SubcontractorProjects`;
  edit, form, messages, attachments: architect → their request), `ArchitectInstructionScope`
  (file on a project, act on an instruction), `VariationOrderScope` and `DefectScope` (both
  gained the architect branch; both, like `QuoteScope`, answer true for an internal role first
  and false for an external login with no link — `DefectScope` used to let an unlinked Client
  through). Pinned by `ArchitectScopeTests`; the shape to copy for the next external role.
- **The connector runs the same scopes** (`AiActionScopes`, `api/Features/Ai/Tools/Actions`):
  `perform_action` used to run an action's Authorisation and Validation and nothing else, so an
  external login connected through MCP reached any record its role admitted (the security
  review's finding, 2026-09-21). `AiActionExecutor` now asks `AiActionScopes.AllowsAsync` after
  the role gate, and so does `post_request_message`; a command that consults a scope on its
  endpoint needs an entry there — the permission check's rule "a scoped command is scoped on the
  connector too" fails when one is missing. Pinned by `AiActionScopesTests`.

## Data protection: the notice, the rights and the retention (contracts + api + jpms + worker)

- **The privacy notice is `/privacy`** (2026-09-21, the data-protection task): a landing-layout
  page outside the sign-in gate (`jpms/Pages/Privacy.razor`, sections in
  `jpms/Features/Privacy/`), linked from the sign-in page, every invite and reset email
  (`PrivacyNoticeLink.Beside(link)` — same origin as the link the email carries), the imagine
  form and the prospect emails' signature. The words are the business's — reviewed by people,
  not rewritten by a session — and the processing record behind them is `docs/data-processors.md`
  (who processes what, where, and every retention period). The privacy contact is
  `PrivacyContacts.Address`; change it there and nowhere else.
- **A person's dossier and their erasure live in `api/Features/DataProtection`.**
  `GetPersonDossier(email)` (Admin → Data protection, `get_person_dossier`) is the subject-access
  reading: the rows that ARE the person (`PersonRecordKinds` — one entry per kind, with what its
  erasure means) and every column that merely names them (`PersonColumns`: every string property
  ending in `Email` on the EF model, keys excluded — a column added next month is covered the day
  it is added). `AnonymisePerson` (administrators only, confirm-first, `anonymise_person`) erases
  the records, rewrites every mention to `PersonPseudonym.For(email)` — one deterministic
  `erased-<hash>@erased.invalid` per person, so the trail still shows one actor — and redacts the
  sentences that quote them (`PersonFreeText`: audit detail, agent summary, lead timeline, message
  body, KPI note, imagine brief). Nothing is deleted: orders, invoices and audit rows keep their
  money and dates. It refuses while a sign-in carries the address — `DeleteDirectoryUser` is the
  door for that, and it now pseudonymises the audit and agent-activity actor (`ActorTrail`) — and
  while a worker under it has history nobody has retired. Never add a "delete everything" path.
- **A worker with history is retired, never deleted**: `RetireWorker` (Workers page → Retire,
  connector `retire_worker` by name — across inactive workers too, `WorkerNameResolver.
  ResolveWhetherActiveOrNot`) clears contact email and phone, marks inactive, closes the
  engagement and stamps `Workers.RetiredAt`; name, rate history and timesheets stay because
  recorded cost and CIS returns are built on them. `DeleteWorker` stays for a worker with none.
- **The imagine form asks two things, separately**: `ImagineSubmission.Consent` (email me my
  concepts — the service tick the round cannot go ahead without) and `KeepInTouch` (optional,
  marketing). The second lands as `Leads.MarketingConsentGivenAt`; the prospect's own "Stop
  keeping in touch" on their imagine page (`POST imagine/{token}/keep-in-touch/stop`) and the
  sales team's `WithdrawLeadMarketingConsent` (`withdraw_lead_marketing_consent`) land
  `MarketingConsentWithdrawnAt`. `LeadMarketingConsents.Of(lead)` is the ONE reading — the later
  stamp wins; neither is NotRecorded — carried as `Lead.MarketingConsent` and `ImagineView.
  MarketingConsent`. `SendSalesProposal` refuses a Withdrawn lead; NotRecorded (every lead captured
  before the question existed) still allows a follow-up on the enquiry they made. The
  concepts-ready email is service delivery and never consults it.
- **Retention is `worker/Retention/RetentionPeriods.cs`**, one period per store with its reason,
  swept nightly by `RetentionSweepWorker`: since 2026-09-21 the audit trail (7 years, indexed on
  `OccurredAt` by `AddDataProtectionColumns`) and the agent activity log (2 years) too. A new
  store that names a person gets a period there, or a line in the processors document saying why
  it is kept for good.

## The onboarding forms, Jewel Bespoke Build's (contracts + api + jpms + worker)

- **The forms of Jeremy's forms dashboard, ported — not redesigned** (2026-09-23, the YBT goal "New
  starters and sub-contractors complete their forms in the portal"). Nine definitions in
  `contracts/Models/FormDefinitions`, their slugs the dashboard's own addresses (`FormSlugs`); the
  questionnaire is the JBB book's "Sub Contractor Questionnaire 2025". **A change to a field, a wording
  or a declaration is its own YBT task decided by Jeremy — never edit a definition, `FormWording` or
  `FormSheetWording` in passing.** The office's own words (screens, emails the dashboard did not send)
  are ours. The dashboard is its own system: the portal never changes, redirects or retires it.
- **The forms are Jewel Bespoke Build's alone** (James, 2026-09-23: the portal is JBB's). The first
  build carried a second Jewel company on every link, pack, submission and register row and a company
  question on the right-to-work form; it was taken out the same day (migrations
  `AddComplianceDocumentIsFromAForm`, then `DropFormCompanyColumns`). `JewelBespokeBuild`
  (contracts/Models) is the particulars the sheet's footer, the emails' Reply-To and the PDFs print.
  Never add a company to a form, a link or a register row.
- **Public pages** `/f/{form}` and `/f/pack/{token}` (`PublicForm`, `PublicFormPack`; `LandingLayout`,
  raw `HttpClient`, no sign-in) talk to `api/Features/Forms/Public`. The sheet is the dashboard's paper
  form in JBB's colours (`.form-sheet`: the canvas black, Poppins, the logo's gold), so its inputs are
  hand-written on purpose, as the imagine form's are. A draft lives in the browser
  (`FormDraftStorage`); files upload one at a time BEFORE the form is sent, under the page's session
  id, and belong to nobody until the form claims them — an unclaimed upload is the abandoned-form
  signal the retention sweep clears after 18 months. Sending twice answers with the first receipt.
- **Links**: a one-time link (`FormInvites`) or a new starter's pack (`FormPacks`, forms decided by
  `FormPackPlanner`, 14 days kept alive 7 more on each use, 60 at most). Only the SHA-256 of a link's
  secret is stored. A link is spent by SENDING, never by opening, and one opened while alive is still
  honoured when sent. The office chases a pack from `/forms/packs`.
- **Data protection is the code's, not the email's**: right-to-work and starter-checklist files live in
  their own containers (`form-right-to-work`, `form-payroll-starters`, else `form-uploads`), read only by
  `FormRoleSets.ReadersOf(store)`; `SensitiveAnswers` (the dashboard's SENSITIVE pattern + special
  category) is never echoed in the person's copy or the office alert, and a restricted form's alert
  carries no answers at all; an emergency form's health answers are withheld until revealed, each reveal
  audited (`FormHealthAnswersRevealed`). **`RevealHealthAnswers` has no connector tool and never gets
  one**; `get_form_submission` withholds the sensitive answers too. Recording a licence check deletes the
  licence photograph and withholds the driving record (`RecordDrivingLicenceCheck`).
- **The registers**: the right-to-work check is recorded against its NAMED checker
  (`SaveRightToWorkCheck`, `RightToWorkRules` — the statutory excuse is the checker's, not the form's),
  with the confirmation email (`SendRightToWorkConfirmation`); training certificates are accepted onto
  the expiry register (`AcceptTrainingCertificate`); every NO on a workstation assessment is an action.
- **Retention is carried out, not proposed**: `FormRetention` (Jeremy's periods from `lib/retention.js`)
  run nightly by `worker/Forms/FormRetentionWorker` on linked api source — destroyed on the date, a
  tombstone and `FormRecordsDestroyed` left behind; a clock starts only on a leaving date the office
  records (Forms → People & companies, which also dates the person's checks and training records).
  The renewal chase (`FormRenewalChase`, 07:30) asks for insurance and tickets before they lapse —
  insurance only where the certificate came in on a form (`ComplianceDocuments.IsFromAForm`); the rest
  of the directory was never promised a reminder.
  Who is on site with lapsed cover is read, not chased: the compliance register's "On site,
  insurance lapsed" chip is `ListCompaniesOnSite` (a Released work order on a project that is not
  Completed) beside `ComplianceInsurance.HasLapsed` (a current certificate whose kind names
  insurance, past its expiry) — connector `list_lapsed_cover_on_site` — and a company on site says
  where under its name on every register row.
  Adding an api file the sweep or chase needs means a `Compile Include` in the worker — run
  `tools.worker_link_check`.
- **Settings**: `Forms__Sender`, `Forms__OfficeAlert`, `Forms__AccidentAlert` (semicolon-separated;
  unset, mail leaves from the portal's own sender with `info@jewelbb.co.uk` as the Reply-To, and alerts
  go to that address), `FormStorage__ConnectionString` (else DrawingsStorage, else AzureWebJobsStorage).
  Migration `AddOnboardingForms` (`add-onboarding-forms.sql`); the company came out in two steps,
  `add-compliance-document-is-from-a-form.sql` before the api deploys and `drop-form-company-columns.sql`
  after. Connector: 15 actions (the five that email someone confirm-first) and 10 reads, pinned by
  `FormsConnectorTests`; page guides in `FormsPageGuides`.

## Katy-Louise's site checks are forms in the forms engine (contracts + api + jpms)

- **Eight definitions, her paper sheets as they are and no more** (Nigel, 2026-09-23, the YBT
  goal's five form tasks): `ToolboxTalkRegisterForm` (G-01, `/f/toolbox-talk`; the talk chosen
  from `ToolboxTalkTopics` — Akeva's G-02 89 + G-03 26, one choice gives title and number),
  `LadderInspectionRecordForm` (I-05, `/f/ladder-inspection`), `WorkEquipmentScheduleForm` (I-01,
  `/f/equipment-schedule`), `PuwerInspectionRecordForm` (I-03, `/f/puwer-inspection`),
  `SiteIncidentReportForm` (K-02, `/f/site-incident`), `PersonnelIncidentReportForm` (K-03,
  `/f/personnel-incident`), `FirstAidKitChecklistForm` (I-14, `/f/first-aid-kit`; the sixteen
  items and their quantities per kit size are `FirstAidKitContents`), `FireExtinguisherInspectionForm`
  (E-02, `/f/fire-extinguishers`; eight dated check columns, no servicing, no supplier, no
  reminders). `FormCatalogue.HealthAndSafety` lists them; the door is `HsSiteCheckFormsMenu` on
  the officer's home panel and the project H&S tab. A change to a field or a wording is
  Katy-Louise's own task, as every definition's is Jeremy's.
- **The engine grew what her sheets needed, as data.** `FormQuestionKind.Table` (`Ask.Table`,
  `FormTable`: `FormColumn`s of kind Text / Date / Choice / Tick / Label, a `RowNoun`, an optional
  `FixedRows` checklist, `MostRows` 60) is a paper register's ruled rows; the answer is ONE JSON
  string of rows under the question's key (`FormTableAnswers`: `Read` / `Write` / `Cleaned` —
  only the table's own columns, cells capped at 512, a checklist keeps exactly its fixed rows with
  the labels re-imposed, a free table drops a row nobody wrote on — `HasAnAnswer`, `Sentence` for
  emails and the PDF). On the sheet a row is a card of labelled fields (`FormTableInput`,
  `FormTableCellInput`; a free table adds "another <noun>"); the office reads it as a small table
  (`FormTableAnswerView`). **`FormFilingKind.Site`** files a submission under the site named in
  its `site` answer (a `FormFolder` of kind Site; retention as a company's). **`FormEvidenceStore.
  HealthAndSafety`** (container `form-health-and-safety`) is read by `FormRoleSets.
  HealthAndSafetyReaders` — the office plus the site manager and the H&S officer — so the Received
  list (`/forms`, `list_form_submissions`, `get_form_submission`, the PDF and files) opens to
  `FormRoleSets.AnyReader` and is narrowed per row to the stores the reader may read
  (`FormSubmissionReading.VisibleTo`); a site role sees the Received tab alone (`FormsTabs`,
  `NavigationRoles.FormReaderRoles`). The two incident reports are `IsAnAccidentReport`
  (the alert goes to `Forms__AccidentAlert`, and — the store being restricted — carries no
  answers); K-03's injury, cause, treatment, absence and notes are `IsSpecialCategory` (withheld
  until revealed, each reveal audited), its NI number, date of birth and sex sensitive by key.
  Pinned by `FormTableTests` and the site-role case in `FormsConnectorTests`.

## The IT, Cyber, AI & Monitoring Quiz is a marked form (contracts + api + jpms)

- **One quiz, ported from JPS's Microsoft Form for Jewel Bespoke Build** (2026-09-24, Jeremy's due
  diligence task; the source text is `docs/03-workflows/forms/jps-it-cyber-ai-monitoring-quiz.md`).
  `CyberQuizForm` (`/f/cyber-quiz`, `FormSlugs.CyberQuiz`) asks name, email, company and date, then
  the 25 questions (`CyberQuizQuestions`, the form's order); filed under the COMPANY. JPS reads as
  Jewel Bespoke Build / JBB and nothing else was reworded — a change to a question is Jeremy's task.
- **The answer key never reaches the browser.** The definition travels to the public page, so the
  key and the pass mark live in the api (`FormQuizzes`: the index of each right choice, 23 to pass),
  and `FormQuizzes.Mark` is the ONE rule — a point per right answer — read at every turn from the
  stored answers, never stored: the receipt (`PublicFormReceipt.QuizScore`, the done sheet), the office
  view and its PDF (`FormSubmissionView.QuizScore`), both emails (a "Score" line) and the connector's
  `get_form_submission` (`quizScore`). A corrected key re-marks every quiz already sent.
- **Recorded against the supplier by `FileQuizToDirectory`** (the quiz page's "File to the
  directory…", connector `file_quiz_to_directory`, office roles, confirm-first): the quiz's record PDF
  becomes the company's current compliance document of kind `QuizComplianceRecord.Kind` — one kind,
  so a retake supersedes — named with the result; a pass stands a year, a fail is filed already
  expired so the standing reads Expired until a retake passes. Pinned by `CyberQuizTests`.

## An api file the worker compiles may only reach for what the worker compiles

**Run `python3 -m tools.worker_link_check.check .` before committing anything under `api/`.** It
answers this in about a second and it is the only check that does.

`worker/Jewel.JPMS.Worker.csproj` links a NAMED SUBSET of `api/` by `Compile Include` (about 180
files), so a linked file compiles twice — once with the whole api, once with the worker's much
smaller set. A type it reaches for that the worker does not compile fails only the worker build,
and the source gives no sign of it: **a type in the file's OWN namespace needs no `using`**, so
nothing to read says it is missing. That broke main three times on 2026-09-21 —
`IImagineImageStore` and `Jewel.JPMS.Api.Auth` through usings on `ImagineNotifier.cs`, then
`LeadMarketingConsents` through a bare call in `SalesEntityMapping.cs`, which no reading of the
usings could have caught.

The second shape of the same break (2026-09-23, the H&S digest sweep): a type the worker DOES
compile, named through a global using only the api has — `JpmsContext` with no
`using Jewel.JPMS.Api.Data;`, because `api/GlobalUsings.cs` supplies it and `worker/GlobalUsings.cs`
does not. A linked file writes every using it needs itself (`Jewel.JPMS.Api.Data`,
`Microsoft.EntityFrameworkCore`); the check reads both GlobalUsings files and fails on the gap.

Two ways out, and the type decides which: a fact the api, the worker and the portal all state
belongs in `contracts/` under `Jewel.JPMS.Models`, which all three global-use (`PrivacyNoticeLink`
moved there for this reason); a type that is genuinely the api's own earns a new `Compile Include`
line next to the file that needs it (`LeadMarketingConsents` beside `SalesEntityMapping`). Neither
project can be compiled from a cloud session — the SDK's hosts are blocked by the egress proxy —
so the first CI build is the compile check, and this tool is what stands in for it.

## A supplier accepts a work order from the link in the PO email (contracts + api + jpms)

- **The acceptance link is the credential** (Nigel, 2026-09-23: suppliers have no portal login and
  need none to accept). Every purchase-order email — the PO page's send, the automatic send on
  release, the tender award, the reply-into-a-thread draft — carries
  `https://portal…/work-orders/accept/{token}` above its sign-off. `WorkOrders.AcceptanceToken`
  (+ `AcceptanceTokenIssuedAt`; migration `AddWorkOrderAcceptanceToken`, script
  `add-work-order-acceptance-token.sql`, unique index by the `[Index]` attribute on the entity —
  SQL Server's convention filters it to IS NOT NULL) is minted ONCE by
  `WorkOrderAcceptanceLinks.IssuedForAsync` on the first send and re-used on every later one, so an
  older email still works; it dies only because the order closes. The paragraph is inserted
  server-side (`WorkOrderAcceptanceEmailParagraph`, above `<p>Kind regards,`), so `WorkOrderPoEmail`
  composes no link and no door ever sees the token. `PublicSiteUrl` is the origin, as for imagine.
- **The public page is `/work-orders/accept/{token}`** (`WorkOrderAcceptance` page on the landing
  layout, raw HttpClient like `/imagine`): the purchase order exactly as the PDF prints it
  (`WorkOrderPoDocumentBuilder` feeds `WorkOrderAcceptanceView`, rendered by the same
  `PurchaseOrderSheet`), the directory contact's name pre-filled and editable, one Accept. GET/POST
  `work-orders/accept/{token}` (`WorkOrderAcceptanceEndpoints`, anonymous, declared in
  `tools/permissions/policy.json`): an unknown token is a 404 with no detail. The stamp is
  `WorkOrderAcceptance.TryStamp` — the ONE rule, shared with the logged-in `AcceptMyWorkOrder` door:
  issued orders only, and an accepted order is never re-stamped. Via the link the order carries
  the TYPED name and the directory contact's email; Jewel hears about it through the Accepted pill,
  `AuditEventType.WorkOrderAccepted` (actor = that email) and the connector's work-order reads —
  no email. `WorkOrder.IsAwaitingAcceptance` is the page's reading of whether Accept is offered.
  Pinned by `WorkOrderAcceptanceTests`.

## Work-order mail tags are project-qualified (api + jpms)

- **A work order's tag stem carries its project** (2026-09-14, the By France / Coombe Lane
  collision): `JPMS/JBB-2026-001-WO-0045`, built by `WorkOrderTags.Stem` exactly as `RequestTags`
  builds a request's. Order numbers are minted PER PROJECT (`CreateManualWorkOrder` /
  `ApproveWorkOrder` take MAX within the project; the migrated Buildertrend seeds keep their PO
  numbers), so the flat `JPMS/WO-0045` named By France's Farrant order AND Coombe Lane's Hamilton
  Glass order, `WorkOrderLinkProvider.FindByTagAsync` returned whichever row came first, and the
  Control Centre's "use existing tags" refused the thread as another project's. Every new link,
  PO email and reply draft (`SendWorkOrderPoEmailHandler`, `PrepareWorkOrderReplyDraftHandler`)
  writes the qualified tag; the resolver verifies a qualified stem against the candidate's own,
  and accepts the bare legacy stem only when ONE order carries the number — never a guess.
- **Every reference a person reads is project-qualified too** (2026-09-24, Jeremy: WO-0054 was By
  France's JP Air Conditioning order AND JBB-2026-005's painting order). Numbers stay per project
  and existing orders keep theirs; what is shown or sent — screens, the PO PDF and its file name,
  PO emails, statements, the supplier account, the Contractor's Report, Xero bill cards and the
  ledger note, audit text, the connector — reads `JBB-2026-001-WO-0054`, the same spelling as the
  tag stem. `WorkOrderReferences` (contracts) is the one spelling (`Short`, `Qualified`, and
  `TryRead` for however it is said); the `WorkOrder` model carries `ProjectReference`, filled by
  `ToModel(projectReference)` / `WorkOrderProjectReferences.ModelOfAsync`, so `order.Reference`
  is qualified; `WorkOrderEntity.Reference` stays SHORT because the tag stems build on it, and
  `ReferenceOn(projectReference)` is the entity's qualified reading. Never format
  `WO-{Number:0000}` in a view. `find_by_reference` takes the qualified form (exactly one order)
  and the short form (every project's order with that number, each with its project);
  `get_work_order_context` takes either. Pinned by `WorkOrderReferenceTests`.
- **Historic mail moves with `RetagWorkOrderWorkflowTags`** (POST `mailbox/retag-work-orders`,
  connector action `retag_work_order_tags`, confirm-first, triage roles): `WorkOrderRetagPlanner`
  moves a unique number's tag whole, moves each thread of a colliding number to the order the
  audit trail's link rows name (`RecordLinked` / `EmailTriaged` / `EmailSent`… carry the order and
  the conversation), and LEAVES what the trail cannot place, listed in the summary
  (`leftForAPerson`) for the Control Centre's Tagged view. Run it once after deploying; re-runs
  are safe. Until it has run, a colliding legacy tag resolves to nothing and the triager picks
  the records by hand.
- **The connector reads the audit register: `list_audit_trail`** mirrors `/audit` — same
  filters, same shape-dependent gate (`AuditReadGate`, shared with `AuditEndpoints`: the register
  for the triage roles, one record's history for the internal team, cost-centre recodes for the
  commercial team, KPI events for administrators). The MD's rule, 2026-09-14: if it can be read
  on the site it can be read over the connector.

## Work orders are placed with suppliers as well as subcontractors (api + jpms)

- **One record, both panes** (Nigel, 2026-09-15): the purchase order Jewel places with a
  materials/goods SUPPLIER is the same `WorkOrder` as the one it places with a trade — one Work
  Orders tab, one per-project WO sequence, one PO PDF — so there is no separate "purchase order"
  feature. `PathwayPaneConfig.Supplier` offers Work Order for tagging and Raise Work Order in
  Actions exactly as the Subcontractor pane does (`RecordLinkVocabulary.SupplierLinkTypes` too).
  The company picker (`WorkOrderCompanies`, shared by `WorkOrderForm` and the Control Centre's
  staged order) offers vetted Subcontractor AND Supplier directory records — never prospects,
  clients or architects — under the label "Company", keeping an order's existing pick listed
  whatever its category is now.
- **The pathway follows the company, not the type.** `CompanyPathways` (api, was
  `WorkOrderPathways` until 2026-09-16): a Supplier-category company files the record's mail
  under `JPMS/Supplier`, any other under `JPMS/Subcontractor`. The answer travels per record on
  `LinkableRecord.Pathway` (filled by `WorkOrderLinkProvider` and, since 2026-09-16,
  `DefectLinkProvider`) and the link layer reads it through
  `TriageCategories.BucketFor(LinkableRecord, chosenBucket)` — `LinkMessageToRecordHandler`, the
  composer's `LinkRecordAsync`, the backfill sweep (work-order stems resolve through the
  provider). Outbound PO mail (`SendWorkOrderPoEmailHandler`, `PrepareWorkOrderReplyDraftHandler`)
  stamps the company's bucket and audits under its label.
  `TriageCategories.BucketFor(RecordType.WorkOrder)` still answers Subcontractor — the type's
  default for callers holding only the type — and `RecordLinkVocabulary.ImpliedPathway(WorkOrder)`
  is null (the Tagged picker's heads-up can't say).
- **A defect is pathway-NEUTRAL** (2026-09-16; `BucketFor(RecordType.Defect)` is null): the
  same DEF-#### is a trade's workmanship (Subcontractor pane) or a merchant's faulty goods
  (Supplier pane, since 2026-09-07). The side is the pane's explicit choice when there is one —
  `TriageQueue.PanePathwayFor` sends it with every Control Centre pick of a defect (as it does for
  a cost centre), and the staged create carries it on `CreateDefectFromMessage.Pathway` — else the
  defect's own company (`DefectLinkProvider` → `CompanyPathways`; Subcontractor when none is
  named), which is what the defect page's Find & tag and composer get. For a neutral type the
  choice beats the record's own pathway; for a typed record the record answers and the choice is
  ignored (`BucketFor(LinkableRecord, chosenBucket)`). Pinned by `DefectPathwayTests`.
- **A defect goes to its supplier from the assistant too** (2026-09-16):
  `SendDefectToSupplier` (`api/Features/Closeout/Commands`, `AllInternal`) wraps the SAME
  `SendMailboxEmail` the defect page's composer sends — the supplier's address from the
  defect, the raise or chase wording from `DefectSupplierEmails` (contracts/Closeout; the page's
  `DefectSuppliers` now reads the same wording), the defect's tag, the company's pathway
  (`CompanyPathways.LabelFor`) — so `DefectSupplierSendRecorder` stamps `SentToSupplierAt`
  exactly as a page send does. Connector: `send_defect_to_supplier` (confirm-first, `SentByEmail`
  stamped; subject/body overridable), and `list_defects` carries `supplierEmail` — the address,
  subject and body the action would send — so the assistant shows the email before the yes.
- **Badges count where the draft was staged**: `StagedRecordCreate.Pathway` is stamped by
  `StagedRecordActionEditor` from its pane, so a work order (or defect) drafted on the Supplier
  pane counts on the Supplier badge. Display only for every kind but the defect, whose pane IS
  its side (above); the server decides every other filing.
- **A line may be £0** (2026-09-15, the accountant's On The Level order: the quote lists the
  outlet gullies at 0.00, included under the formers, and the PO must list what the quote
  lists). An amount of 0 is an ENTERED line everywhere — `WorkOrderForm.EnteredLines` (anything
  typed on the row; blankness is "nothing typed", never "amount is zero"), the Control Centre's
  staged order (`TriageStaging.WorkOrderProblem`, `TriageQueue.StagedCreate`) and the three
  server validations (`CreateManualWorkOrder`, `UpdateManualWorkOrder`,
  `CreateWorkOrderFromMessage`, which the connector actions share). A line still needs a cost
  centre and a title; a £0 line adds nothing to the order's value, prints on the PO at £0.00
  with its quantity ("2 nr"), and recodes whole like any line. Pinned by
  `WorkOrderNoChargeLineTests`. Never bring "Every line needs a non-zero amount" back.

## A valuation is ONE object — the claim is the statement (api + jpms)

- **The claim IS the valuation** (2026-09-18, YBT "Consolidate Valuation Reports and Snapshot
  Tagging into One Object"). A valuation report (`ValuationClaim` + `ClaimLines`) and a tagged
  valuation snapshot (`ValuationReportSnapshots` + `…Lines`) were two objects for one thing: the
  state of a project's valuation at a point in time. Now one record is created, tagged, reported
  on, emailed and invoiced from. **Locking IS the statement:** `PreapproveValuationClaimHandler`
  (and an early Confirm from Draft) calls `ValuationStatementLines.FreezeAsync`, which copies
  every bill line by value onto the claim's own `ClaimLines` rows (description, code, qty, rate,
  amount, client reference, statement order — a 0% row for every line the claim had no entry
  for), re-states each row's "this period" by the one rule, and stamps `LockedAt`. Nothing is
  captured at invoice raise, submit or issue any more. `ValuationStatementLines.ReadAsync` is
  the one reader: a locked claim's own rows, a Draft's working copy (`ComputeAsync`). The
  contract is `ValuationStatement(Claim, Lines, IsDraft, AsAt)` / `ValuationStatementLine`;
  `GetValuationStatement(id)` serves it (the viewer, `ValuationStatementPdfBuilder`, the
  workbook, the email, `get_valuation_statement`), and accepts a RETIRED snapshot id too.
- **A locked claim's money never moves; its line shape follows the 2026-09-16 rule.** A
  value-neutral variation re-breakdown re-deals under a locked claim (`VariationClaimRespread`)
  and now re-copies the bill line onto the rows it touches or adds, so a re-downloaded statement
  prints the new breakdown with the same money (the emailed PDF in the mailbox is the historical
  document). Changing the FIGURES is cancel invoice → reopen → edit → re-lock → re-raise, as
  before. Bill edits (`RemoveValuationLineItem`, `RejectVariationOrder`,
  `ReturnVariationOrderToQuoting`) remove only DRAFT claims' rows (`DraftClaimRows`) — a locked
  claim's rows are its statement and survive with `ValuationLineItemId` as provenance.
  `DeleteValuationClaim` refuses while a live invoice stands (the claim is that invoice's
  statement). `Reopen` clears `LockedAt`.
- **One tag per valuation: `JPMS/VAL-{project}-{claim number}`** (`ValuationClaimTags`, the one
  spelling). The Control Centre's Client pane has one "Valuation reports" section — one row per
  period, the claim (`ValuationClaimLinkProvider.ForProjectAsync`, newest first, Confirmed
  inactive). The retired `RecordType.ValuationReportSnapshot` (enum kept, never written) has
  `RetiredValuationStatementLinkProvider`: lists nothing; `FindAsync(old snapshot id)` returns
  the CLAIM's record; `FindByTagAsync("VRS-…")` resolves to the claim. The claim's companion
  stems (`ICompanionRecordProvider`) are the retired `VRS-` stems frozen from it, so
  `RecordEmailReader` still reads mail tagged before the consolidation. The alias register is
  `ValuationClaimLegacyStatements` (old id, project, claim, VRS number, label, taken-at,
  superseded, invoice) — NOT a domain object: nothing lists, tags or renders it.
- **Old addresses keep working**: `/projects/{id}/valuation-snapshots` →
  `LegacyValuationSnapshotsRedirect` → `/valuation`; `GET /api/valuation-report-snapshots/{id}`
  and `/{id}/pdf`, `POST …/{id}/draft-email` resolve the id and serve the claim's statement;
  connector `list_valuation_snapshots` / `get_valuation_snapshot` answer as `list_valuations` /
  `get_valuation_statement`; `file_email_to_record` type `ValuationReportSnapshot` files to the
  claim. `ValuationStages.Of(claim, invoice)` (contracts) is the derived stage — Draft, Locked,
  Invoiced, Sent, Approved, Rejected, Certified, Paid, Confirmed — never stored.
- **Migration in two steps** (a person runs each): `consolidate-valuation-statements.sql`
  (additive: the `ClaimLines` columns, `LockedAt`, the alias register, the backfill from each
  claim's live snapshot else the live bill, the printed reconciliation) BEFORE the api deploys;
  `drop-valuation-report-snapshots.sql` AFTER, once checked. Tests: `ValuationClaimStatementTests`,
  `ValuationStatementTests`, `ValuationStatementExportTests`, `ValuationReportPdfLayoutTests`.
- **Doctrine lives in three places** and must say the same thing: the connector descriptions
  (`file_email_to_record` notes, `get_valuation_context`, `list_valuations`, the Valuation Report
  and Control Centre page guides), `docs/ai/skills/jpms/jpms-email-triage.md` +
  `jpms-valuation-cycle.md` (re-save to the portal's stored skills with `save_skill` after the
  deploy — the DB copy is what the connector loads), and the jpms-operator skill's references.

## Generated documents wear one house style (api)

- **Every PDF the portal renders is `JewelDocumentStyle`** (`api/Features/Documents`): `A4Page`
  (the one page geometry, its bottom margin sized to clear the footer), **`CleanHeader`** — the
  clean document top since 2026-09-15 (Nigel: the documents follow the tender's branding): the
  logo centred on white as the page header, the title with its subtitle on the left, the
  document's key facts (`HeaderFact`: reference, status, dates) on the right, a fine gold rule —
  and **`HouseFooter`** with the `OrangeBand` bleeding off the foot of every page. The navy header
  band every renderer used to draw is gone; the facts it carried moved into the clean top. The
  type is Poppins through `DocumentFontResolver` (shipped under the OFL in the API's fonts
  folder; host fallbacks after it). Palette, `SectionHeading`, `Panelled`, the Label / Value /
  Header / Body cells and the Date / Money helpers live there too — a renderer composes them and
  never re-types a colour or a margin. Word output (the Contractor's Report) mirrors the same
  colours and rules in `ContractorsReportWordParts`.

## Request and variation text is unbounded (api)

- **An RFI's description, response and impact-if-late, and a variation's description, are
  `nvarchar(max)`** (2026-09-16, the site team's ask: RFI text over 2048 characters was refused;
  migration `WidenRequestAndVariationText`, script `widen-request-and-variation-text.sql`). The
  `[MaxLength(2048)]` attributes, `UpdateRequestFormValidation.ImpactMax`, the mailbox create's
  `Clamp(…, 2048)`, the merge's truncation, the three variation creates' description clamps and
  the page's `MaxVariationDescriptionChars` are all gone — never re-add a length on these
  fields. Titles keep their 256.

## A value that does not fit its column is refused with a sentence, never a 500 (api + jpms)

- **Prose has no length limit** (2026-09-24, V33 on Abbot Road, JPMS-4F2116: a 550-character line
  description met `ValuationLineItems.Description` nvarchar(512) and the approval failed as
  "Backend call failure"). Migration `WidenWrittenTextColumns` (script
  `widen-written-text-columns.sql`) took 82 columns people write sentences into — line
  descriptions (valuation, claim, bid package, quote, work order, BoQ, estimate), notes, reasons,
  comments, narratives, message bodies — to nvarchar(max), and `ReconciliationPackages.Name` to
  256 (it is named from a work order's title). The handlers that silently cut those columns
  (`Clamp`, `Truncate`, `[..N]`) were removed with them. A new column people write sentences into
  is unbounded from birth; never put a length on it, and never truncate what a person typed.
- **Every save checks its values first** (`JpmsContext.StoredValues.cs`, `api/Data/StoredValues`,
  linked into the worker): text no longer than its `[MaxLength]`, a decimal inside its precision,
  a required string present — read from the EF model, so a column added tomorrow is checked the
  day it is added. A misfit throws `StoredValuesRejectedException` naming record and field
  ("Name on the project is 257 characters long; it can hold at most 256"), nothing is saved, and
  `StoredValueRejectionMiddleware` answers it — and SQL Server's own truncation / overflow /
  null refusals — as a 400 with the validation array every dialog already shows. The connector
  answers it as `ok: false, errors` (`AiStoredValueRefusal`). Pinned by `StoredValueChecksTests`.
- **A short field's form stops the typing at its column**: `StoredTextLengths` (contracts) holds
  each kind of short field's limit — Name, Title, Reference, AddressLine… — and a form writes
  `maxlength="@StoredTextLengths.X"` (or `MaxLength` on `FormField`'s shortcut input), never a bare
  number. `StoredTextLengthsTests` pins every constant to the columns it guards; a new bounded
  field on a form gets its constant and its pin in the same commit.

## A Useful Information note may hold one shared site credential (contracts + api + jpms)

- **The credential is masked, encrypted and revealed by the directors alone** (2026-09-22,
  Jeremy: the Woodhouse Lane WiFi code; James's decisions: a secret on the existing note, not a
  new record; reveal by role — `UsefulInformationRoles.AllowedToReveal` = Admin, MD, FD; AES at
  rest; every reveal audited). `UsefulInformationNotes.SecretCiphertext` (migration
  `AddUsefulInformationSecret`, script `add-useful-information-secret.sql`) holds the value
  AES-256-GCM under the app setting **`UsefulInformation__SecretKey`** (32 random bytes, base64;
  `SecretProtector`, `UsefulInformationOptions`) — unset, no credential can be held or revealed and
  the notes are unaffected. The list model carries `HasSecret` only; the value leaves the database
  through `RevealUsefulInformationSecret` (GET `useful-information-notes/{id}/secret`) alone, which
  writes `AuditEventType.SiteCredentialRevealed` naming the note and the person, never the value.
- **Setting it is its own command, and the connector never carries it.** `SetUsefulInformationSecret`
  (PUT the same route; `AllowedToManage`, so the site manager who set the WiFi writes it down;
  blank removes it) is deliberately NOT on Add/Update and NOT an `AiAction` — a credential is typed
  on the page (`UsefulInformationCredentialField`, masked), never into a chat, and
  `list_useful_information` returns `hasSecret` only. **Shared site credentials only** — WiFi,
  alarm, gate codes, shared equipment PINs. A person's own password is never stored anywhere in
  the portal; personal device access is a local admin account and a held recovery key.

## The sidebar and the home page are per role (jpms + api)

- **Every sidebar row is gated by a named duty set in `NavigationRoles`** (2026-09-22, Nigel: "review
  each role and build the appropriate homepage dashboard and side nav for their role"; the
  2026-08-11 directors-only clamp is gone). Each set mirrors the API gate of the page its rows land
  on — a row is never shown to a role its page would refuse — then narrows to the role's job:
  the MD/FD/Admin see everything; PM and QS the whole project plus the money (`FinanceRoles`);
  the Site Manager, H&S officer and the office roles the project without any money; Accounts the
  to-do list, Weekly Cashflow and the aged reports; the Foreman the site rows and the Site
  Operative My Day + Policies. A new row names its set from `NavigationRoles` (add one, mirroring
  the page's gate, when none fits) — never a bare role list, never `DirectorRoles` by default.
  `RoleHome` picks its panels by the same reads (a panel whose read the API would refuse for the
  role is not shown — the architect's home carries no cross-project RFI panel for that reason).
- **External logins.** There is ONE app: a view is the same page for every role, tailored to it,
  and what is protected is protected by the API — never a sub-site per role (Nigel, 2026-09-24;
  the client portal's `/client` pages were deleted as dead code the same day). The project's
  client and architect open the same RFI and Variation Orders views as the team, on the projects
  that are theirs (the project list is scoped by `ListProjectsVisibleToUser`, the endpoint filling
  the party from the login, never the request). A Subcontractor still lands on `/portal`
  (`Dashboard` bounces it; `HomeRouteFor` sends Home there).
- **The hard rule: no external person reads the mail stored behind a record.**
  `RecordEmailRoles.Readers` (contracts, = AllInternal) is the ONE rule: the record-mail
  endpoints and the connector's `read_record_emails` refuse outside it, and the widgets that
  render tagged mail (`RecordCorrespondencePanel`, `RecordEmailList`, `RecordCorrespondenceSection`)
  render nothing and read nothing outside it — `Session.MayReadRecordEmails`, judged on the roles
  the person HOLDS, as the API judges. A page an external role can open never depends on a 403
  being handled. A new widget that shows mail gates on the same reading.
- **The rule sits on the data, not the door** (2026-09-24: the RFI conversation,
  `requests/{id}/messages`, merged the tagged mail into its answer and admitted the architect,
  because only the endpoints NAMED for mail were gated). `SignedInCaller` (api/Gates, scoped,
  set by `SignedInUserResolver` on every path — cookie, cache hit, the connector's bearer) is who
  the invocation runs for; `RequestEmailReader` and `RecordEmailReader` return nothing, and the
  request and variation conversation reads return only the shared typed thread, when
  `MayReadInternalCorrespondence` is false — every client, architect, subcontractor and site
  operative login. The request and variation reads admit the project's client and architect
  through `PartyReads` (below) — confined to their projects and stripped; the Architect's
  Instruction register and the other internal registers stay `ProjectDeliveryTeam` / internal;
  `InternalAndArchitect` is for the architect's scoped WRITES and is never a read gate. The
  permission check's rule "mail is reached only by the internal team" follows every endpoint and
  connector tool through the types it is handed (`tools/permissions/reach.py`) and fails any
  that reaches a mail reader while admitting an external role — whatever its route is called.
  An external party's reads come back through its own portal's scoped reads, as the client's do.

## The project's client and architect read the same views, confined and stripped (api + jpms)

- **One rule, `Features/Parties/PartyReads`** (2026-09-24). The RFI and variation reads —
  `projects`, `projects/{id}/requests`, `projects/{id}/variation-orders`, `requests/{id}` (and its
  document, messages, voq, attachments), `variation-orders/{id}` (and its document, messages) —
  are gated by `JpmsRoleSets.DeliveryTeamAndParties` and each asks `PartyReads.MayRead…Async`:
  the internal team reads every project; a linked client (`ClientProjects`) or architect practice
  (`ArchitectProjects`) reads its own, and anything else answers 404. A variation in Quoting has
  not reached a party. What leaves the server for a party is `AsReadBy(user)`: a request without
  its value or internal notes, a variation without its cost code, subcontractor or tender, a
  project without its valuation and Xero facts. The conversations give a party the shared typed
  thread alone and the mail readers give it nothing (`SignedInCaller`). A new field that is
  internal to Jewel is stripped in `AsReadBy` the day it is added.
- **The pages are the team's pages.** `ProjectRequests`, `ProjectRequestDetail`,
  `ProjectVariations`, `ProjectVariationDetail` open to `DeliveryTeamAndParties`, and the sidebar's
  RFI and Variation Orders rows show for the architect and the client (`NavigationRoles.RequestRoles`).
  A panel whose read is the team's alone is not rendered for a party — never left to 403.
- **An external login is made by an invite from its own record, never by hand.** "Invite to
  portal" on the client, the architect practice (`ArchitectInviteDialog`: the office picks one of
  the practice's contacts) or the company links the login; each invite refuses an email that is a
  staff login (`LoginRoles.IncludeStaff`), already linked to another party, or revoked, and drops
  the cached login after the link is saved. Admin → Users and the admin invite offer
  `LoginRoles.AssignedByHand` only, and `ScopedRoleGrants` refuses a request that adds Client,
  Architect or Subcontractor to a login that did not already hold it.
- **The permission check follows the mail, not the route.** "Mail is reached only by the internal
  team" (`tools/permissions/reach.py`) fails an endpoint or connector tool that admits an external
  role and reaches a mail reader — unless the reader itself refuses an external caller (it asks
  `SignedInCaller.MayReadInternalCorrespondence`); a reader that stops asking is reported again.

## Record tabs & the in-view toolbar (jpms)

- **The request chain renders as document tabs, not chips.** `RecordTabBar` (Components) is on
  every page in the chain — Request → official stage (RFI/NOD/EOT) → Variation. Only records that
  EXIST get a tab; the action that creates the next stage lives on the current stage's tab, never
  on a placeholder. On the request page the Request and official tabs are local panes
  (`LocalRequestTabs` + `OnSelect`); the variation tab navigates to the record's own page, which
  renders the same bar — moving along the chain reads as switching tabs. Deep-link the official
  pane with `?tab=official`. Bid packages are NOT on the bar (separation 2026-08-12) — they are
  standalone records under the project's Bid Package Invites tab.
- **Two dates, two meanings.** `Issued` is the official date the correspondent/client was notified
  — it is what lists lead with, and it is user-editable (requests) or stamped by the status
  transition (variations). `Created` is the system's own stamp (`Request.RaisedAt`,
  `VariationOrder.CreatedAt`) — shown only as a secondary fact on detail pages, and never as a
  list's lead date. Don't label `CreatedAt` "Raised".
- **In-view menu options are a `Toolbar` of icon buttons** (`ToolbarButton`, glyphs from
  `ActionIcon`, hover text mandatory), grouped by related functionality with `ToolbarDivider` —
  e.g. document actions (download PDF, email) | data actions (export, refresh). Underlined text
  links and one-off `btn-secondary`s are not the way to add a view action any more. The labelled
  `btn-primary` next to a toolbar stays reserved for the view's one primary act of creation
  ("Raise request"). `ExportToExcelButton` already renders as a toolbar button — keep passing
  `ShowIncludeAllRows`/`IncludeAllLabel` and it offers the current-view / include-all choice as a
  menu. Never wrap a toolbar in a LoadGate; pass `Disabled`/`Busy` to the buttons instead.

## Shared components (jpms) — the look lives in components, never in a view

The Open Book Figma is the design (`docs/ui/open-book-design-rules.md`; tokens in
`jpms/tailwind.config.js`, recipes in `jpms/Styles/app.tailwind.css`, the short form in
`jpms/DESIGN-SYSTEM.md`). A view composes the components below and never re-types their class
strings; when a view needs a look none of them gives, the answer is a new shared component with
its rule written here, not a one-off div. `jpms/DESIGN-SYSTEM.md` §5 has the lint grep that
finds drift.

- **`Page`** wraps every routed view except the landing page: the signed-in + approved gate and
  the one content gutter. `CanAccess`/`AccessDeniedMessage` is the page's own role check; `Bare`
  is for a full-bleed workspace (the Control Centre). No page types `<section class="px-…">` or
  the `sessionReady`/`RequestAccessView` preamble.
- **`PageHeader`** is the page's one header, directly inside `Page`. The top bar (`PageHeading`,
  fed by `PageContext.LabelFor`) IS the page title, exactly as the Figma puts it — so a register
  page gives no `Title`, only the subtitle/count strapline and the actions. A record page's
  `Title` is the record (reference as `Eyebrow`). `Primary` holds the ONE `btn-primary` on the
  page; everything else goes in `Actions` (a `Toolbar`, `SearchInput`, `FilterChips`). Every
  route must answer `PageContext.LabelFor` — add a fallback there before shipping a new route.
- **`SectionHeader`** titles an un-boxed region (18/Semi + its `Actions`); **`Panel`** is the boxed
  one (`Title` + `HeaderActions` + `IsLoading`). `class="panel"` is not written in pages.
- **`Notice`** is the only message box: a fact ABOUT an action or a state, in a `Tone`
  (`Tone.cs`: Negative / Warning / Positive / Info / Muted). Field validation (400/409/422)
  stays next to the field as `FormField Error="…"`; the app-wide error is `ErrorToast`. No
  hand-rolled `rounded … bg-negative/10` box, and no raw Tailwind colour (`amber-*`, `red-*`,
  `emerald-*`) anywhere — `warning` is a token.
- **`Pill`** is every status badge. A view never picks a status colour: it maps the enum to a
  `Tone` in `StatusTones.cs` (`status.ToTone()`) and that mapping is the status vocabulary.
  Domain badges (`ComplianceStatusPill`, `LeadStagePill`, `StatusPill`…) are one-line `Pill`
  wrappers. With `OnClick` a Pill is a button — the status opening its move DIALOG
  (`EstimateStatusPill` → `EstimateStatusMoveDialog`). A status pill that opens a MENU is a
  `DropdownMenu` whose `ToggleClass` is `Pill.ClassesFor(status.ToTone())`, never a `Pill` with
  its own panel beside it.
- **`DropdownMenu`** is EVERY toggle-and-panel on the site: a menu of actions, a row's status
  transitions, a multi-select filter, a picker. There is no second way to open a panel from a
  button. It owns the open-state and the whole dismissal contract — the toggle closes it, a
  press anywhere outside closes it *and still lands on what was pressed*, Escape closes it, and
  an item closes it before running. A page never holds its own `menuOpen`/`pickerOpen` bool, and
  never renders a `fixed inset-0` backdrop to catch the outside click: a backdrop swallows the
  press, so the user pays for the same click twice. Pass `Items` for the common case,
  `ChildContent` (plus `@ref` and `Close()`) when the panel needs its own markup — a tick list
  that must stay open across several picks, a swatch grid. `ToggleClass` makes the toggle look
  like whatever it is (a pill, a chip, a nav row); `ShowCaret="false"` when the toggle draws its
  own. A panel is drawn against the viewport from its toggle's
  measured rect, so no scrolling ancestor clips it and no container gives up its own scrolling to
  show one — the workaround that did (`RecordsTable.HasOpenMenu` and its four siblings) is gone,
  along with the lost scroll position it cost. The price is that a fixed panel cannot travel with
  its toggle, so every menu closes on a scroll. `ShouldKeepFocus` is only
  for a menu acting on a text selection — it stops the toggle taking focus, at the cost of
  keyboard reach. This rule exists because six menus were left hand-rolled when the component
  was pulled out and drifted into three different dismissal behaviours (2026-09-17 fixed the
  lot: the variations and requests status pills, the tagged-inbox filter, the role switcher, the
  rich-text colour menu and the SideNav project picker); the typeahead
  `SearchSelect` is the one deliberate exception, being a form control rather than a menu — but
  it is not an exception to the dismissal rule: since 2026-09-18 it registers with the same
  watcher in `wwwroot/js/dropdown-menu.js`, so "a press outside closes it" has one definition in
  the codebase and no component hand-rolls its own. That watcher is the shared thing, not the
  component; a fixed popup asks it for the scroll close too (`shouldCloseOnScroll`), which an
  absolutely positioned panel must never do or it would close on its own container scrolling.
- **`FormField`** wraps every labelled control — label (14/Med white), the control wearing the
  `field` class, `Hint`, `Error`, `Required`. No bare `<label>` over an input, select, textarea
  or picker. **`Checkbox`** for a labelled tick; native checkboxes and radios get the Figma box
  from the base stylesheet.
- **`RecordsTable`** is a list of records: it owns the panel, the scroll box, `IsLoading` and
  `IsEmpty`/`EmptyMessage`; the view writes the `<thead>`/`<tbody>` inside it. Every `<table>`
  wears `data-table` (sticky header, canvas header row, 48px rows; `data-table-dense` for long
  registers); cells carry only alignment/width classes — never padding, background or colour.
  A clickable row is `tr.is-clickable`, a totals row is `<tfoot>`. `SortableColumnHeader` is the
  header cell of any sortable column. Dates render through `DateText`/`DateTimeText`
  (`DateFormats.cs`, global using), money through `Money`/`WholeMoney`.
- **`TabRow`** (links, underline) switches sibling views by navigating or by pane; **`FilterChips`**
  (buttons, pills) narrows the rows on screen. The difference is visible on purpose. A page never
  defines a `*TabClass`/`*ChipClass` helper — the classes are `tab`/`tab-active` and
  `chip`/`chip-active` and only these two components (and `WorkspaceSectionNav`/`RecordTabBar`)
  render them.
- **`SearchInput`** is every search box (debounced, Escape clears). **`StatTile`** is every
  labelled figure (`IsLoading`, never a placeholder zero); `MetricStat` the un-boxed headline
  figure with delta. **`EmptyState`** is every "No … yet" line, rendered only once its region has
  loaded.
- **`ConfirmDialog`** (a `Modal` preset, `Danger` for the irreversible) and **`InlineConfirm`**
  (the two-click armed button, disarms on blur or after 5s) are the only ways to confirm; a
  page never holds its own `confirming*`/`*Armed` bool.
- **Buttons**: `btn-primary` once per view or dialog footer; `btn-secondary` (grey outline, white
  text) for everything else, `text-negative` on it for destructive; `btn-lg` in dialog footers
  (a `Modal` footer upsizes automatically); `btn-icon` inside a `Toolbar`. Never a hand-rolled
  button class string, never a green outline, never a red fill.
- **Shape and type**: corners are `rounded` (4px) on controls, `rounded-lg` on the modal only,
  none on panels/cards/tables; no shadows except the modal (and the dropdown until its Figma
  frame is read); nothing below `text-xs`; no `uppercase`/`tracking-*` — a label is the `eyebrow`
  class (14/Med G5) or `FormField`'s label.

## Labour settlement & the Xero coding run (api)

- **The run codes one settlement party at a time, never one worker.** `RunXeroCodingHandler`
  (`Commands/XeroCoding`, one partial per concern) groups the month's schedules by settlement
  counterparty (`CodingParty`, 2026-09-08): a sole trader is a party of one; every worker a
  company bills on one invoice is one party, and that invoice is recoded ONCE to every worker's
  lines — status, total, VAT and attachment kept — with `XeroLineTimesheetCover.WorkerId` stamped
  per line so `SettlementScheduleBuilder` reconciles each worker on their own lines (a cover
  without a worker is the counterparty's as a whole, the pre-2026-09-08 meaning). Every gate
  (sign-off, run-once, mapping) is answered per party: one worker not ready holds the company
  bill and every outcome says who is waiting for whom. Asking for one worker on a company bill
  runs and reports every worker on it. Outcomes and run records stay per worker-month.
- **Approving a covered labour bill is the portal's, once every worker on it reads Matches**
  (`ApproveLabourBillHandler`, 2026-09-08): DRAFT → AUTHORISED through the same
  `ApproveInvoiceAsync` the allocation write-back uses, with an EMPTY instruction list so every
  line passes through untouched. `CoveredBillResolver` tells the settlement view which bill
  covers each worker and whether the rule is met (`WorkerSettlementSchedule.CoveredBill`); the
  row's "Approve in Xero" and the `approve_labour_bill` action share the handler. The outcome is
  recorded per worker as `BillApproved`, and `XeroCodingOutcomes.IsWritten` is the ONE definition
  of a written month — the run-once gate, the reset, the correction guards, the chase list and
  the table's Reset button all read it; never re-list the outcomes by hand.
- **A matched bill Xero says is gone is followed to its live re-issue** (`.Reissue`): same
  invoice number (`IXeroClient.FindBillsByNumberAsync`), same contact, same period, DRAFT /
  SUBMITTED / AUTHORISED; the predecessor's ledger lines and cover move onto the re-issue's fresh
  lines. Several recognised candidates are read fresh from Xero before the run says "two bills"
  — the ledger's status can be a night old. Of several LIVE bills sharing one number,
  `ReissueChoice` takes the one whose net is the schedule's, else the newest (`UpdatedUtc`, then
  date), and the preface says which over which; a tie is a skip. Different numbers stay a skip.
  Never stage a draft beside a voided bill.
- **A settlement variance follows the cover of the line it names** (`SettlementVariances`,
  2026-09-08): the builder nets it into that worker's month (`PostedVariance`), so
  `Difference`/`Verdict` read what is still unexplained. A variance with no line has no month —
  the connector notes tell the accountant to post against `coveredBill.lineIds`.

## Work Order bills on the Cost allocation page (api + jpms)

- **A Work Order bill is coded from its order — the one place the ORDER drives the invoice.**
  Everywhere else the invoice drives the order (`WorkOrderInvoiceRecoding`: linking recodes the
  order's lines to the invoice's centre); `ApproveWorkOrderBillHandler` (2026-09-08, the
  accountant's ask) never calls it. `WorkOrderBillRecognition` runs on every unallocated read
  beside `LabourSupplierRecognition` and stamps `XeroLedgerLine.WorkOrderMatch` / 
  `WorkOrderExceptionReason` per BILL (decided once per invoice, memoised): the labour registry
  wins → the supplier resolves to its directory record through `DirectoryXeroMatcher` → a WO
  number on the bill (`WorkOrderBillReference`: Reference, then descriptions, then invoice
  number; supplier + number, since numbers are per project; the bill's site breaks a tie — the
  project set on the bill in the portal first, else its Xero Sites hint, the sweep's own
  precedence, 2026-09-09) → else exactly one open order → else, among several, the amounts
  (`WorkOrderMatchRule.ByRemainingValue`, 2026-09-11, the accountant's ladder: the bill's net is
  exactly what is left on one order, or on ONE unique subset of the supplier's open orders —
  £3,092 = WO-0055 £1,748 + WO-0056 £1,344 — and each order's remaining value is its proposed
  figure, so the card and `approve_work_order_bill` need only Approve; credit notes and
  suppliers with more than 16 open orders are not tried) → else `BySupplierOrders`: the card with
  every order listed and NO figure proposed, a person keys the split (a part bill naming no
  order, or a total that fits more than one way — nothing is ever guessed) → the value gate. "Open" = Released with remaining value
  > 0 (decision 2026-09-08). Nothing is persisted for the match, so Sync and Re-check re-run it
  for free; the sweep (`AllocateSuggestedXeroLinesHandler`, page button and nightly worker
  alike) skips matched bills exactly as it skips labour lines.
- **The queue a line sits in is ONE rule, shared** (`contracts/Xero/XeroLedgerQueues.cs`,
  2026-09-11, the accountant's finding: the connector read 44 Unallocated lines against a tab bar
  of 14): `XeroLedgerQueues.Of(line, viewerMayHandleUnplaced)` → ToCode / Labour / LabourCovered
  / WorkOrderBill / WorkOrderBillHeldForFinance. The page partitions with it (by the role the
  user is VIEWING AS — `Session.ActiveRole`), `list_xero_ledger_lines` stamps `queue` (+
  `projectTab`, a `labour` block with worker/covered month/verdict) on every Unallocated line,
  takes `queue` as a filter and returns the tab bar (`tabBar`: toCode, workOrderBills as BILLS,
  labourOutstanding, labourCovered, awaitingAction) from `GetXeroLedgerCounts`. Never partition
  the ledger any other way.
- **An unplaced Work Order bill (`BySupplierOrders`, figures to key) is the Finance Director's
  card** (2026-09-11, the accountant's rule: the owner never sees a money field, and the plain
  queue would lose the order link). `XeroLedgerQueues.MayHandleUnplacedWorkOrderBill` = FD or
  Admin: any other viewer gets no card and no plain-queue row (`WorkOrderBillHeldForFinance` —
  the bill waits on the FD's tab), the connector withholds `workOrderBill` on those lines, and
  `ApproveWorkOrderBillHandler.RequireApproverMayKeyTheFiguresAsync` refuses server-side off the
  approver's directory roles (`UserRoles.ForAsync`), whatever a client sent.
- **Reference beats amount; the conflict is badged** (`WorkOrderBillMatch.AmountNote`,
  2026-09-11): when a bill landed by its reference / line references / the supplier's only order
  and its net is exactly what is left on a DIFFERENT order or unique set (the remaining-value rung
  run as a cross-check over what it named), the note names the fit; the card shows an amber
  "Amount fits another order" pill, the connector reads it out in the confirm turn, nobody
  re-routes the bill. Amounts that agree with the reference earn nothing.
- **One bill may pay several of the supplier's open orders, as a figure per order off the
  BILL TOTAL — never off the Xero lines** (2026-09-09, the accountant's ask: the lines are the
  supplier's own CIS labour / materials split and are left exactly as raised). The card is one
  `WorkOrderBillOrderSlice` per open order (`WorkOrderBillMatch.ProposedSlices` seeds it, the
  same on every line of the bill; `SupplierOrders` lists the orders) and checks the figures tie
  to the bill's net. The read proposes them: the whole bill on the matched order; a slice per
  order when the lines' descriptions name two or more different orders
  (`WorkOrderMatchRule.ByLineReference`, `…Recognition.ByLine`; an unnamed line goes with the
  bill's reference, else the supplier's only order, else the first named order, and the detail
  says so); a bill whose reference names several orders reaches the card on the first, gated
  against the orders' COMBINED remaining value. Otherwise the gate is per order. At approval
  `WorkOrderBillSliceSpread` spreads each slice over the lines pro rata (penny-safe per line,
  the drift settled on the largest line so every order is exact too) and each line's portion
  over the order's cost codes — portal-side only. `WriteBackWorkOrderBillAsync` passes
  `keepLinesWhole`: when every line lands on one centre the tracking is written as before; when
  any line would need two tracking values the bill is approved in Xero with NO tracking at all
  (`XeroWriteBackOutcome.Note` = `WorkOrderBillTracking.NotWrittenNote`, the line's `Note` gains
  "no Xero tracking", the card says so before Approve via `WorkOrderBillTracking.CanBeWritten`).
  The accountant's rule (2026-09-09 15:08): lines exactly as raised beats the tracking — the
  order split is the portal's work-order links, Xero tracking is a convenience. Never split a
  Xero line for a Work Order bill.
- **Approve is per bill, undo is per bill, both FD/Director/Admin only** (`WorkOrderBillRoles`).
  Approve re-runs the match server-side, refuses an order that is not the supplier's, figures
  that do not add up to the bill, and a slice over its order's remaining value, stamps every
  line `Note = "Work order WO-0026"` (or "Work orders WO-0055, WO-0056"), writes one
  `XeroLineWorkOrderLinks` row per line share — order AND
  `CostCenterCode` (nullable; hand links leave it null; the unique index is (line, order, code)
  since `AddXeroLineWorkOrderLinkCostCenterCode`) — and one `WorkOrderBillApprovals` row per
  order with that order's slice as `BillNet` (the audit's "which rule matched" and the undo's
  handle; `WorkOrderBillApprovalStamp.Orders` lists them), then `IXeroWriteBackService.WriteBackWorkOrderBillAsync`
  (tolerates an AUTHORISED-unpaid bill via `XeroApprovalRequest.RecodeApproved` — the
  re-approval after an undo). Undo reverses lines, splits, links and package slices in one
  save and clears the tracking off the bill in Xero (`IXeroClient.ClearTrackingAsync`, by bill,
  not by line id — a split approval replaced the Xero lines); **Xero never un-approves**, so
  the outcome and the toast say the bill stays awaiting payment there. Never void from the undo.
- **Links may sit on a same-project centre split since 2026-09-08** (a Work Order bill against a
  multi-code order). `KeepOrClearLinksAsync` keeps links through a same-project re-cut and
  recodes the orders only for a whole-line move; `WorkOrderLinkSlices` expands a split line's
  link into one slice per share for the financial summary — a link that carries its own
  `CostCenterCode` is one slice on that centre outright. A cross-project split still clears.
  The WO Allocation tab's hand link (`SetXeroLineWorkOrderLinks`) still refuses centre splits.
- On the page the tab is a sub-view of Unallocated like Labour (`workOrderBillsTab`, token
  `WorkOrderBills` in the tab memory); the cards (`WorkOrderBillCard` + `…OrderSlices` — the
  figure per order — and `…LinesTable`, read-only) render instead of the table; `notWorkOrderBillInvoiceIds` is
  the this-visit escape to the plain queue; the Allocated row's Undo becomes "Undo bill"
  (`ConfirmDialog`, Danger) when `WorkOrderApproval` is set.

## The supplier account on a project (api + jpms)

- **One supplier's account on one project is `GetProjectSupplierAccount`** (2026-09-14, the
  accountant's ask: "show the invoice numbers linked to these work orders — they have
  over-invoiced"): the supplier's LIVE orders there (Released / Complete) with their priced lines,
  invoiced-and-linked, paid and left to invoice per order — the WO Allocation tab's own figures,
  from the same link slices and `WorkOrderPaidPositions` — then EVERY invoice received from them
  for the project, and the position the two halves add up to: `Received − Ordered`, positive is
  the over-invoice. It is internal (the Work orders tab's `AllInternal` gate); the supplier-facing
  document stays `GetSubcontractorStatement`, which is correspondence and never shows payment or
  unmatched bills. Surfaces: `SupplierAccountModal` ("Account" on a supplier row in the supplier
  view, "Supplier account…" in any order's Actions menu), `DownloadProjectSupplierAccountPdfEndpoint`
  (named by `ProjectSupplierAccountFileNames`), and the connector's `get_project_supplier_account`.
- **Which bills are "received for the project" is `SupplierBillFinder`, decided per BILL**: any bill
  linked to one of the supplier's orders here (whoever the Xero contact is); the supplier's bills
  (the directory record's name by `DirectoryXeroMatcher`, its linked Xero contact name, or the
  contact of any bill already linked to their orders — `SupplierNames`) allocated to the project
  whole or by split share, their still-pending lines riding along; and the
  supplier's bills nobody has allocated yet that `SupplierBillPlacement` places here — the queue's
  precedence: the project set on the bill, else its Xero Sites hint, else a WO number written on
  it, else only when the supplier holds live orders on no other project (`AssumedFromSupplier`,
  said on the row). A bill pointing elsewhere is not this account's; a bill in dispute is
  `Disputed`. `NetElsewhere` says how much of a bill sits outside the project.
- **Labour / materials is by Xero account, never by description**: a line on
  `XeroOptions.CisLabourAccountCode` ("321" unless `Xero__CisLabourAccountCode`) is labour,
  everything else is materials — the supplier's own CIS split, exactly as raised. An invoice's
  `State` (`ProjectSupplierInvoiceStates.For`) reads the settled fraction first, then DRAFT /
  SUBMITTED as "Received, awaiting approval" — received, so it counts in the over-invoice, but
  nothing is owed on it until Xero approves it.
- **The actual payment made and the CIS deducted are Xero's figures, read, never computed**
  (2026-09-14, the accountant's second ask). The ledger sync stamps three more per-INVOICE facts
  on every stored line beside `InvoiceTotal` / `AmountDue`: `AmountPaid` (the cash Xero recorded
  — under CIS, short of the total by the deduction), `CisDeduction` (Xero's own `CISDeduction`,
  calculated at approval and withheld for HMRC) and `FullyPaidOnDate` (migration
  `AddXeroLinePaymentDetail`, script `add-xero-line-payment-detail.sql`; `XeroTransaction` carries
  `CisDeduction` / `FullyPaidOnDate` off the paged read). They default to 0 / null, so a line synced
  before the migration reads "no detail yet" until the next sync — the account shows a DASH, never
  £0.00, for a bill with neither (`ProjectSupplierAccountInvoice.HasPaymentDetail`): a draft has no
  deduction until Xero approves it, an unpaid bill no payment. Never derive CIS as labour × the
  directory's rate — the directory holds the rate as a status string, and Xero already knows.

## Xero write-back state on a ledger line (api + jpms)

- **`InvoiceStatus` is what Xero holds; `WriteBackStatus` is what the portal did.** Two facts,
  never one: `None` covers "approved outside JPMS" AND "still draft, nothing written yet", so a
  "still draft in Xero?" question reads `InvoiceStatus` (`IsAwaitingApproval`), never the
  write-back status. Sync refreshes `InvoiceStatus`; every write (`XeroWriteBackService`:
  approval, site write, tracking clear) stamps Xero's `FreshStatus` back onto the lines through
  `StampXeroStatus` so the ledger does not wait a night to agree with Xero.
- **A failure is never forgotten by the success that cures it** (2026-09-08, the accountant's
  ask): `WriteBackError` + `WriteBackFailedAtUtc` are the LAST failure and survive a later
  Approved / None; only a fresh failure rewrites them and only the Work Order bill undo clears them. The
  Allocated row reads "Approved in Xero by JPMS · Earlier attempt failed <when>: <error>"; the
  `Draft in Xero` / `Write-back failed` chips (`XeroAllocation.XeroState.cs`, `FilterChips`) and
  the export's Xero status / Write-back / Last write-back error columns read the same fields.

## The Invoice document window shows the whole bill (api + jpms)

- **The portal never reads Dext — it reads Xero.** What the bookkeeper types in Dext's
  Description box (2026-09-14, her ask: "Jack Uploaded - Toolstation", so Nigel knows who a
  receipt came from) reaches the portal only as what Dext publishes onto the bill in Xero — a
  line's description, or the bill's `Reference` — and the sync refreshes both every night.
  Dext's Note and Messages tabs never leave Dext; nothing in the portal can show them.
- **So the Invoice document window shows the bill, not just the line it was opened from.**
  `InvoiceBillLines` (jpms/Features/Xero) sits under `LedgerLineSummary` and renders the Xero
  reference when it says more than the invoice number (`XeroLedgerDisplay.ReferenceBeyondInvoiceNumber`
  — Xero's own screens call a bill's number its reference, so a repeat is nothing) and, for a
  bill with more than one stored line, every line with where it stands and its net, the opened
  line marked. It reads `ListXeroLedgerLinesForInvoice` (`/api/xero/invoice/lines?id=`,
  `IBestEffortQuery`: the window is complete without it, so a failed read says so inside the
  card, never as a banner behind the modal). A single-line bill with no reference renders
  nothing — a conditional card, never gated. Stored lines only, so the card never claims a bill
  total. The connector reads the same bill whole: `list_xero_ledger_lines` with `xeroInvoiceId`.
  Both the status read and the bill read shape a line through `XeroLedgerReads.ToModelsAsync` —
  splits, suggestions, labour and Work Order bill recognition, the standing approval — so a bill
  read whole lands every line in the same queue the page shows it in; never build a lighter
  projection that leaves `queue` reading ToCode on a labour or Work Order bill.

## Directory ↔ Xero links (api + jpms)

- **A directory record's Xero link is one `SubcontractorXeroLinks` row, written three ways and
  read one way.** Import from Xero writes it with a NEW record; `LinkDirectoryRecordToXeroContact`
  (2026-09-08, the accountant's ask) writes it onto an EXISTING record — the one-field change that
  replaces "import a duplicate, then Consolidate"; consolidation moves it to the master. Both
  sides must be free: a record already holding a link, or a contact linked to another record, is
  refused naming the holder, and `UnlinkDirectoryRecordFromXeroContact` is the only way to free
  one. `Subcontractor.XeroLinks` carries the contact id/name and who linked it; `XeroLinked` stays
  the bool every list reads. Every link/unlink is audited (`DirectoryRecordXeroLinkChanged`).
- **Name matching between the directory and Xero is `DirectoryXeroMatcher`, which IS
  `WorkerDirectoryMatcher`** — one rule for every "does this name mean that company" question.
  `ListXeroSuppliers` stamps each unlinked contact with the ONE unlinked record it matches
  (`MatchingSubcontractorId`), which is what the import modal's "Link to …" and the record page's
  "Suggested" read; several matches stamp nothing. The connector's `list_unlinked_directory_records`
  shows every candidate, and a match is a suggestion a human confirms — nothing links by itself.
- **Xero's primary person is read, and details move only on request** (2026-09-09, the
  accountant's ask). `XeroSupplier.PrimaryPersonName` is the contact's own FirstName + LastName
  (Xero's "Primary person"), held apart from `ContactPersons`; `XeroDetailsPull.PrimaryPersonOf`
  is the one reading (primary person, else the first additional person) the import and the link
  share. Linking never touches the record unless `LinkDirectoryRecordToXeroContact.
  PullDetailsFromXero` is asked for — a choice, never automatic, because Xero's details are
  often older than the directory's — and then `XeroDetailsPull` copies only where Xero has a
  value and adds Xero's people as contacts where the record lacks them.
- **Contacts push to Xero when a person presses it** (`PushDirectoryContactsToXeroContact`,
  `api/Features/Subcontractors/XeroContacts`). One rule, `XeroContactPushPlanner`, plans both the
  preview (`PreviewXeroContactPush`, the record page's modal: Xero's people NOW beside AFTER) and
  the write, read fresh from Xero each time (`IXeroClient.GetContactPeopleAsync`): the record's
  primary contact → Xero's primary person, its company contacts → Xero's additional persons
  (Xero replaces the list wholesale, five at most), and an EMPTY side on the portal never clears
  Xero's. Names go as first + last split on the first space (`XeroClient.NameParts`). Needs the
  Cost Integration app's `accounting.contacts` scope; a 403 comes back saying so. Audited
  (`DirectoryContactsPushedToXero`). Never call the push from a handler.

## Directory: CIS verification & the compliance register (api + jpms)

- **The HMRC CIS verification result is three fields written together** (2026-09-09, the
  accountant's ask): `CisStatus` is the SHORT reading only ("Verified 20% standard", 64 chars);
  the verification number ("V1415495651") and the verified-on date are `CisVerificationNumber` /
  `CisVerifiedOn`, and `RecordCisVerification` (the record page's "Record verification…", the
  connector's `record_cis_verification`) is the ONE writer of all three. `UpdateSubcontractor`
  keeps its `CisStatus` parameter and refuses one over 64 characters with a 400 that points at
  `RecordCisVerification` — never squeeze the number and date into the status again. On
  consolidation the number and date follow whichever record's status was chosen.
- **Compliance standing is per company and reads in one order.** A company's standing is the
  worst status among its current documents, Missing when it holds none
  (`ComplianceOverviewReadModel.WorstStatusFor`); `ComplianceStatusExtensions.ReadingOrder` in
  contracts (Expired → Expiring soon → Current → Missing, 2026-09-10 the accountant's second ask:
  Missing is most of the directory and was burying the handful of Current companies) is the ONE
  order every compliance list, chip row and the connector's `list_compliance_register` read in —
  `status.ReadingRank()` sorts by it; never redeclare the array locally. The Directory's Compliance `FilterChips` and the register at `/directory/compliance`
  (`ComplianceRegister` page, `ComplianceRegisterRow.Build`: one row per current document plus
  one Missing row per empty company) both read it; the two views are siblings joined by
  `DirectoryViewTabs` (a `TabRow`), and the dashboard's "Documents expiring" tile lands on the
  register. `CisVerificationPanel` sits directly above `SubcontractorComplianceList` on the
  record page — the two together are the record's standing to be paid.
- **The public liability figure lives on the document, and £5m is a flag, never a status**
  (2026-09-10 evening, the accountant's third ask: Jewel's insurer requires £5m PL of every
  subcontractor on a big job). `ComplianceDocument.PublicLiabilityCover` (nullable pounds,
  `ComplianceDocuments.PublicLiabilityCover`, migration `AddComplianceDocumentPublicLiabilityCover`)
  is the limit of indemnity the certificate states — null is "not recorded", never nil cover,
  and normal on a non-insurance document; it is named for what it is, so employers' liability
  would be a sibling column, not a second meaning. The dead `Subcontractors.Pli`/`PliExpiry`
  strings from the master-sheet import are NOT its home — leave them. All three filing routes
  take it (`publicLiabilityCover` form field on the office and portal multipart uploads, read by
  `PublicLiabilityCoverField`; the `FileDocumentToSubcontractor` command), and
  `SetComplianceDocumentDetails` (the record page's "Edit details…", connector
  `set_compliance_document_details`) corrects expiry + figure on the CURRENT version without a
  re-upload — a superseded version is refused. `ComplianceDocumentExtensions.PublicLiabilityRequirement`
  (5,000,000) and `document.IsBelowPublicLiabilityRequirement` are the one rule: the register's
  "PL cover" column reads in `text-warning` and the `DirectoryComplianceFilter.BelowPublicLiabilityRequirement`
  chip ("Below £5m PL", after the standings on both the Directory and the register) lists the
  companies, but the standing and the pill never change for it — the £5m rule is for big jobs
  only, so a smaller policy is a fact for whoever places the work, not an expired document.
  `PublicLiabilityCoverText` ("£5m", "£2.5m", "£750k") is how the figure reads in a cell.

## The connector mirrors the page — every button the accountant gets, the assistant gets (api)

- **A feature is not done until the MCP connector can do it too** (2026-09-09, the coverage
  audit). Every command that gains an endpoint gains an `AiAction` in the same commit
  (`api/Features/Ai/Tools/Actions`, one partial per area — `CommercialActions.WorkOrderBills`
  is the Work Order bill approve/undo, `ApprovedBy`/`UndoneBy` stamped from the caller); every
  page read the model would need to make that decision is on a read tool (`list_xero_ledger_lines`
  carries `workOrderBill` — the card's orders, proposed slices and the supplier's open orders —
  `workOrderExceptionReason` and `workOrderApproval`; `list_compliance_register` is
  `/directory/compliance`; `preview_xero_contact_push` is the push modal's now/after;
  `search_directory` carries `complianceStanding` and the CIS fields). Pin each batch in
  `AiConnectorTests` (`…_reachTheConnector`) so a rename never drops one. A page-only feature
  is a gap the accountant finds first.
- **A context read hands over the ids its actions take** (2026-09-10, the accountant's ask:
  `get_bid_package_context` listed the tender list as company + status, so
  `decline_bid_package_recipient` could never be given the `recipientId` it wants). Every list a
  `get_*_context` tool returns carries the id the matching action takes — `tenderList[]` has
  `recipientId` + `subcontractorId`, `quotes[]` has `quoteId`, `lineItems[]` has `lineItemId`,
  `linkedDocuments[]` has `drawingId` — and the action's `Notes` name that field
  (`tenderList[].recipientId — never the company name`), not "the recipient list". Code:
  `AiRecordTools.BidPackageContext.cs` + `BidPackageContextReads.cs`; pinned by
  `BidPackageContext_handsOverTheIdsItsActionsTake`.
- **Every lookup a create needs is a read tool, and a name in a note is a promise** (2026-09-15,
  the accountant raising a work order on a supplier not yet in the directory). The assistant
  stalled twice on a lookup, not a write: `add_subcontractor_to_directory`'s notes said
  "tradeIds come from list_trades" and no such tool existed, and `import_xero_supplier` wanted a
  Xero ContactID the model could only get by asking the user to paste it from Xero's URL — the
  contact list was there all along under `list_xero_customers`, named for a different job.
  From where the model sits a dangling name IS "the portal cannot", so it reports exactly that.
  Now: `list_trades` (Masters, all internal roles) and `list_xero_suppliers` (the Import-from-Xero
  modal's own view — ContactID, `alreadyImported`, name-matching directory record — same gate as
  the import) exist, the import/link notes send the model to them, and
  `EveryToolOrActionACatalogueTextNames_exists` fails the build when any tool description, action
  description, note or input schema names a snake_case tool or action that does not resolve. Write
  the read first, then the note that names it — and name a read for the job the model will be
  doing when it needs it (`list_xero_suppliers`, not "use list_xero_customers with a flag").

## The sales invoice raised in Xero from the claim card (api + jpms)

- **Issue IS raise-in-Xero** (2026-09-09, the accountant's ask: Cert 15 was raised, tracked and
  had its PDF attached by hand). The claim card's "Raise in Xero & issue…" and the invoices
  section's menu open `ValuationInvoiceXeroRaiseModal`, which shows the plan first
  (`PreviewValuationInvoiceXeroRaise`) and then runs `RaiseValuationInvoiceInXero`: one
  AUTHORISED ACCREC invoice on **the Xero contact mapped on the project** (`Projects.XeroContactId`
  / `XeroContactName`, picked from Xero's contacts in Project settings → Edit details, beside the
  Sites option; 2026-09-10, after a by-name match created a duplicate contact — the raise never
  matches by name and never creates a contact, and an unmapped or not-found contact is a
  blocker), one line for the invoice's cash `Amount` on `XeroOptions.SalesAccountCode` ("200"
  unless `Xero__SalesAccountCode`), the project's `XeroSiteName` as Sites tracking (no cost code
  on income), Xero reference `Valuation NN` and description `Valuation NN - Payment due as per
  <Month yyyy> valuation report (ex VAT)` numbered from the INVOICE (never the claim), and the
  dates the user gives per call (`InvoiceDate` / `DueDate` on the preview and the command; blank
  = today, and certificate issue date + the contract's `FinalDateForPaymentDays` else Xero's
  sales default — `DueDateNote` says which applied). **The VAT treatment is never assumed**:
  `XeroClient.ResolveSalesContactAsync` — the contact's `AccountsReceivableTaxType`, else their
  most recent ACCREC invoice, else Xero's account default — and the note says which, in the
  preview, the outcome and the audit event.
- **Nothing is lost between Xero and the portal.** `RaiseValuationInvoiceInXeroHandler` plans
  (every blocker named before anything is touched), raises, stamps `XeroInvoiceId` /
  `XeroInvoiceNumber` / `XeroRaisedAt` and SAVES, then attaches the register's newest
  certificate for the claim (`IXeroClient.AttachToInvoiceAsync`, best effort — the outcome's
  `AttachmentError` and a `RaisedInXero` audit event say when it did not), then calls the
  existing `IssueValuationInvoice` handler so the issue rules and certified
  totals are the one implementation. An invoice carrying a Xero id OR number is refused a
  second raise; "Issue without raising in Xero" (`IssueValuationInvoice`, now with an optional
  `XeroInvoiceNumber`) stays for one raised by hand — the number is stamped with `XeroRaisedAt`
  and `XeroInvoiceId` stays null (`ValuationInvoice.IsRaisedInXeroByPortal`) — and
  `RecordValuationInvoiceXeroNumber` (`record_valuation_invoice_xero_number`, "Record Xero
  number…" on the invoice row) back-fills the number on an Issued/Paid row; it refuses one the
  portal raised. Xero never un-raises — a wrong invoice is voided in Xero. Connector:
  `preview_valuation_invoice_xero_raise` + `raise_valuation_invoice_in_xero` (confirm-first,
  `RaisedBy` stamped); the preview tells the model to STOP on a missing contact mapping and to
  fix it in Project settings (or `update_project_details` with the user's yes). Needs the Cost
  Integration app's `accounting.attachments` scope for the PDF.
- **Xero is the home of what has been paid — the portal READS it, never asks** (2026-09-11, the
  MD: "the portal can't recognise when a sales invoice is paid"). `IXeroClient.
  GetSalesInvoicesForContactAsync` is every ACCREC invoice on a contact in ANY status but DELETED
  (the aged receivables read is AUTHORISED/DRAFT/SUBMITTED only, so a PAID invoice was invisible),
  read fresh; `GetSalesInvoiceAsync` is one by id or number. ONE rule,
  `ValuationInvoicePaymentSyncPlanner` (`api/Features/ValuationInvoices/XeroPayments`), plans the
  preview (`PreviewValuationInvoicePaymentSync`), the sync (`SyncValuationInvoicePaymentsFromXero`)
  and the nightly worker's run over every project with a `XeroContactId`: per ISSUED invoice only
  (Raised/Submitted/Approved are not certified; Paid/Cancelled are done), a LINKED one (Xero id,
  else number) that Xero holds PAID (`AmountDue == 0 && AmountPaid > 0`) records the PORTAL net
  `Amount` (the paid convention — Xero's SubTotal is a cross-check carried in the note) on
  `FullyPaidOnDate`; an UNLINKED one is matched as a person would — Xero reference/number naming it
  (`VI-0005`, `Valuation 05`/`Valuation 5`, whole number) outranks the same net to the penny —
  and linked only when the match is unique; a Xero row that fits two portal invoices, or two rows
  that fit one, is Ambiguous and nothing moves. RecordPayment goes through the EXISTING
  `RecordValuationInvoicePayment` handler (then `PaidAt` = Xero's date, same save); a Link is
  audited `RaisedInXero`. Page: the invoices section's toolbar "Sync payments from Xero…"
  (`ValuationInvoicePaymentSyncModal`). Connector: `list_xero_sales_invoices` (how the model
  finds a hand-keyed invoice's number — by net, date, reference; never "give me the INV number"),
  `preview_valuation_invoice_payment_sync`, `sync_valuation_invoice_payments_from_xero`
  (confirm-first, `SyncedBy` stamped). The worker links the planner, the sync handler, the payment
  handler, the audit trail, the mapping and `XeroSalesInvoices.cs` — keep the set complete.

## Drawings are transcribed as they land, and queried as rows (api + worker)

- **Every revision that lands is transcribed** (James, 2026-09-16: "whenever drawings are
  triaged, transcribe them into data so work can be done over them without using a lot of AI
  context up"). The two landings — Document Triage's `FileDocumentAsDrawingHandler` and the
  register's `UploadDrawingRevisionHandler` — call `DrawingExtractionAutoQueue` AFTER their own
  save: a PDF with a stored file gets a Queued extraction row and a queue message; a non-PDF is
  skipped; a queue that is down stamps the row Failed with `NotQueuedMessage` and never fails the
  filing (Extract data on the page is the retry). The three writers of a queued row (the page's
  Extract data, the register's extract-all, the auto-queue) share `DrawingExtractionQueueing`.
  There is no per-project switch — a drawing nobody asked to read is still read.
- **The transcription is rows, not only a blob.** `DrawingDataRows.ReplaceAsync` writes one row
  per figured dimension, callout and closed shape into `DrawingDimensions` / `DrawingCallouts` /
  `DrawingShapes` (migration `AddDrawingDataRows`, script `add-drawing-data-rows.sql`; DrawingId
  and ProjectId denormalised; positions in real-world mm from the sheet's bottom-left corner)
  on every successful run, replacing the revision's rows wholesale exactly as the Revu markup
  rows are, and stamps `DrawingExtractions.RowsWrittenAt`. The structure blob stays as the
  archive and the page's source. A revision extracted before the tables existed has no rows
  and a null stamp: `RebuildDrawingDataRows` (POST `drawings/data-rows/rebuild`, connector
  `rebuild_document_data`) queues a `DrawingExtractionMessage` with `RowsOnly` for each — the
  worker's runner re-transcribes from the structure blob and never re-reads the PDF. Run it once
  after deploying.
- **The connector reads a summary, then queries rows — never the whole read.**
  `get_document_extraction` is the title block, revision table, proven scale, counts, warnings
  and Revu markups only (it returned every dimension, callout and shape until 2026-09-16 — ~67k
  characters for an A0 sheet). `query_document_data` (`AiDeliveryTools.DocumentData`, the
  `DrawingReaders` gate) filters and totals the rows in SQL through `DrawingDataQuery` (kind,
  page, contains, axis, value / area ranges, rectangles, within-N-mm-of-a-point, limit 50 /
  max 200, offset; totals cover every match) over the scope `DrawingDataScope` resolves — one
  revision, a document's newest extracted revision, or the newest extracted revision of every
  document on a project. `extract_document_data` queues a read by hand. A tool that needs a
  sheet's geometry reads rows through these; it never opens the structure blob.

## Reading scans — the assistant's document reader (api)

- **A PDF with no text layer is a scan, kept and read — never refused** (2026-09-09, the
  accountant's ask: Quarry's certificates and the executed contracts are all scans).
  `AiSourceReader.LoadPdf` returns the document with `ScanBytes`; `ScannedPdfReading.FillAsync`
  (called by `AiSourceTools.LoadAsync` on every open) OCRs every page through `IDocumentOcr`
  (`AzureVisionOcr` when `DocumentOcr__Endpoint` / `__ApiKey` are set, `NullDocumentOcr`
  otherwise) and caches the result in `DocumentOcrResults` by the file's SHA-256. OCR text is
  FLAGGED: `AiSourceDocument.TextSource = "ocr"` with `OcrConfidence`, on the manifest, in every
  `read_source` result (`text_source`, `ocr_confidence`, a note to say figures came off a scan)
  — never passed off as an extracted layer.
- **Any page of a scan can be SHOWN as a picture** — `ScannedPdfPages.Render` (pdfium via
  Docnet, 150 dpi, `PngEncoder`), returned through `AiImageToolResult`. `read_source` shows the
  page when asked (`as_image`), when its OCR is missing or below
  `ScannedPdfReading.TrustedConfidence`, and — with no OCR at all — for the first page by
  default; `find_in_source` says so instead of searching nothing. The flat
  `AiAttachmentReader.Extract` (tender extractor) still refuses a scan, naming the route.
- **A refusal names the format and the route that works** (`AiSourceReader.RefusalFor`): .xls →
  Save As .xlsx or upload to the chat; .doc → .docx; .msg → open the email; .zip → name the file.
  Never a bare "unsupported".

## The mailbox the connector reads (api)

- **Every mailbox tool reads ONE mailbox, live — the shared projects mailbox, every folder, Sent
  Items included; nothing is filed into it and nothing is stored in the portal** (2026-09-15, the
  MD's Portal-vs-Outlook note: `search_mailbox` returned six thin rows for 17A and was read as "a
  file store that only shows what has been filed"). `search_mailbox` is Graph `$search` over
  `users/{Mailbox}/messages` (`MailboxGraphClient.SearchAsync`, relevance-ordered, not newest
  first); the queue / discarded / tagged views are `$filter` reads of the same mailbox. A message is
  there because the projects mailbox sent or received it — mail sent from a person's own account
  without the projects mailbox copied never is, and the person's own mailbox is the source for
  that. The rule for "what did we send / what did they send": the portal answers for anything
  sent through it or copied to the projects mailbox; Outlook answers for a personal account.
- **A row carries the envelope, the detail carries the body.** `MailboxMessage` rows (`Summary`
  `$select`) now carry `To` / `Cc` / `SentAt` beside `From` / `ReceivedAt` — null when a read did
  not select them; a preview is never the body, so `get_mailbox_message` (full sanitised body,
  clipped at 30,000 chars and said so) is read before anything is quoted. `MailboxMessageDetail.Bcc`
  is populated only for the mailbox's own sent copies — Graph never reveals a received message's
  Bcc. `ReceivedAt` / `SentAt` are Graph's own stamps; there is no filing timestamp anywhere.
  The three tool descriptions state the scope (`AiMailboxTools.MailboxScope`), pinned by
  `MailboxTools_stateTheirScope_andCarryTheEnvelope`. Never describe the mailbox as a store.

## Loading states (jpms)

- **Never render a figure, a row count or an empty state from a store that has not loaded.** A `0`
  that silently becomes `47` a second later is worse than no number: the reader has already believed
  it. Stores expose `IsLoaded`; read models expose a nullable `Current` (null = no fetch has landed).
- **The pulsing jewel is the only loading mark, and `LoadGate` is the only thing allowed to draw
  it.** `JewelSpinner` is a private part of the gate — a view that renders one directly is a mark
  no ancestor can silence, which is exactly how a screen ends up with two of them. `Panel`
  (`IsLoading`) and `RecordsTable` (`IsLoading`) are gates too; they wrap one.
- **Two shapes, and the question that picks between them is "is anything on screen yet?"**
  - `<LoadGate IsLoading="…">` — the COVER. Nothing to show: the gate holds the region's space and
    puts the jewel in it. `Prominent="true"` for a main panel (roughly a third of the screen or
    more). This is a first load.
  - `<LoadGate Overlay="true" IsLoading="…">` — the REFRESH. A previous answer is on screen and is
    being replaced or acted on (a sort, a pager, a filter, a command in flight): the content stays
    put, an opaque veil takes the clicks, and the mark rides in a chip that sticks to the visible
    slab of the region — a 3,000px queue must not centre its spinner 1,500px down.
- **A gate's `Class` is the REGION's layout, and the region keeps it in both states.** `Class` is
  where the call site puts the margin, the width, the way the region sits in its parent; the gate
  carries it whether the jewel is up or the content is. Layout belonging to the content stays on the
  content. A margin that arrives with the mark and leaves with it is the defect this rule exists to
  stop — the admin stat strip sat flush against the panel below it for exactly that reason.
- **One mark per wait — the gate is relational.** A gate showing its mark cascades a claimed
  `LoadScope`; every gate INSIDE it holds its space and says nothing until the claim lifts. So a
  page cover silences the panels beneath it and a workspace-wide busy overlay silences the list
  refreshing under it, without either knowing what is nested inside. Nothing cascades sideways, so
  the rule left to the call site is: **a screen with nothing to show gates ONCE**, around the region
  the reader is waiting for — `LoadState.UntilAll(a.IsLoaded, b.IsLoaded)` composes the sources.
  Two sibling panels that fill from the same page open share one gate around their container; they
  keep their own only when one of them can load while the other is idle.
- **The whole-page mark belongs to the boot, and lives in `wwwroot/index.html`.** It is a sibling
  overlay of `#app` (not inside it, which is what made Blazor wipe it a beat too early), taken
  down through `BootScreen.DismissAsync` (Services; idempotent, never throws) by whichever of
  three components first has something real to show: `ApprovedSessionGate` for every page behind
  it, `LandingLayout` for the routes outside it, `ReportingErrorBoundary` when a page render has
  failed under it. `js/boot-screen.js` owns the overlay and a 12s failsafe that removes it once
  the app has rendered anything — longer than a cold API session check on purpose, because a
  failsafe firing mid-check swaps the boot jewel for the gate's, the very flicker the overlay
  exists to remove. `js/blazor-start.js` is the manual `Blazor.start` with download retries; when
  boot itself fails it swaps the overlay's "Loading" line for the `#boot-failed` template in
  `index.html`. There is no in-app full-page loader: `ApprovedSessionGate` reads
  `SessionService.IsLoaded` synchronously, so an in-app navigation never flashes a session check
  nobody is waiting on.
  **`index.html` and the assemblies are fetched separately**, so a client can run today's DLLs
  against yesterday's shell (JPMS-4BF13E, 2026-09-08 — a deep link served a cached shell that
  predated `js/boot-screen.js`). `staticwebapp.config.json` sends `no-cache, must-revalidate` on
  every path (`/*`), not only the literal `/index.html`, so deep links served through the
  navigation fallback revalidate too. Any NEW JS global that .NET calls must tolerate being absent
  until every client has refetched the shell.
- **Restraint: one jewel per screen.** A gate is for a REGION that will definitely render something
  and occupies real space. In particular:
  - **Never gate a control.** A filter, a picker, a form field: render it `disabled` with a
    "Loading…" placeholder option instead. That says "not ready" in the control's own language,
    holds the layout still, and cannot be used to make a wrong choice.
  - **Never gate a single line of text.** A count or strapline simply does not render until it is
    known — a line of muted text arriving late is invisible, a spinner in its place is an event.
  - **Never gate a conditional panel.** If the panel only exists when there is something to show,
    its absence during the load is the same as its absence after it; a gate there announces a wait
    for something that usually never arrives.
- **`sessionReady` is not `dataReady`.** A page's auth flag says the session has been checked and
  the user is signed in — nothing more. It gates the RequestAccessView branch and the page chrome
  (title, intro, footnotes, tab nav), which need no data. Every data-bearing panel gates on its own
  sources. Naming the flag `isLoaded` and setting it before the awaits is what produced the
  zero-then-value flash this convention exists to prevent.
- **A failed fetch must open the gate.** Pair each gate with a `dataFailed` flag set in a
  `try/catch` around the awaited queries (and `|| X.LastRefreshFailed(id)` for read models that
  record failure rather than throwing), so the panel says what went wrong instead of pulsing
  forever. The error toast already carries the reference and the detail.
- **Backing fields are nullable, not `Array.Empty<T>()`.** An empty list is a real answer that sums
  to a real-looking zero; `null` is the only honest "not fetched yet". Expose a non-null accessor
  (`Rows => rows ?? Array.Empty<T>()`) for the computations and gate on the nullable field.
- Signals to gate on: `IsLoaded` / `LoadedFor(key)` / `XxxLoadedFor(key)` on stores and read models,
  `AsyncQueryCache.Has(key)` underneath most of them, or `Current is not null` on a read model.
  If the signal you need is missing, add it — do not gate on a proxy that happens to correlate.

## Error reporting (jpms)

- `ErrorReporter` holds the single current error; `ErrorToast` renders it full-width along the top
  of both layouts. One at a time, newest wins.
- Every report carries a short reference (`JPMS-7F3A2C`), the time, the signed-in user, the page,
  the endpoint + status, the server's own message and the stack. The user sees and copies only
  the reference, the time, the page and the sentence (`ErrorReport.ToUserText`); the rest is
  posted by `ClientErrorLog` to `POST /api/client-errors` (`LogClientErrorEndpoint`), which writes
  it to the API's log under the same reference — support looks the reference up, the user never
  reads a stack trace. A 401 gets a "Sign in again" button instead of Copy; it is not logged.
- What reaches the toast: all query failures, command failures **except** 400/409/422 (those are
  validation answers the calling dialog already shows next to the field), and any unhandled
  exception — caught either by `ReportingErrorBoundary` in `App.razor` or by
  `ErrorReportingLoggerProvider`, which watches the framework's own error logging because Blazor
  WASM has no usable `AppDomain.UnhandledException`.
- Blazor's `#blazor-error-ui` strip is last-resort only: it appears when the renderer itself has
  stopped and the only honest option left is Reload.

## Front-end data-loading convention (jpms, Blazor WASM)

- Stores that back synchronous render-time reads (e.g. `ForProject`, `LinesFor`, `PackagesFor`) fetch at most once per key to avoid render → fetch → render loops. Every project tab page must therefore call the store's `Refresh(projectId)` once from `OnInitializedAsync` (never from render) so navigating between tabs revalidates cached data in the background (stale-while-revalidate). Follow this pattern when adding new tabs or stores.
- The router (`App.razor`) uses `KeyedPageRouteView`, which keys each page by its type + route parameter values. Navigating between two URLs of the same route template (e.g. the project header's prev/next arrows) therefore recreates the page and re-runs `OnInitializedAsync`, so the convention above fires there too — pages never need `OnParametersSetAsync` guards for route-value changes.

## Project ordering (jpms)

- **Every list of projects is in one order: live work first.** `ProjectOrdering.InWorkOrder()`
  (contracts/Models) sorts by a coarse four-band rank — Pre-Construction/Procurement/Mobilisation/
  Live Delivery/Close-Out (0) → Defects Period (1) → Lead (2) → Completed (3) — then A–Z by name,
  then by reference. The bands are deliberately coarse so a project moving from Procurement to
  Mobilisation does not jump the list mid-build.
- It is applied **once**, in `ListProjectsVisibleToUserHandler`, so everything reading
  `ProjectListReadModel` inherits it. Callers that narrow the list (`.Where(Stage != Completed)`)
  re-apply `.InWorkOrder()` after the filter; nothing sorts projects by its own rule. If a list
  needs a different order, it needs a reason written next to it.
- **Completed projects are ordered last, not hidden** — except from the side-nav switcher, the
  header's prev/next cycle and the finance overview, which are about work in progress. The
  switcher carries a per-user "Show completed" toggle (`ProjectStageFilter`, decision 2026-08-03)
  that adds completed projects back into the picker, the prev/next cycle and project-scoped
  navigation (`CurrentProjectService.ResolveFor(projects, includeCompleted)`) so their records —
  the valuation report above all — stay reachable after handover; the finance overview ignores
  the toggle. Anywhere costs or history are recoded (Xero allocation, audit trail) the full list
  stays available.
- `SearchSelect` already leads its unfiltered list with a blank entry labelled with its
  `Placeholder`, which *is* the clear/"All …" row. Do not prepend another one — that is what put
  "All projects" in the Xero allocation filter twice.

## Database migrations (prod)

- **Every schema change ships with its apply commands, immediately.** When work adds an EF
  migration, hand the user the exact ready-to-run commands in the same reply as the code — never
  leave the database update as a follow-up. The database is updated *before or with* the deploy
  (additive/expand first), because people are using the system and the deployed code must never
  query columns that don't exist yet. That is exactly what broke sign-in on 2026-07-30.
- **Scoped scripts only — the full idempotent script is permanently broken against prod.**
  `20260702170000_SeparateArchitectsFromClients` embeds raw SQL reading `Clients.ArchitectEmail`,
  which a later step drops; SQL Server fails that batch at *compile time* on any database where
  the column is already gone, so `dotnet ef migrations script --idempotent` (unscoped) can never
  run again. Always generate from the last applied migration:
  1. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"`
  2. `cd api && dotnet ef migrations script <that-id> --idempotent -o migrate.sql`
  3. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b -o migrate.log`
  4. Read `migrate.log` — `-b` stops at the first error, and "completed" printed by an *earlier*
     script run proves nothing about the current one.
- **Raw SQL inside migrations must survive the column being dropped later.** Wrap data-moving SQL
  in `EXEC sp_executesql N'...'` so it compiles only when the guard actually runs; inline raw SQL
  referencing columns that a later migration drops is what poisoned the full script.
- **A hand-written `ALTER COLUMN` must clear what depends on the column first.** A column added
  with a default carries a `DF__…` constraint, and SQL Server refuses to change its type while it
  does (Msg 5074 — `widen-written-text-columns.sql`, 2026-09-24). EF's own generated script drops
  the default; a hand-written one must too — drop it, alter, put the same default back under the
  same name — and drop auto-created statistics on the column the same way. Open such a script
  with `SET XACT_ABORT ON` so a failure leaves the database untouched, never half-migrated.
- One-off data fixes (seeds, role grants, remaps) stay as reviewed scripts under `infra/` /
  `scripts/` run via sqlcmd — they are not EF migrations and must never touch schema.

## Never commit package caches or bundles

`_nuget/`, `_nuget_transfer/`, `*.bundle` and `_copy-nuget-cache.sh` exist only so a sandbox can restore
NuGet offline or receive a branch; they are gitignored. If a build-verification step copies a NuGet cache
into the repo under any other name, add that name to `.gitignore` in the same commit — 400 MB of tarballs
in history made every push take an hour on 2026-09-06.
