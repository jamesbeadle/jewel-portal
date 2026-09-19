---
name: code-quality-check
description: Publish this repository's code quality — the score in a box at the bottom of README.md with its breakdown expandable beneath it, the refactoring plan, and the site definition refreshed on the way — on a quality/check-<date> branch as a pull request. Use when the person says "Run the code quality check", "run the quality check", "score the repo", "publish the score", or asks for the refactoring plan to be written or refreshed. It changes no source code. Not for a question ("what is the code quality score?" is answered by the audit reading, no branch) and not for the site definition alone ("Run the widget identification", read-only).
---

# The code quality check

One command measures the repository against the coding rules at the top of `CLAUDE.md`, turns every figure into one percentage, writes that percentage in a box at the bottom of `README.md` — with the breakdown, the count of every file in the repository split by area (frontend, backend, API, database, infrastructure…) and the refactoring plan each expandable beneath it — and writes `tools/refactor/refactor-plan.md` — the steps a refactor of this repository follows, in order — and `tools/refactor/site-definition.md`, the site definition: every route as a user sees it, in the widget notation, read from the views. It touches no source file and never resets `tools/refactor/baseline.json`; only a refactor round does that. It runs on its own branch and reaches the default branch by a pull request the person merges — the branch is for the README box, so it is only worth running when the person wants the score published. A question about the score is answered by the reading alone (`python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast` prints it; no branch, no box), and the site definition alone is *"Run the widget identification"* (`widget-identification`, read-only). Neither of those is this skill.

If the repository has no `tools/refactor/`, run the project-process bootstrap first (`bootstrap.sh --kit <a checkout of the kit>`, or the `curl` line in the kit's README when the kit is public) and tell the person it was installed.

## 0. Branch

```
git fetch origin && git switch <default> && git pull --ff-only
git switch -c quality/check-<YYYY-MM-DD>
```

If the working tree holds uncommitted changes, stop and ask the person what they belong to. If today's branch already exists, switch to it and continue there.

## 1. Measure

```
python3 -m tools.refactor.audit.quality_check .
```

It runs the full audit, scores it, writes the plan and places the box, and prints the score. Duplication needs jscpd (`npm install -g jscpd`); install it first, and if it cannot be installed say so — the box will show duplication as *not measured* and lend its weight to the rest, which makes the score incomparable with one that measured it.

What it writes: `README.md` (only the block between the `code-quality` markers, at the bottom; everything else in the file is left as it was), `tools/refactor/refactor-plan.md`, `tools/refactor/site-definition.md` (when `rules.json` names the widget catalogue), and `tools/refactor/audit-output/` (`audit.json` with the offender lists, `audit-report.md`, `refactor-plan.json`).

## 2. Read it before handing it over

Open `tools/refactor/audit-output/audit-report.md`. The score is only as good as the rules it was measured with, so look for what is plainly a mis-measurement rather than a fault in the code: the **areas**: `rules.json` → `areas` is an ordered list of named globs, first match wins, and it should describe *this* repository's clear grouping — if `other` holds more than a handful of files, or an area the person would name (the connector, the mobile app, the functions project) is missing, add or rename areas until the split reads true, and if `rules.json` has no `areas` block yet, re-run the project-process bootstrap, which adds it from the stack's preset, then fit it; an element *not measured* because `rules.json` points nowhere (no `designPatterns.entityGlobs` for this repository's entities, no `inventory` globs for its pages and components, an empty `siteDefinition.catalogue` — fill it with the names of the repository's shared widgets, the components every view is meant to compose, and the site definition is measured from the next run); a `siteDefinition.handRolled` rule that flags a class of markup the widget was never meant to own (exempt it with `unlessClassPrefixes` or `unlessAttributes`, never by dropping the rule); generated or vendored files counted as source; a predicted file that the pattern truly does not need. Fix the globs in `tools/refactor/rules.json`, or list the exception under `designPatterns.acceptedGaps`, run step 1 again, and say in the pull request what was changed and why. Never adjust a weight or a zero point to improve the number.

## 3. Commit, push, open the pull request

```
git add README.md tools/refactor/refactor-plan.md tools/refactor/site-definition.md tools/refactor/audit-output tools/refactor/rules.json
git commit -m "Code quality <score>%: the score box and the refactoring plan"
git push -u origin quality/check-<YYYY-MM-DD>
gh pr create --base <default> --title "QUALITY: <score>% on <YYYY-MM-DD>" --body "<the three group scores, the five lowest elements, the first five steps of the plan>"
```

Without the GitHub CLI, give the person the compare link the push printed. Never merge it.

## 4. Tell the person

Four sentences: how many files the repository holds and how they split by area; the score and how it moved since the last box in the default branch's README; the lowest-scoring group and the two elements dragging it; the first three steps of the plan. Then the pull request. Post the same on the task this session is logged against in Your Business Today.

## How the score is made

Seventeen elements in four groups, each scored from 100% at no offenders down a straight line to 0% at a stated density of offenders, then averaged by weight. **Standard baseline checks** (60): files over the line limit, the worst file, functions over the limit, else blocks, duplication, explanatory comment lines, inline magic values, orphan components and functions, long member chain lines, deeply indented lines, overlong function names. **Design pattern file count** (20): files the patterns predict but are missing, entities outside their expected file count. **Prose** (20): conditions with calls tangled inside calls, conditions compared to a raw literal, accessor names that want to be a property. **Widget adoption** (8): markup written by hand where a catalogue widget should be, against every place a widget is used or should be. An element that cannot be measured lends its weight to the rest. The weights and zero points live in `tools/refactor/audit/score/elements.py` and may be overridden per repository in `rules.json` under `score.elements.<key>`; the box prints them beside every reading, so the number can always be worked out by hand. A score below 100% never displays as 100.0%.

The score is measured, never judged: no opinion of Claude's enters the number, so the same code always scores the same and the box can be trusted across time. Claude's judgement belongs in the round.
