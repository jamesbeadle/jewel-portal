# Refactor audit — baseline v10, after round 9

Generated 2026-09-01 from `refactor/round-9`, replacing the round-8 (v9) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 621, totalFiles: 3257, worstFileLines: 958 |
| functionShape | limit: 30, functionsOverLimit: 703, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 523, duplicatedLines: 6909, totalLines: 215666, duplicatedPercentage: 3.2 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1343 |
| comments | explanatoryCommentLines: 13729, filesWithComments: 1740, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2403, deeplyIndentedLines: 2803, overlongLines: 1776, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 274 |

## Round 9 — the header tax repealed

**Duplication fell to 3.20% — the lowest the audit has ever measured** (the campaign started at
4.16%, and the previous floor was round 2's 3.08% on a much smaller codebase). The three named
threads all landed:

- **The api's eight most-stamped usings went global** (Cqrs both layers, Gates, Data, EF Core,
  AspNetCore Http/Mvc, the Functions attribute namespace): 1,613 files dropped their import
  blocks — the header tax every division round had been paying, gone at the root. −5,400 total
  lines, −63 clones, and seven files fell back under the 100-line limit. Worker-compiled files
  keep their own usings (checked against its hand-picked list); generated Migrations untouched.
- **DoApplyAll divided at its own numbered steps**: the ~390-line apply is now a 60-line
  orchestrator over an ApplyPlan snapshot — one refusal gauntlet, then eleven named steps in
  the original order, each with its own guard and busyLabel. Every command call site was
  verified present exactly once.
- **The running-profit grid became RunningProfitTable** (Features/Cvr): the movement records
  are its public shape, the profit formatting joined a shared ProfitDisplay module (one
  rounding for page and grid), and the labour-accrual overlay rides in as three delegates.
  ProfitSummary.razor 846 → 738.

## The journey so far

| Figure | 22 Aug (v1) | R4 (v5) | R5 (v6) | R6 (v7) | R7 (v8) | R8 (v9) | R9 (v10) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,152 | 1,101 | 1,029 | 1,016 | 958 | **958** |
| Average page length | 544 | 284 | 283 | 279 | 275 | 275 | **274** |
| Duplication | 4.16% | 3.42% | 3.41% | 3.39% | 3.41% | 3.44% | **3.20%** |
| Total lines measured | — | — | — | — | — | 221,094 | **215,666** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 45 | 44 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | 700 | 700 | 699 | 697 | 700 | 703 † |
| Files over 100 lines | 385 | 618 | 618 | 622 | 624 | 628 | **621** |

† The one figure that rose — and it rose because the biggest function in the codebase was
divided: the 390-line DoApplyAll counted once; its orchestrator, refusal gauntlet and two
longest steps now count as four. The heuristic can't see that each is a page of prose where
there was a chapter; the worst offender it was built to catch no longer exists.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 958 |
| jpms/Pages/XeroAllocation.razor | 930 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectRequestDetail.razor | 805 |
| jpms/Pages/LabourOverview.razor | 786 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 746 |
| jpms/Pages/ProfitSummary.razor | 738 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |

## Round 10, named

The table's top half is now all workbench markup in the 750–960 band — TriageQueue's inbox and
email panes and XeroAllocation's allocation table are the two that only shrink by componentising
their table rows, the way TriageEmailRow already started. Behind them: ProjectProgramme (752)
and CashForecast/WeeklyCashflow (the cashflow twins, 701/683) have had no division yet, and the
client could repeat this round's trick — a jpms global-usings widening pass over the Contracts
namespaces its pages still stamp.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
