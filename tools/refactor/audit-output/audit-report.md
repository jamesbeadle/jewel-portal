# Refactor audit

Generated 2026-09-01 08:35 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 520, totalFiles: 3070, worstFileLines: 1471 |
| functionShape | limit: 30, functionsOverLimit: 671, elseBlocks: 1184, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 756, duplicatedLines: 8864, totalLines: 219680, duplicatedPercentage: 4.03 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1324 |
| comments | explanatoryCommentLines: 13773, filesWithComments: 1577, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2766, overlongLines: 1783, measurementIsHeuristic: True |
| inventory | pages: 92, components: 129, orphanComponents: 6, averagePageLines: 390 |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Features/MailboxIntake/Graph/MailboxGraphClient.cs | 1471 |
| jpms/Pages/ProjectProgramme.razor | 1413 |
| jpms/Pages/WeeklyCashflow.razor | 1410 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1399 |
| api/Features/Ai/Tools/AiToolCatalogue.cs | 1199 |
| jpms/Components/ValuationReportTable.razor | 1169 |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectValuation.razor | 1130 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/ProjectLabour.razor | 1002 |
| jpms/Pages/Subcontractors.razor | 984 |
| jpms/Components/FinancialsTable.razor | 982 |
| jpms/Pages/LabourOverview.razor | 981 |
| contracts/Ai/ModalCatalog.cs | 937 |
| jpms/Pages/DocumentControl.razor | 898 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 859 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectVariations.razor | 813 |
| jpms/Pages/Todos.razor | 785 |

Full detail, including every offender list, is in `audit.json`.
