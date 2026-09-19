# Refactor audit

Generated 2026-09-19 09:24 UTC.

## Headline

**Code quality score 74.0%.** **781 of 4,140 source files are over the 100-line limit (18.9%)**; the worst file is 553 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **70.4%** | **60** | |
| Files over the line limit | 781 in 4140 files | 62.3% | 10 | 50% of files |
| Worst file, in limits over | 4.53 | 49.7% | 5 | 9 |
| Functions over the line limit | 827 in 8190 functions | 59.6% | 8 | 25% of functions |
| Else blocks | 1177 in 12002 branches | 80.4% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16116 in 276.13 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 134 in 276.13 thousand lines | 97.6% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 8336 components and functions | 96.8% | 4 | 10% of components and functions |
| Long member chain lines | 754 in 276.13 thousand lines | 90.9% | 4 | 30 per thousand lines |
| Deeply indented lines | 3078 in 276.13 thousand lines | 62.8% | 4 | 30 per thousand lines |
| Overlong function names | 53 in 8190 functions | 93.5% | 4 | 10% of functions |
| **Design pattern file count** | | **84.0%** | **10** | |
| Files the patterns predict but are missing | 188 in 2350 predicted files | 84.0% | 10 | 50% of predicted files |
| Entities outside their expected file count | not measured | not measured | — | 50% of entities |
| **Prose** | | **92.6%** | **20** | |
| Conditions with calls tangled inside calls | 278 in 12002 branches | 90.7% | 8 | 25% of branches |
| Conditions compared to a raw literal | 246 in 12002 branches | 91.8% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 8190 functions | 95.8% | 6 | 10% of functions |
| **Widget adoption** | | **42.8%** | **8** | |
| Markup written by hand where a widget should be | 669 in 2338 widget slots | 42.8% | 8 | 50% of widget slots |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,178 | 2,174 | 110,665 |
| frontend | 1,224 | 1,114 | 118,010 |
| contracts | 629 | 629 | 22,490 |
| database | 273 | 0 | 0 |
| connector | 215 | 215 | 24,361 |
| docs | 188 | 0 | 0 |
| tooling | 156 | 0 | 0 |
| tests | 128 | 0 | 0 |
| infrastructure | 40 | 0 | 0 |
| worker | 8 | 8 | 606 |
| **whole repository** | **5,039** | **4,140** | **276,132** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 781, totalFiles: 4140, totalLines: 276132, worstFileLines: 553, worstFileTimesOverLimit: 4.53 |
| functionShape | limit: 30, functionsOverLimit: 827, totalFunctions: 8190, elseBlocks: 1177, ifBlocks: 12002, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 53, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34, carriedFromBaseline: True |
| naming | bannedAbbreviationHits: 717, unprefixedBooleans: 1960 |
| comments | explanatoryCommentLines: 16116, filesWithComments: 2263, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 754, deeplyIndentedLines: 3078, overlongLines: 1830, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 278, literalComparisonLines: 246, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 8190 |
| designPatterns | roleFamilies: 160, predictedFiles: 2350, predictedFilesMissing: 188, entities: 0, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 104, components: 146, orphanComponents: 6, averagePageLines: 191 |
| siteDefinition | routes: 116, views: 555, siteComponents: 525, catalogue: 34, widgetUsages: 1669, handRolledElements: 669, widgetSlots: 2338, viewsWithHandRolled: 293, designSheets: 0, designsLastChecked: never, brandCheckedAt: never |
| fileAreas | totalFiles: 5039, api: 2178, frontend: 1224, contracts: 629, database: 273, connector: 215, docs: 188, tooling: 156, tests: 128, infrastructure: 40, worker: 8 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 75.3% | 74.0% | — |
| fileLength.filesOverLimit | 794 | 781 | better |
| fileLength.worstFileLines | 557 | 553 | better |
| functionShape.functionsOverLimit | 836 | 827 | better |
| functionShape.elseBlocks | 1186 | 1177 | better |
| duplication.duplicatedPercentage | 2.34 | 2.34 | held |
| comments.explanatoryCommentLines | 16311 | 16116 | better |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 3165 | 754 | better |
| prose.deeplyIndentedLines | 3125 | 3078 | better |
| functionNames.overlongFunctionNames | 58 | 53 | better |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 278 | held |
| conditions.literalComparisonLines | 246 | 246 | held |
| designPatterns.predictedFilesMissing | 187 | 188 | worse |
| siteDefinition.handRolledElements | None | 669 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 553 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| jpms/Services/Navigation/SidebarFolders.cs | 452 |
| jpms/Components/ProjectDetailsEditor.razor | 440 |
| jpms/Services/HttpLabourStore.cs | 436 |
| jpms/Pages/XeroAllocation.razor | 433 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Pages/ProjectValuation.razor | 420 |
| jpms/Pages/SubcontractorDetail.razor | 419 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Pages/DocumentControl.Filing.cs | 409 |
| api/Features/Ai/Tools/AiValuationInvoiceTools.cs | 405 |
| jpms/Pages/TriageQueue.razor | 401 |
| jpms/Pages/ProjectVariations.razor | 400 |
| jpms/Features/Triage/AttachmentPicker.razor | 396 |
| jpms/Components/ValuationReportTable.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor | 385 |
| jpms/Components/DrawingExtractionPanel.razor | 383 |

Full detail, including every offender list, is in `audit.json`.
