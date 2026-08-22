# Refactor audit

Generated 2026-08-22 02:58 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 385, totalFiles: 2573, worstFileLines: 4961 |
| functionShape | limit: 30, functionsOverLimit: 602, elseBlocks: 1087, measurementIsHeuristic: True |
| duplication | clones: 637, duplicatedLines: 7601, totalLines: 182833, duplicatedPercentage: 4.16 |
| naming | bannedAbbreviationHits: 413, unprefixedBooleans: 1165 |
| comments | explanatoryCommentLines: 12004, filesWithComments: 1259, taskMarkers: 46 |
| magicValues | inlineHexColours: 50, inlineStyleAttributes: 42, repeatedStringLiterals: 30 |
| inventory | pages: 79, components: 117, orphanComponents: 5, averagePageLines: 544 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 4961 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 3219 |
| jpms/Pages/ProjectRequestDetail.razor | 2618 |
| jpms/Pages/XeroAllocation.razor | 2175 |
| api/Features/Xero/XeroClient.cs | 2157 |
| jpms/Pages/ProfitSummary.razor | 1837 |
| jpms/Pages/LabourOverview.razor | 1719 |
| jpms/Components/Chat/ChatPanel.razor | 1548 |
| jpms/Pages/ProjectWorkOrders.razor | 1505 |
| jpms/Pages/CashForecast.razor | 1436 |
| jpms/Pages/ProjectProgramme.razor | 1428 |
| api/Features/MailboxIntake/Graph/MailboxGraphClient.cs | 1406 |
| jpms/Pages/ProjectVariationDetail.razor | 1351 |
| api/Features/Ai/Tools/AiToolCatalogue.cs | 1137 |
| jpms/Components/ValuationReportTable.razor | 1124 |
| api/Features/Ai/AiTurnRunner.cs | 1033 |
| jpms/Pages/ProjectValuation.razor | 1022 |
| jpms/Components/FinancialsTable.razor | 984 |
| jpms/Pages/ProjectLabour.razor | 970 |
| jpms/Pages/Subcontractors.razor | 960 |

Full detail, including every offender list, is in `audit.json`.
