# Refactor audit — baseline v6, after round 5

Generated 2026-09-01 from `refactor/round-5`, replacing the round-4 (v5) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 618, totalFiles: 3222, worstFileLines: 1101 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 583, duplicatedLines: 7523, totalLines: 220560, duplicatedPercentage: 3.41 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1325 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1712, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2821, overlongLines: 1785, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 283 |

## Round 5 — the TriageQueue compose finale

TriageQueue is no longer the worst file in the codebase — for the first time since the audit
began. The round moved its whole compose story out of the page:

- **NewEmailComposerPane** — the Compose window is now a self-contained component owning its
  envelope, body, attachments, the file-to-a-record picks and the send itself. The page keeps
  two lines of wiring (close the window, show the outcome banner) in place of a 15-field state
  block, a partial file, and an 80-line fragment.
- **ReplyComposerForm** — the inline reply/forward form became a pure-view component. The
  fields deliberately stay page-owned: triage parks a half-written reply per email
  (ParkedTriage) and the bar's one Send is what actually sends, so the component renders the
  form and hands every keystroke straight back.
- **TriageDecisionRow** — the bar's blank-until-answered Yes/No pill pair was stamped out
  three times (Relevant Event, Entire thread, Use existing tags); it is now one component
  owning the styling and the no-un-answer rule.
- The triage bar itself was assessed and deliberately left on the page: its sentences and
  gates are the page's own orchestration voice — a twenty-parameter view component would
  relocate it without dividing it.
- **A live deploy failure was fixed mid-round** (also delivered for `main` as its own commit):
  round 4's division of XeroClient.Reads left the two new partials off the worker's hand-picked
  compile list, so CI's worker publish failed with CS0535. Both files are on the list, and every
  IXeroClient member's implementation is verified present in the worker's compile set.

## The journey so far

| Figure | 22 Aug (v1) | R1 (v2) | R2 (v3) | R3 (v4) | R4 (v5) | R5 (v6) |
| --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,471 | 1,399 | 1,152 | 1,152 | **1,101** |
| TriageQueue.razor | 4,961 | 1,471 | 1,399 | 1,152 | 1,152 | **1,016** |
| Average page length | 544 | 390 | 356 | 333 | 284 | **283** |
| Duplication | 4.16% | 4.03% | 3.08% | 3.25% | 3.42% | **3.41%** |
| `else` blocks | 1,087 | 1,184 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | — | 45 | 45 | 45 | **44** |
| Files over 100 lines | 385 | 520 | 552 | 570 | 618 | **618** † |

† Flat for the first time: round 5's extractions moved code into three new components without
any of them crossing the limit — the figure's long climb (mass leaving giants for mid-size
partials) has crested; the long tail of dividing those partials below 100 is what remains.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1101 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/TriageQueue.razor | 1016 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 686 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |

## Round 6, named

The table's top is now the two workbench twins: **ProjectBidPackageInviteDetail** (1,101) and
**XeroAllocation** (1,095 + a 686-line code-behind). Both are section-shaped pages whose markup
only shrinks by componentising sections, and both already lean on the shared mail widgets — so
round 6 divides them the TriageQueue way: find each page's two or three natural sections, give
each a component or a concern partial, and let any shared table/panel shapes surface as they
did for the compose feature.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
