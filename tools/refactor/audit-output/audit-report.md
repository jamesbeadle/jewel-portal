# Refactor audit

Generated 2026-09-01 12:55 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 618, totalFiles: 3219, worstFileLines: 1152 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 584, duplicatedLines: 7540, totalLines: 220495, duplicatedPercentage: 3.42 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1325 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1709, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2821, overlongLines: 1791, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 284 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1101 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 737 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 686 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 562 |
| jpms/Pages/ProjectWorkOrders.razor | 552 |

Full detail, including every offender list, is in `audit.json`.
