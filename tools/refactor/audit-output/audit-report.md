# Refactor audit

Generated 2026-09-01 07:11 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 471, totalFiles: 2992, worstFileLines: 4659 |
| functionShape | limit: 30, functionsOverLimit: 673, elseBlocks: 1234, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 744, duplicatedLines: 8934, totalLines: 219486, duplicatedPercentage: 4.07 |
| naming | bannedAbbreviationHits: 472, unprefixedBooleans: 1343 |
| comments | explanatoryCommentLines: 13814, filesWithComments: 1506, taskMarkers: 48 |
| magicValues | inlineHexColours: 50, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2415, deeplyIndentedLines: 2688, overlongLines: 1783, measurementIsHeuristic: True |
| inventory | pages: 92, components: 127, orphanComponents: 6, averagePageLines: 522 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 4659 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 2892 |
| jpms/Pages/XeroAllocation.razor | 2759 |
| api/Features/Xero/XeroClient.cs | 2163 |
| jpms/Pages/ProjectRequestDetail.razor | 2158 |
| jpms/Pages/ProfitSummary.razor | 1845 |
| jpms/Pages/ProjectVariationDetail.razor | 1583 |
| jpms/Pages/LabourOverview.razor | 1501 |
| api/Features/MailboxIntake/Graph/MailboxGraphClient.cs | 1471 |
| jpms/Pages/CashForecast.razor | 1436 |
| jpms/Pages/ProjectProgramme.razor | 1428 |
| jpms/Pages/WeeklyCashflow.razor | 1425 |
| jpms/Pages/ProjectWorkOrders.razor | 1406 |
| api/Features/Ai/Tools/AiToolCatalogue.cs | 1199 |
| jpms/Components/ValuationReportTable.razor | 1170 |
| jpms/Pages/ProjectValuation.razor | 1144 |
| jpms/Pages/ProjectLabour.razor | 1015 |
| jpms/Components/FinancialsTable.razor | 984 |
| jpms/Pages/Subcontractors.razor | 984 |
| contracts/Ai/ModalCatalog.cs | 937 |

Full detail, including every offender list, is in `audit.json`.
