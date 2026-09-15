---
name: refactor-round
description: Run one measured refactor round on this repository — baseline first, worst files first, behaviour unchanged, baseline last. Use when a task titled "REFACTOR: round N" is claimed from Your Business Today, when asked to "run a refactor round", or when the audit gate needs the baseline reset after a round of extraction.
---

# The refactor round

A round is Stage 4 of `tools/refactor/playbook.md`: the extraction loop, run once, measured before and after. It never adds behaviour and never changes the schema. It is the unit Your Business Today raises by itself every N deploys of the default branch; the Builder claims the task and runs this skill. A person can run it too.

Read, in this order, before the first change: the coding rules at the top of `CLAUDE.md`, `tools/refactor/playbook.md`, `tools/refactor/rules.json`, then `tools/refactor/baseline-report.md` (the current report — its "Next round, named" section is where this round starts). If the repository has no `tools/refactor/`, bootstrap the kit first (`curl -fsSL https://raw.githubusercontent.com/jamesbeadle/project-process/main/bootstrap.sh | bash`) and commit what it installs as the round's first commit.

## 1. Baseline first

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
```

The gate must pass before anything is touched. If it fails, the codebase has drifted since the last baseline: adopt the current reading as the new baseline in its own commit ("Baseline vN adopted: <what drifted>") and say so in the report — a round always starts from a green gate. The report you start from is the round's *before*.

Duplication needs jscpd (`npm install -g jscpd`). A reading without it is incomplete; install it before the first audit.

## 2. The extraction loop, worst file first

Take the `fileLength` offender list. Skip anything the repository's own checks cannot build in this environment and say so. Then, for each target:

1. Read the whole file, not the offender line. Name each section that has one purpose.
2. Divide at the seam into two named things, repeatedly: page markup into components with explicit parameters; logic into partials or modules named for the concern; a table into its row family; a long function into a short sequence of named steps. Never invent an abstraction to make a split possible — if the split needs one, the split is wrong.
3. Behaviour does not change. Same rendered output, same events, same refusals; every reset, guard and seeded draft moves with the code that owned it. Nothing is deleted unless every caller across the repository is gone, and the commit says so.
4. Run the repository's checks (typecheck, lint, tests, build) and fix what fails. Then re-run the audit and the gate.
5. Commit one verified step at a time, titled as prose stating what became what ("The labour overview's three dialogs become components"), the figure in the body ("ProjectProgramme 750 → 78: a tab bar and four panes").

Hold every ratcheted figure: a comment the new filename now carries comes off; a new `else` becomes an early return; a member chain becomes a local. Accept, with an honest note, only the division signature — `filesOverLimit` and `functionsOverLimit` rising when one 700-line file becomes eight 100-line ones — and inspect any clone pair before claiming duplication moved either way.

A round is 5–10 commits. Stop at a sensible seam rather than mid-extraction: the next round picks up from the report.

## 3. Baseline last

At the end, never mid-round:

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
cp tools/refactor/audit-output/audit.json tools/refactor/baseline.json
```

Rewrite `tools/refactor/baseline-report.md` in this shape and commit it with `baseline.json` and `audit-output/` as the round's last commit, titled "Baseline vN: <the round's name>":

```
# Refactor audit — baseline vN, after round N-1

Generated <date> from <branch>, replacing the vN-1 baseline.

## Headline
<the audit report's own headline: files over the limit, of how many, and the worst file>

## Summary
<the audit report's Summary table, copied exactly>

## Round N-1 — <the round's name>
Prose, then one bullet per file worked: **File before → after**: what became what, where
the state went, what was deleted and why it was safe. A **Held** bullet listing every held
figure. A **Division signature** bullet for accepted drifts, naming the file count or the
clone pair honestly.

## The journey so far
| Figure | v1 | … | vN |   — worst file, average page length, duplication, else blocks,
functions over the limit, files over the limit; bold when improved or held, † when accepted.

## Worst files by length
<top 15 from fileLength.offenders>

## Next round, named
The next targets by name and line count, with the division each wants, and one sentence on
what the worst file is now.
```

## 4. Hand it back

Under the Builder: push the branch, open the pull request with the round's before → after headline and the report's round section as its body, arm auto-merge, and call `report_build`. By hand: the same pull request, and post the headline on the round's task in Your Business Today. Either way, the task carries the numbers — they are the argument.
