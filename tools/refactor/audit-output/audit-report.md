# Refactor audit

Generated 2026-09-01 07:36 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 481, totalFiles: 3007, worstFileLines: 2892 |
| functionShape | limit: 30, functionsOverLimit: 673, elseBlocks: 1230, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 749, duplicatedLines: 9135, totalLines: 219836, duplicatedPercentage: 4.16 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1353 |
| comments | explanatoryCommentLines: 13790, filesWithComments: 1519, taskMarkers: 48 |
| magicValues | inlineHexColours: 50, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2420, deeplyIndentedLines: 2711, overlongLines: 1782, measurementIsHeuristic: True |
| inventory | pages: 92, components: 128, orphanComponents: 6, averagePageLines: 484 |

## Worst files by length

| File | Lines |
| --- | --- |
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
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectValuation.razor | 1144 |
| jpms/Pages/ProjectLabour.razor | 1015 |
| jpms/Components/FinancialsTable.razor | 984 |
| jpms/Pages/Subcontractors.razor | 984 |
| contracts/Ai/ModalCatalog.cs | 937 |

Full detail, including every offender list, is in `audit.json`.
