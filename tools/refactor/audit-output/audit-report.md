# Refactor audit

Generated 2026-09-01 11:55 UTC.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 570, totalFiles: 3155, worstFileLines: 1152 |
| functionShape | limit: 30, functionsOverLimit: 681, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 559, duplicatedLines: 7131, totalLines: 219694, duplicatedPercentage: 3.25 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1325 |
| comments | explanatoryCommentLines: 13755, filesWithComments: 1650, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2791, overlongLines: 1791, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 333 |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1101 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| api/Features/Xero/XeroClient.Reads.cs | 747 |
| jpms/Pages/TriageQueue.Compose.cs | 737 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 688 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| jpms/Pages/ProjectWorkOrderAllocation.razor | 678 |
| api/Features/Ai/Tools/Actions/ProcurementActions.cs | 677 |
| api/Features/Ai/Tools/Actions/SiteAndProgressActions.cs | 637 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| jpms/Pages/CostCodes.razor | 622 |
| jpms/Pages/ProjectBuildingControl.razor | 612 |

Full detail, including every offender list, is in `audit.json`.
