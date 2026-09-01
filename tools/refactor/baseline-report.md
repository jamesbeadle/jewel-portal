# Refactor audit — baseline v11, after round 10

Generated 2026-09-01 from `refactor/round-10`, replacing the round-9 (v10) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 618, totalFiles: 3262, worstFileLines: 954 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 468, duplicatedLines: 6040, totalLines: 214587, duplicatedPercentage: 2.81 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1348 |
| comments | explanatoryCommentLines: 13728, filesWithComments: 1744, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2803, overlongLines: 1775, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 272 |

## Round 10 — under three percent

**Duplication fell to 2.81%** — through the 3% floor for the first time (4.16% at the start;
3.20% a round ago). The client got the same cure the api did:

- **The seven Contracts namespaces the pages stamp most went global** (Ai, Commercial, Cqrs,
  Procurement, RecordLinks, Requests, Xero), mirrored into `_Imports.razor` so razor tag
  resolution and C# agree on one list, and 429 files dropped their redundant imports.
  Verified on a clean build: zero RZ10012, all 434 tests green.
- **The weekly plan's rows became CashflowEntryRow and CashflowGroupRow** — the ‹ ↺ › movement
  cell and the supplier group's combined row as Features/WeeklyCashflow components, the group
  rendering its open members through the host's one row template. WeeklyCashflow.razor
  683 → 604.
- **The cashflow twins now share one display module** (CellAmount, Signed, MonthLabel were
  hand-written identically on both pages), and the forecast's SVG balance line became
  ForecastBalanceChart. CashForecast.razor 701 → 665.
- ProjectProgramme was assessed and left whole for a dedicated round: it is one Gantt over
  its geometry partial — dividing it well means shaping chart components, not slicing markup.

## The journey so far

| Figure | 22 Aug (v1) | R5 (v6) | R6 (v7) | R7 (v8) | R8 (v9) | R9 (v10) | R10 (v11) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,101 | 1,029 | 1,016 | 958 | 958 | **954** |
| Average page length | 544 | 283 | 279 | 275 | 275 | 274 | **272** |
| Duplication | 4.16% | 3.41% | 3.39% | 3.41% | 3.44% | 3.20% | **2.81%** |
| Total lines measured | — | — | — | — | 221,094 | 215,666 | **214,587** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | 700 | 699 | 697 | 700 | 703 | **700** |
| Files over 100 lines | 385 | 618 | 622 | 624 | 628 | 621 | **618** |

Every figure moved the right way or held. Files-over-100 is back to its round-4 level with a
codebase that has since gained thirty components; functions-over-30 recovered the three the
DoApplyAll division cost it.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 954 |
| jpms/Pages/XeroAllocation.razor | 929 |
| jpms/Pages/ProjectVariationDetail.razor | 837 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 830 |
| jpms/Pages/ProjectRequestDetail.razor | 802 |
| jpms/Pages/LabourOverview.razor | 785 |
| jpms/Pages/ProjectProgramme.razor | 750 |
| jpms/Pages/TriageQueue.Compose.cs | 742 |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |

## Round 11, named

The two workbench giants keep the top: TriageQueue's email pane and XeroAllocation's allocation
table are the row-family extractions the weekly plan just modelled (CashflowEntryRow is the
template). ProjectProgramme's Gantt earns its dedicated chart-component round, and the api's
remaining pair — JpmsContext (the entity map, divisible by feature area) and the two big
renderers — round out the list.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
