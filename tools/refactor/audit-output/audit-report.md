# Refactor audit

Generated 2026-09-17 11:52 UTC.

## Headline

**Code quality score 75.3%.** **794 of 4,102 source files are over the 100-line limit (19.4%)**; the worst file is 557 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **68.0%** | **60** | |
| Files over the line limit | 794 in 4102 files | 61.3% | 10 | 50% of files |
| Worst file, in limits over | 4.57 | 49.2% | 5 | 9 |
| Functions over the line limit | 836 in 8138 functions | 58.9% | 8 | 25% of functions |
| Else blocks | 1186 in 12046 branches | 80.3% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16311 in 275.33 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 134 in 275.33 thousand lines | 97.6% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 8282 components and functions | 96.7% | 4 | 10% of components and functions |
| Long member chain lines | 3165 in 275.33 thousand lines | 61.7% | 4 | 30 per thousand lines |
| Deeply indented lines | 3125 in 275.33 thousand lines | 62.2% | 4 | 30 per thousand lines |
| Overlong function names | 58 in 8138 functions | 92.9% | 4 | 10% of functions |
| **Design pattern file count** | | **84.0%** | **10** | |
| Files the patterns predict but are missing | 187 in 2338 predicted files | 84.0% | 10 | 50% of predicted files |
| Entities outside their expected file count | not measured | not measured | — | 50% of entities |
| **Prose** | | **92.6%** | **20** | |
| Conditions with calls tangled inside calls | 278 in 12046 branches | 90.8% | 8 | 25% of branches |
| Conditions compared to a raw literal | 246 in 12046 branches | 91.8% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 8138 functions | 95.8% | 6 | 10% of functions |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,144 | 2,140 | 109,502 |
| frontend | 1,217 | 1,107 | 117,814 |
| contracts | 629 | 629 | 22,279 |
| database | 269 | 0 | 0 |
| connector | 213 | 213 | 24,137 |
| docs | 187 | 0 | 0 |
| tooling | 136 | 0 | 0 |
| tests | 114 | 0 | 0 |
| infrastructure | 40 | 0 | 0 |
| worker | 13 | 13 | 1,603 |
| **whole repository** | **4,962** | **4,102** | **275,335** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 794, totalFiles: 4102, totalLines: 275335, worstFileLines: 557, worstFileTimesOverLimit: 4.57 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 8138, elseBlocks: 1186, ifBlocks: 12046, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 58, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34 |
| naming | bannedAbbreviationHits: 701, unprefixedBooleans: 1913 |
| comments | explanatoryCommentLines: 16311, filesWithComments: 2258, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 3165, deeplyIndentedLines: 3125, overlongLines: 1840, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 278, literalComparisonLines: 246, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 8138 |
| designPatterns | roleFamilies: 154, predictedFiles: 2338, predictedFilesMissing: 187, entities: 0, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 104, components: 144, orphanComponents: 6, averagePageLines: 193 |
| fileAreas | totalFiles: 4962, api: 2144, frontend: 1217, contracts: 629, database: 269, connector: 213, docs: 187, tooling: 136, tests: 114, infrastructure: 40, worker: 13 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | 74.9% | 75.3% | — |
| fileLength.filesOverLimit | 799 | 794 | better |
| fileLength.worstFileLines | 599 | 557 | better |
| functionShape.functionsOverLimit | 836 | 836 | held |
| functionShape.elseBlocks | 1187 | 1186 | better |
| duplication.duplicatedPercentage | 2.35 | 2.34 | better |
| comments.explanatoryCommentLines | 16313 | 16311 | better |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | 21 | 21 | held |
| prose.longMemberChainLines | 3171 | 3165 | better |
| prose.deeplyIndentedLines | 3125 | 3125 | held |
| functionNames.overlongFunctionNames | 58 | 58 | held |
| accessorNames.gluedAccessorNames | 34 | 34 | held |
| conditions.tangledConditionLines | 278 | 278 | held |
| conditions.literalComparisonLines | 246 | 246 | held |
| designPatterns.predictedFilesMissing | 187 | 187 | held |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Data/JpmsContext.Model.cs | 557 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Services/Navigation/SidebarFolders.cs | 454 |
| jpms/Components/ProjectDetailsEditor.razor | 440 |
| jpms/Services/HttpLabourStore.cs | 436 |
| jpms/Pages/XeroAllocation.razor | 435 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Pages/SubcontractorDetail.razor | 419 |
| jpms/Pages/ProjectVariations.razor | 417 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Pages/DocumentControl.Filing.cs | 409 |
| jpms/Pages/ProjectValuation.razor | 405 |
| jpms/Pages/TriageQueue.razor | 403 |
| jpms/Features/Triage/AttachmentPicker.razor | 396 |
| jpms/Components/ValuationReportTable.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor | 385 |
| jpms/Components/DrawingExtractionPanel.razor | 383 |

Full detail, including every offender list, is in `audit.json`.
