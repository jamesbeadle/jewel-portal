# Refactor audit

Generated 2026-09-02 09:56 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 658, totalFiles: 3445, worstFileLines: 475 |
| functionShape | limit: 30, functionsOverLimit: 695, elseBlocks: 1161, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 487, duplicatedLines: 6073, totalLines: 217080, duplicatedPercentage: 2.8 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1521 |
| comments | explanatoryCommentLines: 13679, filesWithComments: 1889, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2384, deeplyIndentedLines: 2798, overlongLines: 1659, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 207 |

## Worst files by length

| File | Lines |
| --- | --- |
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
| api/Features/Ai/Sources/AiFiledDocuments.cs | 428 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 415 |
| jpms/Pages/ProjectVariations.razor | 411 |
| api/Features/Ai/Tools/AiRegisterTools.cs | 405 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 |
| jpms/Services/HttpLabourStore.cs | 395 |

Full detail, including every offender list, is in `audit.json`.
