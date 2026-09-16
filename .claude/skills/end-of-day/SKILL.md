---
name: end-of-day
description: The end-of-day script for a repository on the project process — run the refactor round if one is due, check the connector covers what changed today, and leave a plain-English work log on the day's tasks in Your Business Today. Use when asked to "run end of day", "close the day", "end-of-day", or at the end of a working session on a repository.
---

# End of day

Four things close a day's work on a repository, in this order. All of it happens on the day's `fix/` or `feature/` branch, never on the default branch — if the working tree is on the default branch with changes in it, stop and move them to a branch named for the task first (`git switch -c fix/<slug>` carries uncommitted changes with it).

## 1. The refactor round, if it is due

```
tools/refactor/deploys_since_baseline.sh
```

It counts the commits on the default branch since `tools/refactor/baseline.json` was last committed and says whether a round is due (ten commits unless the person or the project says otherwise). If it is due, finish step 4 for today's branch first, then run the `refactor-round` skill in full on its own `refactor/round-N` branch — a round never rides along on a fix or a feature. If it is not due, still run the audit and the gate:

```
python3 -m tools.refactor.audit.run_audit . --output tools/refactor/audit-output
python3 -m tools.refactor.audit.gate tools/refactor/baseline.json tools/refactor/audit-output/audit.json
```

A failing gate on a day with no round means today's work moved a figure the wrong way. Fix what today introduced if it is small (a comment the name now carries, an `else` that wants an early return, a file that wants dividing at its seam); otherwise name the figure and the file in the summary so tomorrow starts with it. Never reset the baseline outside a round.

## 2. The connector covers what changed

Where the repository has an AI connector or MCP layer — actions and reads a person's Claude calls (in a .NET portal, the `Ai` tools and actions; in a SvelteKit product, `src/lib/server/mcp/actions`) — every command, endpoint or page that changed today must be reachable through it: a new command has its action, a new read has its tool, a changed input has its changed schema, and every id an action takes is handed over by some read. List today's changes (`git diff --stat` and the day's commits), check each against the connector, and add what is missing now if it is a plain mirror of the page; what is more than that, name as a gap in the summary. A page-only feature is one the person's Claude cannot use.

## 3. The plain-English summary on the day's tasks

For each task worked today in Your Business Today (`find_tasks` on the project, or the tasks this session was logged against), post one message a non-technical reader can follow: what changed for them, in their words, one short paragraph — what they can now do, what looks different, what still waits on someone. Then mark the task done if it is done, or leave it in progress and say what is next. Where the day found a gap in the connector or a figure the gate holds against, it goes in that paragraph too, plainly.

## 4. Commit, push and open the pull request

Commit what is left on the branch — one commit per verified step, each message a sentence about the domain — then `git push -u origin <branch>` and open the pull request into the default branch if it is not already open: title the task's title, body the day's summary from step 3 plus what is left. With the GitHub CLI, `gh pr create --base <default> --title "<task title>" --body "<summary>"`; without it, give the person the compare link the push printed. Never merge it; the person reviews and merges. Put the pull request's link in the task's message from step 3.

End by telling the person, in two sentences, which branch and pull request the day's work is on and whether a round ran.
