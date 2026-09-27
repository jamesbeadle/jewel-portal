# Refactor audit

Generated 2026-09-27 06:02 UTC.

## Headline

**Code quality score 71.0%.** **783 of 4,602 source files are over the 100-line limit (17.0%)**; the worst file is 557 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **69.3%** | **60** | |
| Files over the line limit | 783 in 4602 files | 66.0% | 10 | 50% of files |
| Worst file, in limits over | 4.57 | 49.2% | 5 | 9 |
| Functions over the line limit | 837 in 9094 functions | 63.2% | 8 | 25% of functions |
| Else blocks | 1171 in 12789 branches | 81.7% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16308 in 298.8 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 135 in 298.8 thousand lines | 97.7% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 9243 components and functions | 97.1% | 4 | 10% of components and functions |
| Long member chain lines | 4117 in 298.8 thousand lines | 54.1% | 4 | 30 per thousand lines |
| Deeply indented lines | 3066 in 298.8 thousand lines | 65.8% | 4 | 30 per thousand lines |
| Overlong function names | 56 in 9094 functions | 93.8% | 4 | 10% of functions |
| **Design pattern file count** | | **92.2%** | **20** | |
| Files the patterns predict but are missing | 187 in 2406 predicted files | 84.5% | 10 | 50% of predicted files |
| Entities outside their expected file count | 0 in 19 entities | 100.0% | 10 | 50% of entities |
| **Prose** | | **93.2%** | **20** | |
| Conditions with calls tangled inside calls | 272 in 12789 branches | 91.5% | 8 | 25% of branches |
| Conditions compared to a raw literal | 241 in 12789 branches | 92.5% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 9094 functions | 96.3% | 6 | 10% of functions |
| **Widget adoption** | | **45.5%** | **8** | |
| Markup written by hand where a widget should be | 693 in 2541 widget slots | 45.5% | 8 | 50% of widget slots |
| **Input validation** | | **0.0%** | **8** | |
| Doors that write without checking their input against the columns | 18121 in 680 write doors | 0.0% | 8 | 50% of write doors |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,335 | 2,331 | 118,640 |
| frontend | 1,376 | 1,266 | 125,630 |
| contracts | 741 | 741 | 27,135 |
| database | 304 | 0 | 0 |
| connector | 250 | 250 | 26,572 |
| docs | 205 | 0 | 0 |
| tooling | 191 | 0 | 0 |
| tests | 172 | 0 | 0 |
| infrastructure | 37 | 0 | 0 |
| worker | 14 | 14 | 824 |
| other | 1 | 0 | 0 |
| **whole repository** | **5,626** | **4,602** | **298,801** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 783, totalFiles: 4602, totalLines: 298801, worstFileLines: 557, worstFileTimesOverLimit: 4.57 |
| functionShape | limit: 30, functionsOverLimit: 837, totalFunctions: 9094, elseBlocks: 1171, ifBlocks: 12789, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 56, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34, carriedFromBaseline: True |
| naming | bannedAbbreviationHits: 734, unprefixedBooleans: 2015 |
| comments | explanatoryCommentLines: 16308, filesWithComments: 2300, taskMarkers: 52 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 65, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 4117, deeplyIndentedLines: 3066, overlongLines: 1905, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 272, literalComparisonLines: 241, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 9094 |
| designPatterns | roleFamilies: 181, predictedFiles: 2406, predictedFilesMissing: 187, entities: 19, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 116, components: 149, orphanComponents: 6, averagePageLines: 177 |
| siteDefinition | routes: 129, views: 648, siteComponents: 617, catalogue: 34, widgetUsages: 1848, handRolledElements: 693, widgetSlots: 2541, viewsWithHandRolled: 309, designSheets: 0, designsLastChecked: never, brandCheckedAt: never |
| inputValidation | schemaTables: 212, limitedColumns: 1268, writeDoors: 680, unvalidatedDoors: 0, looserLimits: 18121 |
| fileAreas | totalFiles: 5626, api: 2335, frontend: 1376, contracts: 741, database: 304, connector: 250, docs: 205, tooling: 191, tests: 172, infrastructure: 37, worker: 14, other: 1 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 75.3% | 71.0% | — |
| fileLength.filesOverLimit | 794 | 783 | better |
| fileLength.worstFileLines | 557 | 557 | held |
| functionShape.functionsOverLimit | 836 | 837 | worse |
| functionShape.elseBlocks | 1186 | 1171 | better |
| duplication.duplicatedPercentage | 2.34 | 2.34 | held |
| comments.explanatoryCommentLines | 16311 | 16308 | better |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 4117 | 4117 | held |
| prose.deeplyIndentedLines | 3125 | 3066 | better |
| functionNames.overlongFunctionNames | 58 | 56 | better |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 272 | better |
| conditions.literalComparisonLines | 246 | 241 | better |
| designPatterns.predictedFilesMissing | 187 | 187 | held |
| siteDefinition.handRolledElements | None | 693 | — |
| inputValidation.unvalidatedDoors | None | 0 | — |
| inputValidation.looserLimits | None | 18121 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 557 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| jpms/Services/Navigation/SidebarFolders.cs | 461 |
| jpms/Services/HttpLabourStore.cs | 442 |
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
