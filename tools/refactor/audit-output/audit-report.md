# Refactor audit

Generated 2026-09-02 07:21 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 652, totalFiles: 3394, worstFileLines: 520 |
| functionShape | limit: 30, functionsOverLimit: 698, elseBlocks: 1170, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 490, duplicatedLines: 6220, totalLines: 216620, duplicatedPercentage: 2.87 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1489 |
| comments | explanatoryCommentLines: 13682, filesWithComments: 1855, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2396, deeplyIndentedLines: 2800, overlongLines: 1677, measurementIsHeuristic: True |
| inventory | pages: 92, components: 132, orphanComponents: 6, averagePageLines: 214 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| jpms/Components/ValuationReportTable.razor | 517 |
| jpms/Pages/Subcontractors.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |
| api/Features/Ai/Tools/Actions/RequestsActions.cs | 492 |
| jpms/Pages/XeroAllocation.razor | 488 |
| jpms/Components/ValuationInvoicesSection.razor.cs | 476 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/ProjectLabour.razor | 472 |
| jpms/Pages/TriageQueue.razor | 470 |
| jpms/Pages/DocumentControl.razor | 455 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 453 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.Sections.cs | 448 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Labour.cs | 445 |
| jpms/Pages/TriageQueue.Apply.cs | 445 |
| jpms/Pages/Todos.razor.cs | 444 |
| api/Features/Ai/Tools/AiDeliveryTools.cs | 440 |
| jpms/Pages/ProjectRequestDetail.razor | 434 |
| api/Features/Registers/RegistersSlices.cs | 433 |

Full detail, including every offender list, is in `audit.json`.
