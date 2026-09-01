# Refactor audit

Generated 2026-09-01 10:28 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 556, totalFiles: 3123, worstFileLines: 1399 |
| functionShape | limit: 30, functionsOverLimit: 676, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 775, duplicatedLines: 9087, totalLines: 220321, duplicatedPercentage: 4.12 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1323 |
| comments | explanatoryCommentLines: 13768, filesWithComments: 1620, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2791, overlongLines: 1795, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 356 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1399 |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/DocumentControl.razor | 898 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectVariations.razor | 813 |
| jpms/Pages/Todos.razor | 785 |
| api/Features/Ai/Tools/Actions/CommercialActions.cs | 776 |
| jpms/Components/ValuationInvoicesSection.razor | 773 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.cs | 768 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| api/Features/Xero/XeroClient.Reads.cs | 749 |
| jpms/Pages/ProjectRequests.razor | 745 |
| jpms/Pages/TriageQueue.Compose.cs | 737 |
| jpms/Features/Triage/AttachmentPicker.razor | 716 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 688 |

Full detail, including every offender list, is in `audit.json`.
