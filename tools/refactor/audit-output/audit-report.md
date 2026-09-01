# Refactor audit

Generated 2026-09-01 14:07 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 622, totalFiles: 3233, worstFileLines: 1029 |
| functionShape | limit: 30, functionsOverLimit: 699, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 581, duplicatedLines: 7493, totalLines: 220821, duplicatedPercentage: 3.39 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1331 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1723, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2819, overlongLines: 1782, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 279 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/XeroAllocation.razor | 1029 |
| jpms/Pages/TriageQueue.razor | 1016 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 562 |
| jpms/Pages/ProjectWorkOrders.razor | 552 |
| jpms/Pages/TriageQueue.Outbox.cs | 524 |

Full detail, including every offender list, is in `audit.json`.
