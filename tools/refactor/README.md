# Refactor pipeline

Measurable, staged refactoring for any repository. `playbook.md` is the process; `rules.json` is the standard; `audit/` measures compliance; `audit/gate.py` stops regression. This folder is installed and refreshed by the project-process kit (`kit-version` says which version); `rules.json`, `baseline.json` and `baseline-report.md` belong to this repository and are never overwritten by the kit.

## Run the audit

From the repository root:

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
```

Writes `audit-output/audit.json` (every figure and offender list) and `audit-output/audit-report.md` (the human summary, led by the headline and the against-the-baseline table). Requires Python 3.10+. Duplication measurement additionally needs jscpd (`npm install -g jscpd`); without it that one check is skipped and everything else still runs — a report with duplication missing is incomplete, so install it before resetting a baseline.

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

Exit code 1 when any ratcheted figure is worse than the baseline: files over the length limit, worst file length, over-long functions, else blocks, duplication percentage, explanatory comment lines, inline hex colours, orphan components, long member chains, deep indentation, overlong function names. The `end-of-day` and `refactor-round` skills run exactly this; nothing runs on GitHub unless the kit was installed with `--with-ci-gate`.

## Is a round due?

```
tools/refactor/deploys_since_baseline.sh        # or: … 5, for a rhythm of five
```

Counts the commits on the current branch since `baseline.json` was last committed and exits 0 when a round is due.

## Point it at another repository

Everything repository-specific lives in `rules.json`: `sourceGlobs`/`excludeGlobs` for the language mix, `inventory` globs and widget markers for the UI framework, `styleTokens.markupGlobs` for the styling layer, `prose.indentedFileGlobs` for the files whose indentation depth is measured. The kit ships presets (`dotnet-blazor`, `sveltekit`, `generic`) and the bootstrap picks one by what it finds in the repository; edit the copy here freely. The checks are heuristic and language-tolerant: C-family and TypeScript function shapes and names, C# and TypeScript booleans, `//`, `#`, `@*` and `<!--` comments.
