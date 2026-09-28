# Refactor audit

Generated 2026-09-28 18:19 UTC.

## Headline

**Code quality score 71.2%.** **781 of 4,672 source files are over the 100-line limit (16.7%)**; the worst file is 557 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **69.6%** | **60** | |
| Files over the line limit | 781 in 4672 files | 66.6% | 10 | 50% of files |
| Worst file, in limits over | 4.57 | 49.2% | 5 | 9 |
| Functions over the line limit | 836 in 9200 functions | 63.7% | 8 | 25% of functions |
| Else blocks | 1167 in 12888 branches | 81.9% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16284 in 301.18 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 135 in 301.18 thousand lines | 97.8% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 9347 components and functions | 97.1% | 4 | 10% of components and functions |
| Long member chain lines | 4105 in 301.18 thousand lines | 54.6% | 4 | 30 per thousand lines |
| Deeply indented lines | 3064 in 301.18 thousand lines | 66.1% | 4 | 30 per thousand lines |
| Overlong function names | 56 in 9200 functions | 93.9% | 4 | 10% of functions |
| **Design pattern file count** | | **92.3%** | **20** | |
| Files the patterns predict but are missing | 187 in 2426 predicted files | 84.6% | 10 | 50% of predicted files |
| Entities outside their expected file count | 0 in 19 entities | 100.0% | 10 | 50% of entities |
| **Prose** | | **93.3%** | **20** | |
| Conditions with calls tangled inside calls | 272 in 12888 branches | 91.6% | 8 | 25% of branches |
| Conditions compared to a raw literal | 235 in 12888 branches | 92.7% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 9200 functions | 96.3% | 6 | 10% of functions |
| **Widget adoption** | | **46.5%** | **8** | |
| Markup written by hand where a widget should be | 688 in 2571 widget slots | 46.5% | 8 | 50% of widget slots |
| **Input validation** | | **0.0%** | **8** | |
| Doors that write without checking their input against the columns | 12617 in 685 write doors | 0.0% | 8 | 50% of write doors |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,377 | 2,373 | 120,016 |
| frontend | 1,400 | 1,290 | 126,478 |
| contracts | 745 | 745 | 27,281 |
| database | 306 | 0 | 0 |
| connector | 250 | 250 | 26,585 |
| docs | 211 | 0 | 0 |
| tooling | 191 | 0 | 0 |
| tests | 176 | 0 | 0 |
| infrastructure | 38 | 0 | 0 |
| worker | 14 | 14 | 824 |
| other | 1 | 0 | 0 |
| **whole repository** | **5,709** | **4,672** | **301,184** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 781, totalFiles: 4672, totalLines: 301184, worstFileLines: 557, worstFileTimesOverLimit: 4.57 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 9200, elseBlocks: 1167, ifBlocks: 12888, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 56, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34, carriedFromBaseline: True |
| naming | bannedAbbreviationHits: 744, unprefixedBooleans: 2023 |
| comments | explanatoryCommentLines: 16284, filesWithComments: 2295, taskMarkers: 52 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 65, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 4105, deeplyIndentedLines: 3064, overlongLines: 1903, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 272, literalComparisonLines: 235, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 9200 |
| designPatterns | roleFamilies: 181, predictedFiles: 2426, predictedFilesMissing: 187, entities: 19, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 116, components: 147, orphanComponents: 6, averagePageLines: 176 |
| siteDefinition | routes: 129, views: 664, siteComponents: 633, catalogue: 34, widgetUsages: 1883, handRolledElements: 688, widgetSlots: 2571, viewsWithHandRolled: 309, designSheets: 0, designsLastChecked: never, brandCheckedAt: never |
| inputValidation | schemaTables: 213, limitedColumns: 1274, writeDoors: 685, unvalidatedDoors: 0, looserLimits: 12617 |
| fileAreas | totalFiles: 5709, api: 2377, frontend: 1400, contracts: 745, database: 306, connector: 250, docs: 211, tooling: 191, tests: 176, infrastructure: 38, worker: 14, other: 1 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 75.3% | 71.2% | — |
| fileLength.filesOverLimit | 794 | 781 | better |
| fileLength.worstFileLines | 557 | 557 | held |
| functionShape.functionsOverLimit | 836 | 836 | held |
| functionShape.elseBlocks | 1186 | 1167 | better |
| duplication.duplicatedPercentage | 2.34 | 2.34 | held |
| comments.explanatoryCommentLines | 16311 | 16284 | better |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 4117 | 4105 | better |
| prose.deeplyIndentedLines | 3125 | 3064 | better |
| functionNames.overlongFunctionNames | 58 | 56 | better |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 272 | better |
| conditions.literalComparisonLines | 246 | 235 | better |
| designPatterns.predictedFilesMissing | 187 | 187 | held |
| siteDefinition.handRolledElements | None | 688 | — |
| inputValidation.unvalidatedDoors | None | 0 | — |
| inputValidation.looserLimits | None | 12617 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 557 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| jpms/Services/Navigation/SidebarFolders.cs | 461 |
| jpms/Services/HttpLabourStore.cs | 443 |
| jpms/Components/ProjectDetailsEditor.razor | 441 |
| jpms/Pages/XeroAllocation.razor | 433 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Pages/ProjectValuation.razor | 420 |
| jpms/Pages/SubcontractorDetail.razor | 419 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Pages/ProjectVariations.razor | 412 |
| jpms/Pages/DocumentControl.Filing.cs | 409 |
| api/Features/Ai/Tools/AiValuationInvoiceTools.cs | 405 |
| jpms/Pages/TriageQueue.razor | 402 |
| jpms/Features/Triage/AttachmentPicker.razor | 396 |
| jpms/Components/ValuationReportTable.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor | 385 |
| jpms/Components/DrawingExtractionPanel.razor | 383 |

Full detail, including every offender list, is in `audit.json`.
