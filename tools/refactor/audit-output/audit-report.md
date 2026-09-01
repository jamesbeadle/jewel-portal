# Refactor audit

Generated 2026-09-01 15:45 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 618, totalFiles: 3262, worstFileLines: 954 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 468, duplicatedLines: 6040, totalLines: 214587, duplicatedPercentage: 2.81 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1348 |
| comments | explanatoryCommentLines: 13728, filesWithComments: 1744, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2803, overlongLines: 1775, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 272 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 954 |
| jpms/Pages/XeroAllocation.razor | 929 |
| jpms/Pages/ProjectVariationDetail.razor | 837 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 830 |
| jpms/Pages/ProjectRequestDetail.razor | 802 |
| jpms/Pages/LabourOverview.razor | 785 |
| jpms/Pages/ProjectProgramme.razor | 750 |
| jpms/Pages/TriageQueue.Compose.cs | 742 |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| api/Data/JpmsContext.cs | 592 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.cs | 577 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| api/Features/Requests/Documents/RequestDocumentRenderer.cs | 523 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 517 |

Full detail, including every offender list, is in `audit.json`.
