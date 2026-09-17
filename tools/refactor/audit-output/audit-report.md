# Refactor audit

Generated 2026-09-17 11:04 UTC.

## Headline

**Code quality score 74.9%.** **799 of 4,053 source files are over the 100-line limit (19.7%)**; the worst file is 599 lines.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **67.5%** | **60** | |
| Files over the line limit | 799 in 4053 files | 60.6% | 10 | 50% of files |
| Worst file, in limits over | 4.99 | 44.6% | 5 | 9 |
| Functions over the line limit | 836 in 8121 functions | 58.8% | 8 | 25% of functions |
| Else blocks | 1187 in 12029 branches | 80.3% | 5 | 50% of branches |
| Duplication % | 2.35 | 88.2% | 8 | 20 |
| Explanatory comment lines | 16313 in 274.81 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 134 in 274.81 thousand lines | 97.6% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 8265 components and functions | 96.7% | 4 | 10% of components and functions |
| Long member chain lines | 3171 in 274.81 thousand lines | 61.5% | 4 | 30 per thousand lines |
| Deeply indented lines | 3125 in 274.81 thousand lines | 62.1% | 4 | 30 per thousand lines |
| Overlong function names | 58 in 8121 functions | 92.9% | 4 | 10% of functions |
| **Design pattern file count** | | **84.0%** | **10** | |
| Files the patterns predict but are missing | 187 in 2338 predicted files | 84.0% | 10 | 50% of predicted files |
| Entities outside their expected file count | not measured | not measured | — | 50% of entities |
| **Prose** | | **92.6%** | **20** | |
| Conditions with calls tangled inside calls | 278 in 12029 branches | 90.8% | 8 | 25% of branches |
| Conditions compared to a raw literal | 246 in 12029 branches | 91.8% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 8121 functions | 95.8% | 6 | 10% of functions |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## The repository by area

| Area | Files | Of which audited source | Source lines |
| --- | --- | --- | --- |
| api | 2,144 | 2,140 | 109,502 |
| frontend | 1,168 | 1,058 | 117,288 |
| contracts | 629 | 629 | 22,279 |
| database | 269 | 0 | 0 |
| connector | 213 | 213 | 24,137 |
| docs | 186 | 0 | 0 |
| tooling | 136 | 0 | 0 |
| tests | 114 | 0 | 0 |
| infrastructure | 40 | 0 | 0 |
| worker | 13 | 13 | 1,603 |
| **whole repository** | **4,912** | **4,053** | **274,809** |

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 799, totalFiles: 4053, totalLines: 274809, worstFileLines: 599, worstFileTimesOverLimit: 4.99 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 8121, elseBlocks: 1187, ifBlocks: 12029, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 58, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6282, totalLines: 267881, duplicatedPercentage: 2.35 |
| naming | bannedAbbreviationHits: 708, unprefixedBooleans: 1904 |
| comments | explanatoryCommentLines: 16313, filesWithComments: 2247, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 3171, deeplyIndentedLines: 3125, overlongLines: 1875, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 278, literalComparisonLines: 246, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 8121 |
| designPatterns | roleFamilies: 154, predictedFiles: 2338, predictedFilesMissing: 187, entities: 0, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 104, components: 144, orphanComponents: 6, averagePageLines: 212 |
| fileAreas | totalFiles: 4912, api: 2144, frontend: 1168, contracts: 629, database: 269, connector: 213, docs: 186, tooling: 136, tests: 114, infrastructure: 40, worker: 13 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| code quality score | not measured | 74.9% | — |
| fileLength.filesOverLimit | 787 | 799 | worse |
| fileLength.worstFileLines | 604 | 599 | better |
| functionShape.functionsOverLimit | 816 | 836 | worse |
| functionShape.elseBlocks | 1174 | 1187 | worse |
| duplication.duplicatedPercentage | 2.37 | 2.35 | better |
| comments.explanatoryCommentLines | 16224 | 16313 | worse |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| orphans.orphanFunctions | None | 21 | — |
| prose.longMemberChainLines | 3018 | 3171 | worse |
| prose.deeplyIndentedLines | 3111 | 3125 | worse |
| functionNames.overlongFunctionNames | 57 | 58 | worse |
| accessorNames.gluedAccessorNames | None | 34 | — |
| conditions.tangledConditionLines | None | 278 | — |
| conditions.literalComparisonLines | None | 246 | — |
| designPatterns.predictedFilesMissing | None | 187 | — |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/SalesLeadDetail.razor | 599 |
| api/Data/JpmsContext.Model.cs | 557 |
| jpms/Pages/SalesStrategyDetail.razor | 526 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/Imagine.razor | 462 |
| jpms/Pages/SalesInbox.razor | 457 |
| jpms/Services/Navigation/SidebarFolders.cs | 454 |
| jpms/Pages/SalesEstimateDetail.razor | 442 |
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

Full detail, including every offender list, is in `audit.json`.
