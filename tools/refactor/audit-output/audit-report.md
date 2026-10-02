# Refactor audit

Generated 2026-10-02 12:18 UTC.

## Headline

**Code quality score 78.5%.** **793 of 4,886 source files are over the 100-line limit (16.2%)**; the worst file is 551 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **70.2%** | **60** | |
| Files over the line limit | 793 in 4886 files | 67.5% | 10 | 50% of files |
| Worst file, in limits over | 4.51 | 49.9% | 5 | 9 |
| Functions over the line limit | 836 in 9630 functions | 65.3% | 8 | 25% of functions |
| Else blocks | 1181 in 13291 branches | 82.2% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16311 in 311.07 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 135 in 311.07 thousand lines | 97.8% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 9780 components and functions | 97.2% | 4 | 10% of components and functions |
| Long member chain lines | 4117 in 311.07 thousand lines | 55.9% | 4 | 30 per thousand lines |
| Deeply indented lines | 3064 in 311.07 thousand lines | 67.2% | 4 | 30 per thousand lines |
| Overlong function names | 58 in 9630 functions | 94.0% | 4 | 10% of functions |
| **Design pattern file count** | | **92.4%** | **20** | |
| Files the patterns predict but are missing | 187 in 2468 predicted files | 84.8% | 10 | 50% of predicted files |
| Entities outside their expected file count | 0 in 19 entities | 100.0% | 10 | 50% of entities |
| **Prose** | | **93.5%** | **20** | |
| Conditions with calls tangled inside calls | 278 in 13291 branches | 91.6% | 8 | 25% of branches |
| Conditions compared to a raw literal | 236 in 13291 branches | 92.9% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 9630 functions | 96.5% | 6 | 10% of functions |
| **Widget adoption** | | **47.8%** | **8** | |
| Markup written by hand where a widget should be | 706 in 2703 widget slots | 47.8% | 8 | 50% of widget slots |
| **Input validation** | | **99.2%** | **8** | |
| Doors that write without checking their input against the columns | 3 in 716 write doors | 99.2% | 8 | 50% of write doors |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,470 | 2,465 | 123,947 |
| frontend | 1,487 | 1,376 | 130,842 |
| contracts | 775 | 775 | 28,344 |
| database | 316 | 0 | 0 |
| connector | 256 | 256 | 27,110 |
| docs | 232 | 0 | 0 |
| tests | 195 | 0 | 0 |
| tooling | 192 | 0 | 0 |
| infrastructure | 38 | 0 | 0 |
| worker | 14 | 14 | 824 |
| other | 1 | 0 | 0 |
| **whole repository** | **5,976** | **4,886** | **311,067** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 793, totalFiles: 4886, totalLines: 311067, worstFileLines: 551, worstFileTimesOverLimit: 4.51 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 9630, elseBlocks: 1181, ifBlocks: 13291, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 58, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34, carriedFromBaseline: True |
| naming | bannedAbbreviationHits: 800, unprefixedBooleans: 2054 |
| comments | explanatoryCommentLines: 16311, filesWithComments: 2305, taskMarkers: 52 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 65, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 4117, deeplyIndentedLines: 3064, overlongLines: 1984, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 278, literalComparisonLines: 236, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 9630 |
| designPatterns | roleFamilies: 196, predictedFiles: 2468, predictedFilesMissing: 187, entities: 19, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 120, components: 150, orphanComponents: 6, averagePageLines: 173 |
| siteDefinition | routes: 133, views: 716, siteComponents: 685, catalogue: 34, widgetUsages: 1997, handRolledElements: 706, widgetSlots: 2703, viewsWithHandRolled: 323, designSheets: 0, designsLastChecked: never, brandCheckedAt: never |
| inputValidation | schemaTables: 217, limitedColumns: 1304, writeDoors: 716, unvalidatedDoors: 0, looserLimits: 3 |
| fileAreas | totalFiles: 5976, api: 2470, frontend: 1487, contracts: 775, database: 316, connector: 256, docs: 232, tests: 195, tooling: 192, infrastructure: 38, worker: 14, other: 1 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 75.3% | 78.5% | — |
| fileLength.filesOverLimit | 794 | 793 | better |
| fileLength.worstFileLines | 557 | 551 | better |
| functionShape.functionsOverLimit | 836 | 836 | held |
| functionShape.elseBlocks | 1186 | 1181 | better |
| duplication.duplicatedPercentage | 2.34 | 2.34 | held |
| comments.explanatoryCommentLines | 16311 | 16311 | held |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 4117 | 4117 | held |
| prose.deeplyIndentedLines | 3125 | 3064 | better |
| functionNames.overlongFunctionNames | 58 | 58 | held |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 278 | held |
| conditions.literalComparisonLines | 246 | 236 | better |
| designPatterns.predictedFilesMissing | 187 | 187 | held |
| siteDefinition.handRolledElements | None | 706 | — |
| inputValidation.unvalidatedDoors | None | 0 | — |
| inputValidation.looserLimits | None | 3 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 551 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| jpms/Services/Navigation/SidebarFolders.cs | 465 |
| jpms/Services/HttpLabourStore.cs | 452 |
| jpms/Components/ProjectDetailsEditor.razor | 441 |
| jpms/Pages/XeroAllocation.razor | 433 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Pages/ProjectValuation.razor | 423 |
| jpms/Pages/SubcontractorDetail.razor | 422 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Pages/ProjectVariations.razor | 412 |
| jpms/Pages/DocumentControl.Filing.cs | 409 |
| api/Features/Ai/Tools/AiValuationInvoiceTools.cs | 405 |
| jpms/Pages/TriageQueue.razor | 402 |
| jpms/Features/Triage/AttachmentPicker.razor | 396 |
| jpms/Components/ValuationReportTable.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Pages/ProjectLabour.razor | 386 |
| jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor | 385 |

Full detail, including every offender list, is in `audit.json`.
