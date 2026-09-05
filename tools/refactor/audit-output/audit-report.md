# Refactor audit

Generated 2026-09-05 05:36 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 677, totalFiles: 3582, worstFileLines: 621 |
| functionShape | limit: 30, functionsOverLimit: 704, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 41, maxWords: 5, maxLength: 40 |
| duplication | clones: 467, duplicatedLines: 5750, totalLines: 222062, duplicatedPercentage: 2.59 |
| naming | bannedAbbreviationHits: 476, unprefixedBooleans: 1563 |
| comments | explanatoryCommentLines: 14404, filesWithComments: 2025, taskMarkers: 52 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 52, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2432, deeplyIndentedLines: 2788, overlongLines: 1702, measurementIsHeuristic: True |
| inventory | pages: 92, components: 134, orphanComponents: 6, averagePageLines: 206 |

## Worst files by length

| File | Lines |
| --- | --- |
| api/Features/Labour/Commands/RunXeroCodingSlice.cs | 621 |
| api/Features/Xero/XeroClient.Writes.cs | 568 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/AdminKpis.razor | 445 |
| jpms/Pages/CostCodes.razor | 438 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 422 |
| api/Data/JpmsContext.Model.cs | 411 |
| jpms/Pages/ProjectVariations.razor | 411 |
| jpms/Pages/TriageQueue.razor | 409 |
| jpms/Services/HttpLabourStore.cs | 407 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 |
| jpms/Components/ValuationReportTable.razor | 395 |
| jpms/Features/Triage/AttachmentPicker.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Pages/XeroAllocation.razor | 389 |
| jpms/Pages/ProjectValuation.razor | 387 |
| api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs | 381 |
| api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs | 378 |

Full detail, including every offender list, is in `audit.json`.
