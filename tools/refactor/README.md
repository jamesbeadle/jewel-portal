# Refactor pipeline

Measurable, staged refactoring for any repository. Two sentences run it — **"Run the code quality check"** (`.claude/skills/code-quality-check`) and **"Refactor the repo"** (`.claude/skills/refactor-round`) — and everything below is what those skills run. `playbook.md` is the process; `rules.json` is the standard; `audit/` measures compliance; `audit/gate.py` stops regression. This folder is installed and refreshed by the project-process kit (`kit-version` says which version); `rules.json`, `baseline.json`, `baseline-report.md` and `refactor-plan.md` belong to this repository: the kit never overwrites them, and adds to `rules.json` only the blocks a newer kit expects and it lacks.

## Run the code quality check

From the repository root:

```
python3 -m tools.refactor.audit.quality_check .
```

Runs the full audit, scores it, and writes three things: the box at the bottom of `README.md` (the score, with its breakdown, the repository's files by area and the refactoring plan expandable beneath it — only the block between the `code-quality` markers is touched), `refactor-plan.md` here (the steps a refactor of this repository follows, in order, with the measured detail behind the first targets), and `audit-output/`. It changes no source and never moves the baseline.

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

Exit code 1 when any ratcheted figure is worse than the baseline: files over the length limit, worst file length, over-long functions, else blocks, duplication percentage, explanatory comment lines, inline hex colours, orphan components, orphan functions, long member chains, deep indentation, overlong function names, accessor names that want to be a property, tangled conditions, comparisons to raw literals, and files the design patterns predict but which are missing. A figure the baseline does not hold yet is not gated until the next baseline. The `end-of-day` and `refactor-round` skills run exactly this; nothing runs on GitHub unless the kit was installed with `--with-ci-gate`.

## Is a round due?

```
tools/refactor/deploys_since_baseline.sh        # or: … 5, for a rhythm of five
```

Counts the commits on the current branch since `baseline.json` was last committed and exits 0 when a round is due.

## Point it at another repository

Everything repository-specific lives in `rules.json`: `sourceGlobs`/`excludeGlobs` for the language mix, `inventory` globs and widget markers for the UI framework, `styleTokens.markupGlobs` for the styling layer, `prose.indentedFileGlobs` for the files whose indentation depth is measured, `prose.maxMemberChainDepth` for how many property hops one chain may take (two unless set: `a.b.c` passes, `a.b.c.d` does not; method calls are not hops, so fluent query and array pipelines never count; `prose.freeChainPrefixes` lists the roots that are free, `this.`, `base.`, `self.` and `import.meta.` unless set), `areas` for the repository's own grouping of its files (ordered, first match wins), `designPatterns` for where the entities are declared and which predicted files are accepted gaps, `orphans` for the names and globs the framework calls by itself, and `score.elements` for any weight or zero point the repository sets for itself. The kit ships presets (`dotnet-blazor`, `sveltekit`, `generic`) and the bootstrap picks one by what it finds in the repository; edit the copy here freely. The checks are heuristic and language-tolerant: C-family and TypeScript function shapes and names, C# and TypeScript booleans, `//`, `#`, `@*` and `<!--` comments.
