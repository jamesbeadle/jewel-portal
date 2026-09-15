# Refactor audit

Generated 2026-09-15 16:23 UTC.

## Headline

**787 of 3,963 source files are over the 100-line limit (19.9%)**; the worst file is 604 lines.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 787, totalFiles: 3963, worstFileLines: 604 |
| functionShape | limit: 30, functionsOverLimit: 816, elseBlocks: 1174, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 57, maxWords: 5, maxLength: 40 |
| duplication | clones: 536, duplicatedLines: 6143, totalLines: 259625, duplicatedPercentage: 2.37 |
| naming | bannedAbbreviationHits: 675, unprefixedBooleans: 1886 |
| comments | explanatoryCommentLines: 16224, filesWithComments: 2280, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 3018, deeplyIndentedLines: 3111, overlongLines: 1830, measurementIsHeuristic: True |
| inventory | pages: 102, components: 144, orphanComponents: 6, averagePageLines: 213 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| fileLength.filesOverLimit | 787 | 787 | held |
| fileLength.worstFileLines | 604 | 604 | held |
| functionShape.functionsOverLimit | 816 | 816 | held |
| functionShape.elseBlocks | 1174 | 1174 | held |
| duplication.duplicatedPercentage | 2.37 | 2.37 | held |
| comments.explanatoryCommentLines | 16224 | 16224 | held |
| magicValues.inlineHexColours | 40 | 40 | held |
| inventory.orphanComponents | 6 | 6 | held |
| prose.longMemberChainLines | 3018 | 3018 | held |
| prose.deeplyIndentedLines | 3111 | 3111 | held |
| functionNames.overlongFunctionNames | 57 | 57 | held |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/SalesLeadDetail.razor | 604 |
| jpms/Pages/SalesStrategyDetail.razor | 526 |
| api/Data/JpmsContext.Model.cs | 517 |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/Imagine.razor | 462 |
| jpms/Pages/SalesInbox.razor | 457 |
| jpms/Services/Navigation/SidebarFolders.cs | 449 |
| jpms/Pages/SalesEstimateDetail.razor | 442 |
| jpms/Services/HttpLabourStore.cs | 436 |
| jpms/Pages/XeroAllocation.razor | 435 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Components/ProjectDetailsEditor.razor | 428 |
| jpms/Pages/SubcontractorDetail.razor | 419 |
| jpms/Pages/ProjectVariations.razor | 417 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Pages/DocumentControl.Filing.cs | 409 |
| jpms/Pages/ProjectValuation.razor | 405 |
| jpms/Pages/TriageQueue.razor | 403 |

Full detail, including every offender list, is in `audit.json`.
