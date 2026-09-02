# Refactor audit

Generated 2026-09-02 06:35 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 644, totalFiles: 3347, worstFileLines: 665 |
| functionShape | limit: 30, functionsOverLimit: 698, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 487, duplicatedLines: 6195, totalLines: 216215, duplicatedPercentage: 2.87 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1478 |
| comments | explanatoryCommentLines: 13721, filesWithComments: 1823, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2397, deeplyIndentedLines: 2801, overlongLines: 1694, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 226 |

## Worst files by length

| File | Lines |
| --- | --- |
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
| jpms/Pages/ProjectLabour.razor | 472 |
| jpms/Pages/TriageQueue.razor | 470 |
| jpms/Pages/DocumentControl.razor | 455 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 453 |

Full detail, including every offender list, is in `audit.json`.
