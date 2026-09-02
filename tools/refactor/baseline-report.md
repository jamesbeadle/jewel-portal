# Refactor audit — baseline v16, after round 15

Generated 2026-09-01 from `refactor/round-15`, replacing the round-14 (v15) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 644, totalFiles: 3333, worstFileLines: 736 |
| functionShape | limit: 30, functionsOverLimit: 698, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 488, duplicatedLines: 6207, totalLines: 216128, duplicatedPercentage: 2.87 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1474 |
| comments | explanatoryCommentLines: 13726, filesWithComments: 1811, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2398, deeplyIndentedLines: 2801, overlongLines: 1703, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 231 |

## Round 15 — labour, programme, and the one Apply

The two pane-structured pages the v15 report named, then the first `.cs` at the top of the
table:

- **LabourOverview 785 → 142**: the forecast header, the four views (worker placement with
  the worker's own detail panel, sites, cost codes, weekly sign-off), the settlement schedules
  and the chase list — then the three dialogs (absence, settlement line, the accountant's
  weekly entry), each owning its fields and its save. The month's panes are keyed by month, so
  moving month resets an opened row the way the page used to by hand; the money/day/bar
  formatters joined LabourDisplay. Two site-matching helpers with no caller anywhere in the
  client went with the weekly entry's move.
- **ProjectProgramme 750 → 78**: a tab bar and four panes. ProgrammeWorkbench owns the
  programme outright — reads it, writes every change through one shape and re-reads — and
  hands the page one thing, the delay event a Notice of Delay is raised from;
  ProgrammeGanttChart carries the geometry and the inline task editor, whose save asks the
  workbench whether the write took. ProgrammeClaimsWorkbench, CriticalRfiList and
  RelevantEventsList are the other three panes.
- **TriageQueue.Compose 742 → 311 + 445**: the reply composer and the Apply orchestrator (its
  plan, refusal gauntlet and eleven steps) had sat in one file at one seam; the orchestrator is
  TriageQueue.Apply.cs.
- **Held**: comments 13,727 → 13,726, member chains 2,398 → 2,398 (three typed callbacks and a
  split range read paid for the new `@using static` lines the heuristic counts), overlong
  names 43, functions over 30 lines 700 → 698. **Division signature**: filesOverLimit 638 → 644
  and duplication 2.85% → 2.87% — the new clone pair is the labour page's two bar tables
  (sites, cost codes), which shared their shape on the page and now share it across two files.

## The journey so far

| Figure | 22 Aug (v1) | R10 (v11) | R11 (v12) | R12 (v13) | R13 (v14) | R14 (v15) | R15 (v16) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 954 | 954 | 929 | 837 | 785 | **736** |
| Average page length | 544 | 272 | 272 | 266 | 262 | 246 | **231** |
| Duplication | 4.16% | 2.81% | 2.82% | 2.83% | 2.85% | 2.85% | 2.87% † |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 43 | **43** |
| Functions over 30 lines | — | 700 | 704 | 703 | 702 | 700 | **698** |
| Files over 100 lines | 385 | 618 | 621 | 626 | 629 | 638 | 644 † |

† The division signature: explicit-interface components where page markup stood. Four rounds
of the panel recipe have taken the seven worst pages from 954/929/837/830/802/785/750 to
470/488/302/266/435/142/78; the average page has fallen 41 lines in four rounds.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 517 |
| jpms/Pages/Subcontractors.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |
| api/Features/Ai/Tools/AiSourceTools.cs | 502 |
| api/Features/Ai/Tools/Actions/RequestsActions.cs | 492 |
| jpms/Pages/XeroAllocation.razor | 488 |

## Round 16, named

The finance trio leads — ProfitSummary (736), CashForecast (665), WeeklyCashflow (604) — the
table-heavy pages the CashflowEntryRow and RunningProfitTable families already serve in part;
the recipe is tables-into-row-components rather than panes. ProjectWorkOrders (548) and
Subcontractors (517) are the next pane-shaped pages. On the .cs side, ExcelWorkbookWriter
(589), TriageQueue.Outbox (520) and the AI tool catalogues (AiCommercialTools 560,
AiSourceTools 502, AiSourceReader 518) want the partial-at-a-seam division that Compose/Apply
just had. The worst file is now under 750 for the first time.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
