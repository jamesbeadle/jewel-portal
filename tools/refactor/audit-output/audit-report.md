# Refactor audit

Generated 2026-09-02 10:09 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 658, totalFiles: 3453, worstFileLines: 475 |
| functionShape | limit: 30, functionsOverLimit: 695, elseBlocks: 1156, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 485, duplicatedLines: 6042, totalLines: 217149, duplicatedPercentage: 2.78 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1524 |
| comments | explanatoryCommentLines: 13678, filesWithComments: 1893, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2384, deeplyIndentedLines: 2798, overlongLines: 1657, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 204 |

## Worst files by length

| File | Lines |
| --- | --- |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/DocumentControl.razor | 455 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 453 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.Sections.cs | 448 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Labour.cs | 445 |
| jpms/Pages/Todos.razor.cs | 444 |
| jpms/Pages/TriageQueue.Apply.cs | 441 |
| api/Features/Ai/Tools/AiDeliveryTools.cs | 440 |
| jpms/Pages/ProjectRequestDetail.razor | 434 |
| api/Features/Registers/RegistersSlices.cs | 433 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 428 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 415 |
| jpms/Pages/ProjectVariations.razor | 411 |
| jpms/Pages/TriageQueue.razor | 409 |
| api/Features/Ai/Tools/AiRegisterTools.cs | 405 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 |
| jpms/Services/HttpLabourStore.cs | 395 |
| jpms/Components/ValuationReportTable.razor | 395 |

Full detail, including every offender list, is in `audit.json`.
