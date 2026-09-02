# Refactor audit

Generated 2026-09-02 13:37 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 666, totalFiles: 3475, worstFileLines: 475 |
| functionShape | limit: 30, functionsOverLimit: 695, elseBlocks: 1153, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 480, duplicatedLines: 5929, totalLines: 217893, duplicatedPercentage: 2.72 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1537 |
| comments | explanatoryCommentLines: 13719, filesWithComments: 1906, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2390, deeplyIndentedLines: 2866, overlongLines: 1660, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 203 |

## Worst files by length

| File | Lines |
| --- | --- |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
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
| jpms/Services/HttpLabourStore.cs | 400 |
| jpms/Components/ValuationReportTable.razor | 395 |
| jpms/Pages/XeroAllocation.razor | 389 |
| api/Data/JpmsContext.Model.cs | 386 |
| api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs | 381 |
| api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs | 378 |

Full detail, including every offender list, is in `audit.json`.
