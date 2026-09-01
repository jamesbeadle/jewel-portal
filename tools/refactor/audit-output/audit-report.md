# Refactor audit

Generated 2026-09-01 08:09 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 525, totalFiles: 3066, worstFileLines: 1471 |
| functionShape | limit: 30, functionsOverLimit: 671, elseBlocks: 1230, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 800, duplicatedLines: 10155, totalLines: 221138, duplicatedPercentage: 4.59 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1353 |
| comments | explanatoryCommentLines: 13790, filesWithComments: 1575, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2420, deeplyIndentedLines: 2766, overlongLines: 1782, measurementIsHeuristic: True |
| inventory | pages: 92, components: 128, orphanComponents: 6, averagePageLines: 394 |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Features/MailboxIntake/Graph/MailboxGraphClient.cs | 1471 |
| jpms/Pages/ProjectProgramme.razor | 1428 |
| jpms/Pages/WeeklyCashflow.razor | 1425 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1399 |
| api/Features/Ai/Tools/AiToolCatalogue.cs | 1199 |
| jpms/Components/ValuationReportTable.razor | 1170 |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectValuation.razor | 1144 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/ProjectLabour.razor | 1015 |
| jpms/Components/FinancialsTable.razor | 984 |
| jpms/Pages/Subcontractors.razor | 984 |
| jpms/Pages/LabourOverview.razor | 981 |
| contracts/Ai/ModalCatalog.cs | 937 |
| jpms/Pages/DocumentControl.razor | 917 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 870 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectVariations.razor | 815 |
| jpms/Pages/Todos.razor | 785 |

Full detail, including every offender list, is in `audit.json`.
