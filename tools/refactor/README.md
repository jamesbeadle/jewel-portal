# Refactor pipeline

Measurable, staged refactoring for any repository. `playbook.md` is the process; `rules.json` is the standard; `audit/` measures compliance; `gate.py` stops regression.

## Run the audit

```
cd tools/refactor
python3 -m audit.run_audit ../..            # writes audit-output/audit.json + audit-report.md
```

Requires Python 3.10+. Duplication measurement additionally needs jscpd (`npm install -g jscpd`); without it that one check is skipped and everything else still runs.

## Set / update the baseline

```
cp audit-output/audit.json baseline.json
```

## Gate a change (CI or local)

```
python3 -m audit.run_audit ../.. --output audit-output
python3 -m audit.gate baseline.json audit-output/audit.json
```

Exit code 1 when any ratcheted figure is worse than the baseline: files over the length limit, worst file length, over-long functions, else blocks, duplication percentage, explanatory comment lines, inline hex colours, orphan components.

## Point it at another repository

Edit `rules.json`: `sourceGlobs`/`excludeGlobs` for the language mix, `inventory` globs and widget markers for the UI framework, `styleTokens.markupGlobs` for the styling layer. The checks themselves are language-heuristic and need no changes for C-family codebases.
