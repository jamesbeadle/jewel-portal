# Refactor audit

Generated 2026-09-02 09:39 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 657, totalFiles: 3429, worstFileLines: 492 |
| functionShape | limit: 30, functionsOverLimit: 694, elseBlocks: 1169, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 487, duplicatedLines: 6073, totalLines: 216934, duplicatedPercentage: 2.8 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1507 |
| comments | explanatoryCommentLines: 13684, filesWithComments: 1883, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2390, deeplyIndentedLines: 2798, overlongLines: 1661, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 208 |

## Worst files by length

| File | Lines |
| --- | --- |
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
| api/Features/Ai/Sources/AiFiledDocuments.cs | 428 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 415 |
| jpms/Pages/ProjectVariations.razor | 411 |

Full detail, including every offender list, is in `audit.json`.
