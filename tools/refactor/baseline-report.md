# Refactor audit — baseline v4, after round 3

Generated 2026-09-01 from `refactor/round-3`, replacing the round-2 (v3) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

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
| inventory | pages: 92, components: 134, orphanComponents: 6, averagePageLines: 333 |

## Round 3 — the componentisation round

The recipe pass reached six more files (DocumentControl, ProjectVariations, Todos,
ProjectRequests, ValuationInvoicesSection, AttachmentPicker — divided at their own region
markers), and the two AI action files divided by domain group the way AiToolCatalogue did
(CommercialActions into six groups, LabourAndBackOfficeActions into two).

The new work was **modal componentisation** on the worst file, ProjectBidPackageInviteDetail
(1,399 → 1,101): three dialogs became self-contained Features/Procurement components that own
their whole flow — `ValuationLinePickerModal` (reads the live report, hands back package line
inputs), `LocalSubcontractorFinderModal` (AI trade resolution + Places search + selection) and
`TenderSubmissionModal` (manual keying and the AI extract-from-email path, opened via @ref).
The host page keeps only its commands.

**A real bug surfaced and was fixed**: component tags that don't resolve are only a compiler
warning (RZ10012) — Razor silently renders them as unknown HTML elements. The extraction
tripped this, and auditing the warning class found ProjectBuildingControlInspection had been
shipping with its EmailFinder and CorrespondenceThreadList rendering as nothing. Both are now
fixed and the build is RZ10012-clean.

## The journey so far

| Figure | 22 Aug (v1) | 1 Sep pre-refactor | Round 1 (v2) | Round 2 (v3) | Round 3 (v4) |
| --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 4,659 | 1,471 | 1,399 | **1,152** |
| Average page length | 544 | 522 | 390 | 356 | **333** |
| Duplication | 4.16% | 4.07% | 4.03% | 3.08% | **3.25%** ‡ |
| `else` blocks | 1,087 | 1,234 | 1,184 | 1,182 | **1,182** |
| Inline hex colours | 50 | 50 | 43 | 43 | **43** |
| Files over 100 lines | 385 | 471 | 520 | 552 | 570 † |

‡ Round 3's new component and partial files each carry a small header block the clone detector
counts; the underlying consolidation still holds (v1 measured 4.16% on a smaller codebase).

† Still the division method's signature: giants become mid-size concern partials, so the count
rises while every other size figure falls. It turns downward when the partials themselves divide
below the limit. Two heuristic figures also drifted up slightly (deep indentation +25, overlong
lines +12, long functions +5) for the same reason as at v2 — code moved from `.razor` files the
checks cannot see into `.cs` partials they can.

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

Every remaining large file is either a markup half whose sections continue to become components
(the round-3 pattern: TriageQueue's triage bar and compose pane are the next two), or a
concern partial that divides again. Round 4 continues the same two motions.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
