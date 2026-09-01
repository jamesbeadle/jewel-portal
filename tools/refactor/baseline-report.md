# Refactor audit — baseline v3, after round 2

Generated 2026-09-01 from `refactor/round-2`, replacing the round-1 (v2) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 552, totalFiles: 3125, worstFileLines: 1399 |
| functionShape | limit: 30, functionsOverLimit: 676, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 540, duplicatedLines: 6755, totalLines: 219113, duplicatedPercentage: 3.08 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1323 |
| comments | explanatoryCommentLines: 13771, filesWithComments: 1622, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2791, overlongLines: 1795, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 356 |

## The journey so far

| Figure | 22 Aug (v1) | 1 Sep before round 1 | After round 1 (v2) | After round 2 (v3) |
| --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 4,659 | 1,471 | **1,399** |
| Average page length | 544 | 522 | 390 | **356** |
| Duplication | 4.16% | 4.07% | 4.03% | **3.08%** |
| `else` blocks | 1,087 | 1,234 | 1,184 | **1,182** |
| Inline hex colours | 50 | 50 | 43 | **43** |
| Files over 100 lines | 385 | 471 | 520 | 552 † |

† Still the division method's signature: giants become mid-size concern partials, so the count
rises while every other size figure falls. It turns downward when the partials themselves divide
below the limit. Two heuristic figures also drifted up slightly (deep indentation +25, overlong
lines +12, long functions +5) for the same reason as at v2 — code moved from `.razor` files the
checks cannot see into `.cs` partials they can.

## Round 2 — what it did

Ten more breakdowns, all build-and-test verified, one commit per step:

| Target (v2 size) | Now: largest part | Shape |
| --- | --- | --- |
| MailboxGraphClient.cs (1,471) | 374 (Drafts) | interface + null client + core + 6 concern partials |
| ProjectProgramme.razor (1,413) | 752 markup | code-behind + GanttGeometry, RelevantEvents |
| WeeklyCashflow.razor (1,410) | 683 markup | code-behind + Grid, Moving, GroupsDialog, ItemDialog, Export |
| AiToolCatalogue.cs (1,199) | 301 (Lookup) | Build() concatenates 7 domain partials, order unchanged |
| ValuationReportTable.razor (1,169) | 518 markup | code-behind + 4 concern partials |
| ProjectValuation.razor (1,130) | 379 markup | code-behind + Export, Lifecycle, Invoices, PercentDialog |
| ProjectLabour.razor (1,002) | 473 markup | code-behind + Approval, ManualEntry, Settlement |
| Subcontractors.razor (984) | 518 markup | code-behind + XeroImport, Consolidation |
| FinancialsTable.razor (982) | 363 markup | code-behind + Figures, Export |
| ModalCatalog.cs (937) | 286 (Procurement) | descriptor records own file + 5 domain partials |

And the round's DRY consolidation:

- **The duplication headline** — GlobalUsings for the api (mirrored in the worker, which compiles
  api files directly): 1,097 files dropped their copied using blocks, taking measured duplication
  from 4.03% to 3.08%.
- **SortableColumnHeader** — the sortable table header existed byte-identical in FinancialsTable
  and ProfitSummary; now one widget.
- **FiledEmailList** — the expanded-row "emails filed to this record" list shared by
  ProjectDefects and ProjectInventory; now one widget with a per-page empty message.
- **AzureBlobFileStore** — the blob-storage shell (container-on-first-use, bounded fail-fast
  retries, upload/open/delete) shared by every feature file store; the bid-package, work-order
  and request attachment stores now state only their container name and key scheme, and the
  remaining five-plus stores follow the same path as they are touched.
- **Assessed and deliberately left**: AgedPayables↔AgedReceivables are mirror *concepts* whose
  contract types differ on purpose (ACCPAY vs ACCREC semantics) — merging them would trade
  readability for line count; AdminUsers↔AdminRevokedUsers are already thin scaffolding around
  their panels.

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
| api/Features/Ai/Tools/Actions/CommercialActions.cs | 775 |
| jpms/Components/ValuationInvoicesSection.razor | 773 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.cs | 767 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| api/Features/Xero/XeroClient.Reads.cs | 747 |
| jpms/Pages/ProjectRequests.razor | 745 |
| jpms/Pages/TriageQueue.Compose.cs | 737 |
| jpms/Features/Triage/AttachmentPicker.razor | 716 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 688 |

The list's character has changed: no file is over 1,400 lines, and most of the top twenty are
now the **markup halves** of pages already divided — their next shrink is section-by-section
componentisation (the TriageMessageDetail pattern), not further partial splits. Round 3 is
therefore a different kind of round: extract page sections and modal bodies into components, and
divide the two AI action files that entered the list.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
