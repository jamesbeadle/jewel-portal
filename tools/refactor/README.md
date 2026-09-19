# Refactor pipeline

Measurable, staged refactoring for any repository. Two sentences run it — **"Run the code quality check"** (`.claude/skills/code-quality-check`) and **"Refactor the repo"** (`.claude/skills/refactor-round`) — and everything below is what those skills run. `playbook.md` is the process; `rules.json` is the standard; `audit/` measures compliance; `audit/gate.py` stops regression. This folder is installed and refreshed by the project-process kit (`kit-version` says which version); `rules.json`, `baseline.json`, `baseline-report.md` and `refactor-plan.md` belong to this repository: the kit never overwrites them, and adds to `rules.json` only the blocks a newer kit expects and it lacks.

## Run the code quality check

From the repository root:

```
python3 -m tools.refactor.audit.quality_check .
```

Runs the full audit, scores it, and writes three things: the box at the bottom of `README.md` (the score, with its breakdown, the repository's files by area and the refactoring plan expandable beneath it — only the block between the `code-quality` markers is touched), `refactor-plan.md` here (the steps a refactor of this repository follows, in order, with the measured detail behind the first targets), and `audit-output/`. It changes no source and never moves the baseline.

## The site definition

The quality check also writes `tools/refactor/site-definition.md` — what a user sees at each route, read from the views themselves so it is never stale, in one notation: `a, b` stacked top to bottom · `[ a b ]` side by side · `(3) a` three of them, `(n) a` one per item · `?when: a` shown on a condition · `( a | b )` one or the other · `Widget{ … }` a catalogue widget with its content · `Part=( … )` one of the site's own components opened out · `⚠table→RecordsTable` markup written by hand where the named widget should be. Every component of the site is defined the same way, with how many views use it, and the document opens with the table of what is written by hand, widget by widget. To rewrite it from the last audit alone:

```
python3 -m tools.refactor.audit.site.site_document .
```

It is measured from the `siteDefinition` block of `rules.json`: `viewGlobs` (the view files; `routeGlobs` for a router that names routes by path, as SvelteKit does — Razor's `@page` is read from the file), `catalogue` (the names of the shared widgets every view is meant to compose — empty means *not measured*), `handRolled` (each element a widget owns: `{"element": "table", "ownedBy": "RecordsTable"}`, with `unlessClassPrefixes` and `unlessAttributes` for the classes of markup the rule leaves alone; an owner that is not in the catalogue yet is a widget to build), `ignoredTags`, `horizontalClasses` / `verticalClasses` (how the layout axis is read off a wrapper's classes; a `grid-cols-N` above one is side by side), and `document` (where it is written). A widget's own file is never measured against the rules — it is where the hand-written markup is meant to live — and an owner counts as present anywhere inside it, so a `<table>` inside `RecordsTable` is right. The figure "markup written by hand where a widget should be" scores the *Widget adoption* group, ratchets in the gate, and becomes Pass 1 of the refactoring plan: file-sized steps, the fullest first, each adopting one widget in a few files, ahead of component breakout.

## Run the audit

From the repository root:

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
```

Writes `audit-output/audit.json` (the score, every figure and offender list) and `audit-output/audit-report.md` (the human summary, led by the score, its breakdown, the repository by area and the against-the-baseline table). Add `--fast` between the steps of a round: it carries the duplication figure forward from the baseline instead of running jscpd again, so the reading takes seconds and the score stays comparable. `python3 -m tools.refactor.audit.plan .` rewrites `refactor-plan.md` from the last audit. Requires Python 3.10+. Duplication measurement additionally needs jscpd (`npm install -g jscpd`); without it that one check is skipped and everything else still runs — a report with duplication missing is incomplete, so install it before resetting a baseline.

## Set / update the baseline

```
cp tools/refactor/audit-output/audit.json tools/refactor/baseline.json
```

Do this once per refactor round, at the end, never mid-round, and rewrite `baseline-report.md` in the shape the `refactor-round` skill gives.

## Gate a change

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
```

Exit code 1 when any ratcheted figure is worse than the baseline: files over the length limit, worst file length, over-long functions, else blocks, duplication percentage, explanatory comment lines, inline hex colours, orphan components, orphan functions, long member chains, deep indentation, overlong function names, accessor names that want to be a property, tangled conditions, comparisons to raw literals, files the design patterns predict but which are missing, and markup written by hand where a widget should be. A figure the baseline does not hold yet is not gated until the next baseline. The `end-of-day` and `refactor-round` skills run exactly this; nothing runs on GitHub unless the kit was installed with `--with-ci-gate`.

## Is a round due?

```
tools/refactor/deploys_since_baseline.sh        # or: … 5, for a rhythm of five
```

Counts the commits on the current branch since `baseline.json` was last committed and exits 0 when a round is due.

## Point it at another repository

Everything repository-specific lives in `rules.json`: `sourceGlobs`/`excludeGlobs` for the language mix, `inventory` globs and widget markers for the UI framework, `siteDefinition` for the site definition (above), `styleTokens.markupGlobs` for the styling layer, `prose.indentedFileGlobs` for the files whose indentation depth is measured, `prose.maxMemberChainDepth` for how many property hops one chain may take (two unless set: `a.b.c` passes, `a.b.c.d` does not; method calls are not hops, so fluent query and array pipelines never count; `prose.freeChainPrefixes` lists the roots that are free, `this.`, `base.`, `self.` and `import.meta.` unless set), `areas` for the repository's own grouping of its files (ordered, first match wins), `designPatterns` for where the entities are declared and which predicted files are accepted gaps, `orphans` for the names and globs the framework calls by itself, and `score.elements` for any weight or zero point the repository sets for itself. The kit ships presets (`dotnet-blazor`, `sveltekit`, `generic`) and the bootstrap picks one by what it finds in the repository; edit the copy here freely. The checks are heuristic and language-tolerant: C-family and TypeScript function shapes and names, C# and TypeScript booleans, `//`, `#`, `@*` and `<!--` comments.
