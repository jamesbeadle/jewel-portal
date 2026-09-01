# Refactor audit

Generated 2026-09-01 20:45 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 638, totalFiles: 3316, worstFileLines: 785 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 486, duplicatedLines: 6166, totalLines: 215987, duplicatedPercentage: 2.85 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1461 |
| comments | explanatoryCommentLines: 13727, filesWithComments: 1795, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2398, deeplyIndentedLines: 2801, overlongLines: 1726, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 246 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/LabourOverview.razor | 785 |
| jpms/Pages/ProjectProgramme.razor | 750 |
| jpms/Pages/TriageQueue.Compose.cs | 742 |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 517 |
| jpms/Pages/Subcontractors.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |
| api/Features/Ai/Tools/AiSourceTools.cs | 502 |
| api/Features/Ai/Tools/Actions/RequestsActions.cs | 492 |
| jpms/Pages/XeroAllocation.razor | 488 |
| jpms/Components/ValuationInvoicesSection.razor.cs | 476 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |

Full detail, including every offender list, is in `audit.json`.
