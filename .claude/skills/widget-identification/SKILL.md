---
name: widget-identification
description: Identify the widgets — read every view of the site and write tools/refactor/site-definition.md, what a user sees at each route in the widget notation, with every place a view writes by hand the markup a catalogue widget should own. Read-only, no branch, no pull request, no score box. Use when the person says "Run the widget identification", "identify the widgets", "where are the widgets written by hand", "which views hand-roll a table", "show me the site definition", "write the site definition", or asks what a user sees at a route. Not for adopting the widgets (that is Pass 1 of "Refactor the repo") and not for the widgets' designs ("widget-design").
---

# The widget identification

One reading of the views, written down: every routed view and every component of the site as a tree of the shared widgets it composes, in the widget notation, and a table of the markup written by hand where a catalogue widget should be — a `<table>` outside `RecordsTable`, an `<input>` outside `FormField`, an `<h2>` where `SectionHeader` should be. The result is `tools/refactor/site-definition.md`; the *"find every view that hand-rolls a table"* question is a search of that file.

It **identifies** the widgets and changes nothing else. It does not adopt them — replacing hand-rolled markup with the widget is *widget adoption*, Pass 1 of the refactor round (*"Refactor the repo"*), and the round takes its steps from this reading. It does not publish a score or rewrite the refactoring plan — that is *"Run the code quality check"*, which also writes this file on its way. And it does not check a widget against its design — that is *"Check the widgets against their designs"* (`widget-design`). Three processes say *widget*; this is the one that only reads.

Because it reads, it needs no branch: run it on whatever branch is checked out, the default branch included, and commit nothing. If the repository has no `tools/refactor/`, run the project-process bootstrap first (`bootstrap.sh --kit <a checkout of the kit>`, or the `curl` line in the kit's README) and tell the person it was installed.

## 1. Read

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output --fast
python3 -m tools.refactor.audit.site.site_document .
```

The first line measures the views (the fast reading carries duplication forward, which this reading does not use, so jscpd is not needed; on a large site it takes a minute or two); the second writes the document from it. If it answers *the site definition is not measured*, `rules.json` → `siteDefinition.catalogue` is empty: propose the names of the repository's shared widgets — the components every view is meant to compose, usually the ones the repository's own `CLAUDE.md` or design-system file lists — show the list to the person, and once they say yes write it into `tools/refactor/rules.json` and read again. Filling the catalogue is the one change this skill ever makes to a tracked file other than its own output, and it goes on a `feature/` branch like any change to `rules.json`.

## 2. Answer

Open `tools/refactor/site-definition.md`. Tell the person, in four sentences: how many routes and components of the site there are and how many pieces of markup are written by hand in how many views; the widgets with the most hand-rolled copies, with their counts; where the document is; and that adopting them is the round's first pass, run by *"Refactor the repo"* — the identification never adopts.

When the question was narrower, answer it from the same reading: *which views hand-roll a table* is every `⚠table→RecordsTable` in the document, or `details.siteDefinition.offenders.handRolled` in `tools/refactor/audit-output/audit.json` filtered by element and owner, with the file and line for each; *what does a user see at `/projects/{id}/valuation`* is that route's section, read back in words. A hand-rolled block that truly is not that widget (a layout grid that happens to be a table, a search box that is not a field) is a rule to exempt in `rules.json` under `siteDefinition.handRolled` (`unlessClassPrefixes`, `unlessAttributes`) — say so, and leave the change for the person to ask for.

## 3. Leave it there

`site-definition.md` and `audit-output/` are refreshed in the working tree, exactly as the next quality check or round would write them; they ride along with whatever is committed next, and the person can `git restore tools/refactor/site-definition.md tools/refactor/audit-output` if they want the tree as it was. No branch, no commit, no pull request, no README box, no plan, no baseline. If the session is logged against a task in Your Business Today, post the four sentences there.
