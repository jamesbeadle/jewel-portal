# Refactor audit

Generated 2026-09-01 14:56 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 628, totalFiles: 3254, worstFileLines: 958 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 586, duplicatedLines: 7597, totalLines: 221094, duplicatedPercentage: 3.44 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1334 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1738, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2819, overlongLines: 1783, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 275 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 958 |
| jpms/Pages/XeroAllocation.razor | 930 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectRequestDetail.razor | 805 |
| jpms/Pages/LabourOverview.razor | 786 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 562 |
| jpms/Pages/ProjectWorkOrders.razor | 552 |
| jpms/Pages/TriageQueue.Outbox.cs | 524 |
| api/Features/Requests/Documents/RequestDocumentRenderer.cs | 523 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 518 |

Full detail, including every offender list, is in `audit.json`.
