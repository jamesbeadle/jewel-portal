---
name: refactor-round
description: Refactor this repository — one measured round that follows the repository's refactoring plan in order (widget adoption, then component breakout, then utility functions, then design patterns, then the sweep to zero), behaviour unchanged, and ends by publishing the new code quality score. Use when the person says "Refactor the repo", "refactor the repository", "run a refactor round", when tools/refactor/deploys_since_baseline.sh says a round is due, when a task titled "REFACTOR: round N" is open in Your Business Today, or when the audit gate needs the baseline reset after a round. Not for identifying the widgets or writing the site definition — that is "Run the widget identification" (widget-identification), which reads and changes nothing — and not for a question about the score, which the audit reading answers without a branch. A round is the one process here that changes source; run it only when asked for by one of these names or when it is due.
---

# Refactor the repo: the round

A round is Stage 4 of `tools/refactor/playbook.md`, run once, measured before and after. It never adds behaviour and never changes the schema, and it runs on its own branch: `refactor/round-N` off a fresh default branch, committed step by step, pushed, and handed to the person as a pull request to review and merge. It never commits to the default branch. It is asked for by name, and it is due every N commits on the default branch since the baseline was last committed (`tools/refactor/deploys_since_baseline.sh`, ten unless the project says otherwise); Your Business Today may also have raised `REFACTOR: round N` on the project as the reminder — that task is where the round's record goes.

