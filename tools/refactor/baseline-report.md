# Refactor audit — baseline v12, after round 11

Generated 2026-09-01 from `refactor/round-11`, replacing the round-10 (v11) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 621, totalFiles: 3267, worstFileLines: 954 |
| functionShape | limit: 30, functionsOverLimit: 704, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 469, duplicatedLines: 6044, totalLines: 214643, duplicatedPercentage: 2.82 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1348 |
| comments | explanatoryCommentLines: 13728, filesWithComments: 1748, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2803, overlongLines: 1775, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 272 |

## Round 11 — the api's last giants

The three files that had sat in the api's tail since the audit began all divided, clearing
every remaining 500+ line file out of `api/Data` and `api/Features/*/Documents`:

- **JpmsContext divided**: the DbSet catalogue (grouped by feature area, as its comments always
  were) keeps the context file at 228 lines; OnModelCreating — the composite keys and the
  pinned read-path indexes with their production history — is a 370-line Model partial. The
  model text is unchanged, so the migrations snapshot is untouched, and the worker's compile
  list gained the new file at write time (the round-1 lesson, now a habit).
- **ValuationReportSnapshotRenderer (577 → 98) and RequestDocumentRenderer (523 → 61)** divided
  at their own Sections/Helpers markers — WorkOrderPoRenderer's round-8 recipe, third and
  fourth applications. RequestDocumentRenderer is worker-compiled; its partials joined the list.
- **Assessed in depth, deliberately deferred**: XeroAllocation's allocation row is one mega-row
  with four per-status variants over ~15 page members, and TriageQueue's Tagged panel leans on
  the page's whole linking vocabulary. Both need a designed row-coordinator pass — round 12's
  centrepiece, not a forced afternoon extraction.

## The journey so far

| Figure | 22 Aug (v1) | R6 (v7) | R7 (v8) | R8 (v9) | R9 (v10) | R10 (v11) | R11 (v12) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,029 | 1,016 | 958 | 958 | 954 | **954** |
| Average page length | 544 | 279 | 275 | 275 | 274 | 272 | **272** |
| Duplication | 4.16% | 3.39% | 3.41% | 3.44% | 3.20% | 2.81% | **2.82%** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | 699 | 697 | 700 | 703 | 700 | 704 † |
| Files over 100 lines | 385 | 622 | 624 | 628 | 621 | 618 | 621 † |

† The division signature, as every round a giant splits: five new 100–400-line partials where
three 500–600-line files stood, and the heuristic re-counting what it can now see. The api no
longer has a single file over 500 lines outside the generated migrations.

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
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |

## Round 12, named

The worst-files table is now client markup top to bottom. Round 12 is the **workbench row
round**, designed rather than sliced: XeroAllocation's allocation row wants a row coordinator
(the per-line picks the RowCoding partial already holds, handed to a row component the way
CashflowEntryRow models it), and TriageQueue's Tagged panel wants the page's linking vocabulary
(RecordTypeOptions and its label family) promoted to a Features/Triage module first — the move
that unblocked the compose pane in round 5. ProjectProgramme's Gantt chart components and
ExcelWorkbookWriter round out the list.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
