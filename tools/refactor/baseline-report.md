# Refactor audit — baseline v9, after round 8

Generated 2026-09-01 from `refactor/round-8`, replacing the round-7 (v8) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 628, totalFiles: 3254, worstFileLines: 958 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 586, duplicatedLines: 7597, totalLines: 221094, duplicatedPercentage: 3.44 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1334 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1738, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2819, overlongLines: 1783, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 275 |

## Round 8 — the backend round

**No file in the codebase is over 1,000 lines any more.** The api long tail, untouched since
round 4, divided:

- **AiRecordTools → three tool groups** (Correspondence, Contexts, Directory), concatenated by
  Build() in the original order — the round-4 catalogue recipe. TryMapRecordType stays in the
  core with the shared serialisers. (Not in the worker's compile list — checked, as was every
  file this round.)
- **WorkOrderPoRenderer divided at its own markers**: Render with the class doc, the ten
  document sections, and the styling helpers — 623 → 65 + 448 + 135.
- **SendMailboxEmailHandler's helpers divided by concern**: the ten-step HandleAsync keeps its
  file with the rollback that guards it; attachment resolution and envelope/body shaping moved
  to partials — 607 → 457 core.
- **TriageQueue gave up its two standing panels**: UnassignedRequestsPanel and RecentTriageFold
  (with its href map and relative stamps) joined the Queue component family — the page fell
  under 1,000 (958) and its code-behind to 425.
- **Assessed, deliberately left**: ProfitSummary's two report tables want view-model design
  before componentising (named below); ProjectVariationDetail is already built around the
  thrice-reused VariationApprovePanel — no shallow cut worth making.

## The journey so far

| Figure | 22 Aug (v1) | R3 (v4) | R4 (v5) | R5 (v6) | R6 (v7) | R7 (v8) | R8 (v9) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,152 | 1,152 | 1,101 | 1,029 | 1,016 | **958** |
| Average page length | 544 | 333 | 284 | 283 | 279 | 275 | **275** |
| Duplication | 4.16% | 3.25% | 3.42% | 3.41% | 3.39% | 3.41% | **3.44%** ‡ |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 45 | 45 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | — | 700 | 700 | 699 | 697 | **700** † |
| Files over 100 lines | 385 | 570 | 618 | 618 | 622 | 624 | 628 † |

† The catalogue-division artifacts, both: three group methods now count where one Build()
counted once (the tools inside are unchanged), and five new partials cleared 100 lines while
their parents shrank by more. Same accounting as round 4's action-catalogue splits.

‡ Header tax: five new api partials each carry the feature's using block. The AI tool partials
had theirs trimmed to what they use; an api-side global-usings pass (the round-4 client fix)
is the structural cure, queued below.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 958 |
| jpms/Pages/XeroAllocation.razor | 930 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectRequestDetail.razor | 805 |
| jpms/Pages/LabourOverview.razor | 786 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 562 |

## Round 9, named

Three threads: an **api global-usings pass** to stop the header tax the division rounds keep
paying (the exact fix the client got in round 4); **ProfitSummary's view models** — shape the
bridge and cumulative tables' row models in Figures/Bridge/Cumulative so the two tables can
become components fed by data rather than by twenty helpers; and **TriageQueue.Compose.cs's
DoApplyAll** — the one ~390-line function driving the functions-over-30 figure, which divides
naturally at its own numbered step markers.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
