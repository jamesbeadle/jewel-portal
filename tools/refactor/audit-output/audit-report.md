# Refactor audit

Generated 2026-09-30 22:27 UTC.

## Headline

**Code quality score 78.4%.** **788 of 4,822 source files are over the 100-line limit (16.3%)**; the worst file is 551 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **70.0%** | **60** | |
| Files over the line limit | 788 in 4822 files | 67.3% | 10 | 50% of files |
| Worst file, in limits over | 4.51 | 49.9% | 5 | 9 |
| Functions over the line limit | 836 in 9482 functions | 64.7% | 8 | 25% of functions |
| Else blocks | 1183 in 13174 branches | 82.0% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16307 in 308 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 135 in 308 thousand lines | 97.8% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 9632 components and functions | 97.2% | 4 | 10% of components and functions |
| Long member chain lines | 4114 in 308 thousand lines | 55.5% | 4 | 30 per thousand lines |
| Deeply indented lines | 3064 in 308 thousand lines | 66.8% | 4 | 30 per thousand lines |
| Overlong function names | 58 in 9482 functions | 93.9% | 4 | 10% of functions |
| **Design pattern file count** | | **92.5%** | **20** | |
| Files the patterns predict but are missing | 186 in 2467 predicted files | 84.9% | 10 | 50% of predicted files |
| Entities outside their expected file count | 0 in 19 entities | 100.0% | 10 | 50% of entities |
| **Prose** | | **93.4%** | **20** | |
| Conditions with calls tangled inside calls | 276 in 13174 branches | 91.6% | 8 | 25% of branches |
| Conditions compared to a raw literal | 234 in 13174 branches | 92.9% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 9482 functions | 96.4% | 6 | 10% of functions |
| **Widget adoption** | | **47.8%** | **8** | |
| Markup written by hand where a widget should be | 699 in 2678 widget slots | 47.8% | 8 | 50% of widget slots |
| **Input validation** | | **99.2%** | **8** | |
| Doors that write without checking their input against the columns | 3 in 713 write doors | 99.2% | 8 | 50% of write doors |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,447 | 2,444 | 122,787 |
| frontend | 1,452 | 1,344 | 129,420 |
| contracts | 765 | 766 | 28,066 |
| database | 314 | 0 | 0 |
| connector | 254 | 254 | 26,901 |
| docs | 232 | 0 | 0 |
| tooling | 192 | 0 | 0 |
| tests | 188 | 0 | 0 |
| infrastructure | 38 | 0 | 0 |
| worker | 14 | 14 | 824 |
| other | 1 | 0 | 0 |
| **whole repository** | **5,897** | **4,822** | **307,998** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 788, totalFiles: 4822, totalLines: 307998, worstFileLines: 551, worstFileTimesOverLimit: 4.51 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 9482, elseBlocks: 1183, ifBlocks: 13174, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 58, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34, carriedFromBaseline: True |
| naming | bannedAbbreviationHits: 783, unprefixedBooleans: 2049 |
| comments | explanatoryCommentLines: 16307, filesWithComments: 2303, taskMarkers: 52 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 65, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 4114, deeplyIndentedLines: 3064, overlongLines: 1965, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 276, literalComparisonLines: 234, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 9482 |
| designPatterns | roleFamilies: 187, predictedFiles: 2467, predictedFilesMissing: 186, entities: 19, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 119, components: 150, orphanComponents: 6, averagePageLines: 173 |
| siteDefinition | routes: 132, views: 696, siteComponents: 665, catalogue: 34, widgetUsages: 1979, handRolledElements: 699, widgetSlots: 2678, viewsWithHandRolled: 316, designSheets: 0, designsLastChecked: never, brandCheckedAt: never |
| inputValidation | schemaTables: 216, limitedColumns: 1301, writeDoors: 713, unvalidatedDoors: 0, looserLimits: 3 |
| fileAreas | totalFiles: 5897, api: 2447, frontend: 1452, contracts: 765, database: 314, connector: 254, docs: 232, tooling: 192, tests: 188, infrastructure: 38, worker: 14, other: 1 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 75.3% | 78.4% | — |
| fileLength.filesOverLimit | 794 | 788 | better |
| fileLength.worstFileLines | 557 | 551 | better |
| functionShape.functionsOverLimit | 836 | 836 | held |
| functionShape.elseBlocks | 1186 | 1183 | better |
| duplication.duplicatedPercentage | 2.34 | 2.34 | held |
| comments.explanatoryCommentLines | 16311 | 16307 | better |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 4117 | 4114 | better |
| prose.deeplyIndentedLines | 3125 | 3064 | better |
| functionNames.overlongFunctionNames | 58 | 58 | held |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 276 | better |
| conditions.literalComparisonLines | 246 | 234 | better |
| designPatterns.predictedFilesMissing | 187 | 186 | better |
| siteDefinition.handRolledElements | None | 699 | — |
| inputValidation.unvalidatedDoors | None | 0 | — |
| inputValidation.looserLimits | None | 3 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 551 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| jpms/Services/Navigation/SidebarFolders.cs | 463 |
| jpms/Services/HttpLabourStore.cs | 449 |
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
