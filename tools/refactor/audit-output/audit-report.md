# Refactor audit

Generated 2026-09-01 15:23 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 621, totalFiles: 3257, worstFileLines: 958 |
| functionShape | limit: 30, functionsOverLimit: 703, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 523, duplicatedLines: 6909, totalLines: 215666, duplicatedPercentage: 3.2 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1343 |
| comments | explanatoryCommentLines: 13729, filesWithComments: 1740, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2403, deeplyIndentedLines: 2803, overlongLines: 1776, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 274 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 958 |
| jpms/Pages/XeroAllocation.razor | 930 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectRequestDetail.razor | 805 |
| jpms/Pages/LabourOverview.razor | 786 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 746 |
| jpms/Pages/ProfitSummary.razor | 738 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 552 |
| jpms/Pages/TriageQueue.Outbox.cs | 524 |
| api/Features/Requests/Documents/RequestDocumentRenderer.cs | 523 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 518 |

Full detail, including every offender list, is in `audit.json`.