Read, in this order, before the first change: the coding rules at the top of `CLAUDE.md`, `tools/refactor/playbook.md` (Stage 4 is the method this skill applies), `tools/refactor/rules.json`, then `tools/refactor/baseline-report.md`. If the repository has no `tools/refactor/`, run the project-process bootstrap first (`bootstrap.sh --kit <a checkout of the kit>`, or the `curl` line in the kit's README when the kit is public) and tell the person it was installed.

## 0. Branch

```
git fetch origin && git switch <default> && git pull --ff-only
git switch -c refactor/round-N
```

If the working tree holds uncommitted changes, stop and ask the person what they belong to — a round starts clean.

## 1. Baseline first, then the plan

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
python3 -m tools.refactor.audit.plan .
```

The gate must pass before anything is touched. If it fails, the codebase has drifted since the last baseline: adopt the current reading as the new baseline first, write why in the report ("adopted from drift: <what grew>"), and commit that as the branch's first commit, `Baseline vN: adopted from drift` — a round always starts from a green gate. The report you start from is the round's *before*. Duplication needs jscpd (`npm install -g jscpd`); a reading without it is incomplete, so install it before the first audit.

Before taking a step, read the standing of the designs from the site definition (`tools/refactor/site-definition.md`, *The widgets' designs*) and put it in the report's first lines: whether a design index exists, when the site was last checked against the brand and the widgets against their sheets — or *never*, or *not set up* — and the sentence that runs each (`widget-design`: *"Check the site against the brand"*, *"Check the widgets against their designs"*). The round reports it and does not run it: a design check is a judgement and may change a widget's look, which a round never does.

`tools/refactor/refactor-plan.md` is now current, and it is what the round goes off: every step a refactor of this repository needs, numbered, in the order they are to be done, with the measured detail behind the first targets — the component-sized blocks of each long view and their line ranges, the functions that are used nowhere but inside each block, the functions other files import from a view, the functions whose body is repeated word for word in more than one file, the widgets whose markup views still write by hand and where, the files the design patterns predict but which are missing, the entities outside their expected file count. **Do not spend the round rediscovering any of that.** Take the next five to ten steps from the top, in order. Do not jump ahead to a later pass while an earlier one still has steps, and do not pick by interest. **The round never asks which steps to take**: the plan's order is the answer, and a round that stops to ask is not a process. The only question a round may put to the person is the one the step itself cannot settle — a check that will not build in this environment, a step that fails twice — and it is reported, not asked. Skip a step only when the repository's own checks cannot build that file in this environment, and say so in the report.

## 2. The passes

Each step is one of five kinds, and each kind has its own method. The playbook's Stage 4 has the reasoning; this is the procedure.

**Pass 1 — Widget adoption** (*Adopt `<widget>` in N files that write its markup by hand*, *Build, from the best of these, then adopt `<widget>`*). Adoption is the round's; *identification* — the reading that finds the hand-rolled markup and writes the site definition — is the `widget-identification` skill, and the round consumes its result rather than being asked for in its place. It comes first because it is the cheapest shrink there is: the widget already exists, and every hand-rolled copy it replaces is code gone before anything is broken out. The step names one catalogue widget and a few files where a view writes its markup by hand; `audit.json` lists each place with its line under `details.siteDefinition.offenders.handRolled`, and `tools/refactor/site-definition.md` shows the route it reaches. Open the widget once to see what it takes, then replace each hand-rolled block with it, moving only what the widget's parameters carry — the same rows, the same labels, the same events, nothing restyled. Where the step says *build*, the widget the finding names does not exist yet: make it from the best of the hand-rolled copies, put it beside its siblings in the catalogue, add its name to `siteDefinition.catalogue` in `tools/refactor/rules.json`, and only then adopt it. A hand-rolled block that truly is not that widget (a layout grid that happens to be a table, a search box that is not a field) is left alone and said so in the report — the rule in `rules.json` under `siteDefinition.handRolled` can carry an `unlessClassPrefixes` or `unlessAttributes` exemption when the exception is a class of markup, never one file.

**Pass 2 — Component breakout** (*Break `<view>` into components*). Read the view once, whole. For each block the plan names — and any seam you can see that it could not measure — name the block for its one purpose; create the component; move the markup **and the functions the plan lists as moving with it, and the state only they touch**; declare what the component needs as explicit, typed parameters and what it announces as named events; replace the block in the parent with the component. Use the framework's own wiring and nothing else. A block that needs more than about six parameters is cut at the wrong seam — take the larger block around it or the smaller ones inside it. A new component that is itself over the limit is broken out again before the step is committed. The step is done when the view is under the limit, or every block the plan named is out.

**Pass 3 — Utility function identification** (*Give `<function>` one home*, *Extract what `<handler>` shares*, *Decide what `<function>` is*, *Remove what nothing calls*). Ask of the function, in order: is this the right home — could another component or file in this design pattern use it, or does one already? (A shared *name* is not a shared function: sixteen components each with their own `onSubmit` are sixteen functions, each specific to its form. The plan does not decide this from a list of framework names — it reads the code: a function bound from its own file's markup, or from the template kept beside it, or invoked through an attribute or an `override`, is the framework's and stays where it is; one that another file imports is a utility. Where such a body is long enough that matching cannot be coincidence, the plan asks for the body to move to a utility each handler calls, not for the handler to move. **A *Decide what `<function>` is* step is the plan saying the evidence does not settle it and you must — this is the judgement the round exists to make.** Open one declaration and find what calls it. If the framework binds the name, add that name to `frameworkHandlers.names` in `rules.json` and change no code: the decision is recorded and no later round asks again. If it is one function wearing a familiar name, give it a home and point every caller at it. Either way the round says in the commit which it was and why.) Is it masking a bad implementation of something the framework should be handling? Would a reader expect it in the standard implementation of this kind of view? Then act on the answer: a utility moves to a named, focused module (never `utils`, `helpers`, `common` or `misc`), abstracted only as far as that module's purpose needs, and every other declaration of it is deleted and its callers pointed at the one; a function masking the framework is removed by doing it the framework's way; a function with no justification is inlined or deleted. Nothing is deleted until every caller across the repository is confirmed gone (search for the name, including in markup and in strings the framework resolves), and the commit says so.

**Pass 4 — Design pattern identification and modification** (*Complete the pattern*, *Bring `<entity>` into range*, *Divide `<file>`*). For a predicted file: open two subjects that already follow the pattern to see exactly what that role holds, then find where the subject that lacks it is doing the same job — inline in its handler, in an endpoint, in a catch-all service — and move that code to the predicted path, in the same shape as its siblings. If the subject truly has no such job, add the predicted path to `designPatterns.acceptedGaps` in `tools/refactor/rules.json` with the reason in the commit message; never create an empty file to satisfy the count. For an entity out of range: too few files means its work is piled up — find the long files carrying it and divide them by the pattern's roles; too many means a pattern is being repeated by hand — find the repetition and give it one home. For a long backend file: divide it into the units its pattern names, each function one purpose, the parent a short sequence of named steps. The first time a pattern is relied on, name it in `docs/refactor/design-patterns.md`: the roles, what each holds, and the sentence the audit printed for it.

**Pass 5 — The sweep to zero.** Take the element the step names and clear its offenders from `audit.json`, file by file: an `else` becomes an early return; a condition with calls inside calls gets a named local before it, so the line reads as a sentence (`const apple = getApple(1)` then `if (apple.colour === Colours.red)`); a raw literal in a comparison becomes a named constant; `getAppleColour()` becomes a property of the object; a member chain moves onto the type that owns the data, becoming a local only where that type is not yours to change; deeply indented code becomes a named function called from the block; a comment comes off because the name now says it.

One element is never swept: **Input validation** (doors that write without checking their input against the columns). Adding a check refuses a value that was accepted yesterday, which is a change of behaviour, and a round never changes behaviour — so the plan carries no step for it and the round does not add validators. The gate still holds it: a round must not raise `inputValidation.unvalidatedDoors` or `inputValidation.looserLimits`, and the fixes go on `fix/` branches the person asks for (*"Run the input validation check"* shows where).

## 3. How every step goes

1. Read the target file and the files the plan names for it — not the repository.
2. **Move first, rename second, and rewrite nothing.** Code moves verbatim into its new home; names are corrected once it is there; logic is not edited in a refactor step. Every reset, guard and seeded draft moves with the code that owned it.
3. Never invent a name to make a split possible: no `Part2`, `Section`, `Helpers`, `Utils`, `Manager`. If the new file cannot be named for one purpose, the split is wrong — find the real seam.
4. Verify: the repository's typecheck and lint, and the tests that cover the touched files, after every step; the full suite and the build at the end of each pass and at the end of the round. Fix what fails before moving on.
5. Re-measure with the fast reading and hold the gate:
   ```
   python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast
   python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
   ```
6. Commit the step on the branch before starting the next, its message a sentence stating what became what ("The labour overview's three dialogs become components", "ProjectProgramme 750 → 78: a tab bar and four panes") — the messages become the report's round section.
7. **Two attempts, then put it back.** A step that does not pass its checks after two attempts is restored (`git restore .` and remove the new files), named in the report with what stopped it, and the round moves to the next step. A forced step is how a round produces bad results and loses an afternoon.

Hold every ratcheted figure: a comment the new filename now carries comes off; a new `else` becomes an early return; a member chain moves onto the type that owns the data. Accept, with an honest note, only the division signature — `filesOverLimit`, `functionsOverLimit` and the pattern predictions rising when one 700-line file becomes eight 100-line ones — and inspect any clone pair before claiming duplication moved either way. The standard figures reach zero together, not one at a time: the worst file cannot keep falling while files remain over the limit, and the overflow from one figure into another is yours to recognise and put in order.

Stop at a sensible seam rather than mid-step: the plan is measured again at the end, so the next round picks up exactly where this one stopped.

## 4. Baseline last, then the score

At the end, never mid-round:

```
python3 -m tools.refactor.audit.quality_check .
cp tools/refactor/audit-output/audit.json tools/refactor/baseline.json
```

The quality check runs the full audit, writes the new score box at the bottom of `README.md` and rewrites `tools/refactor/refactor-plan.md` from what is left. Then rewrite `tools/refactor/baseline-report.md` in this shape and commit it with `baseline.json`, `README.md`, the plan and `audit-output/` as the round's last commit, "Baseline vN: <the round's name>":

```
# Refactor audit — baseline vN, after round N-1

Generated <date> from <branch>, replacing the vN-1 baseline.

## Headline
<code quality score before → after, then the audit report's own headline: files over the
limit, of how many, and the worst file>

## Code quality score
<the audit report's score table, copied exactly>

## Summary
<the audit report's Summary table, copied exactly>

## Round N-1 — <the round's name>
Prose, then one bullet per plan step worked, in the plan's words: **File before → after**:
what became what, where the state went, what was deleted and why it was safe. A **Put back**
bullet for every step restored after two attempts, with what stopped it. A **Held** bullet
listing every held figure. A **Division signature** bullet for accepted drifts, naming the
file count or the clone pair honestly.

## The journey so far
| Figure | v1 | … | vN |   — code quality score, worst file, average page length, duplication,
else blocks, functions over the limit, files over the limit; bold when improved or held,
† when accepted.

## Worst files by length
<top 15 from fileLength.offenders>

## Next round, named
The first five steps of the new tools/refactor/refactor-plan.md, and one sentence on what the
worst file is now.
```

## 5. Hand it back

`git push -u origin refactor/round-N` and open the pull request into the default branch, titled `REFACTOR: round N — <the round's name>`, its body the report's Headline and round section (`gh pr create --base <default> …`, or the compare link the push printed). Never merge it. Tell the person the before → after score and headline in two sentences and give them the pull request. Post the headline and the report's round section on the round's task in Your Business Today (the open `REFACTOR: round N` task if one was raised, else the task this session is logged against), with the pull request's link, and mark it done. The task carries the numbers — they are the argument.
