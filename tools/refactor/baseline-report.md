# Refactor audit — baseline v14, after round 13

Generated 2026-09-01 from `refactor/round-13`, replacing the round-12 (v13) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 629, totalFiles: 3290, worstFileLines: 837 |
| functionShape | limit: 30, functionsOverLimit: 702, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 482, duplicatedLines: 6132, totalLines: 215515, duplicatedPercentage: 2.85 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1415 |
| comments | explanatoryCommentLines: 13728, filesWithComments: 1770, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2803, overlongLines: 1749, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 262 |

## Round 13 — the allocation workbench divides

XeroAllocation.razor — the worst file after round 12 — fell 929 → 495, half its August size,
through the row-coordinator pass the v13 report named:

- **The mega-row is a family**: QueueLineRow (the two coding dropdowns, one primary button,
  the menu), LabourLineRow (settlement marking with the inline identity fix), DisputedLineRow
  (the dispute workbench) and AllocatedSummaryRow (where the line went, the write-back badge,
  the undo family) — all sharing LedgerLineIdentityCell, so the table never disagrees with
  itself about what a line is. The picks stay page state: they survive paging and decide
  which project tab a line sits under.
- **The line-pure rules moved to the module**: IsAwaitingApproval, LineMetaText, the
  settlement month and cover rules joined SignedNet in XeroLedgerDisplay — one home shared by
  the page, the rows and the dialogs.
- **The chrome followed**: SendLinesModal serves both directions of the bulk recode with the
  direction's wording as its interface; DisputeLineModal, InvoiceViewerActions (the viewer's
  three inline states), BulkSelectionBar, AllocationTabBar (owning the tab CSS),
  MatchedLinesBanner (owning its own arm-then-confirm — nothing else cared) and
  LabourSectionStrip complete the set. Twelve components in `Features/Xero/Allocation/`.
- **Division signatures**: filesOverLimit 626 → 629 (three of the twelve components clear
  100 lines) and duplication 2.83% → 2.85% — jscpd's new clone pairs, inspected pair by
  pair, are the rows' shared identity/net cells and `[Parameter]` blocks: interface surface,
  no cloned logic. unprefixedBooleans 1393 → 1415 is the same story (`Busy`, `Arrived`,
  `SplitOpen`), matching the component conventions the codebase already has.

## The journey so far

| Figure | 22 Aug (v1) | R8 (v9) | R9 (v10) | R10 (v11) | R11 (v12) | R12 (v13) | R13 (v14) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 958 | 958 | 954 | 954 | 929 | **837** |
| Average page length | 544 | 275 | 274 | 272 | 272 | 266 | **262** |
| Duplication | 4.16% | 3.44% | 3.20% | 2.81% | 2.82% | 2.83% | 2.85% † |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | 700 | 703 | 700 | 704 | 703 | **702** |
| Files over 100 lines | 385 | 628 | 621 | 618 | 621 | 626 | 629 † |

† The division signature: explicit-interface components where page markup stood, and jscpd
counting their shared parameter blocks. Two pages have now been halved in two rounds
(TriageQueue 954 → 470, XeroAllocation 929 → 495), and the worst file is under 850 for the
first time.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/ProjectVariationDetail.razor | 837 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 830 |
| jpms/Pages/ProjectRequestDetail.razor | 802 |
| jpms/Pages/LabourOverview.razor | 785 |
| jpms/Pages/ProjectProgramme.razor | 750 |
| jpms/Pages/TriageQueue.Compose.cs | 742 |
| jpms/Pages/ProfitSummary.razor | 736 |
| jpms/Pages/CashForecast.razor | 665 |
| jpms/Pages/WeeklyCashflow.razor | 604 |
| jpms/Services/Excel/ExcelWorkbookWriter.cs | 589 |
| api/Features/Ai/Tools/AiCommercialTools.cs | 560 |
| jpms/Pages/ProjectWorkOrders.razor | 548 |
| jpms/Pages/TriageQueue.Outbox.cs | 520 |
| api/Features/Ai/Sources/AiSourceReader.cs | 518 |
| jpms/Components/ValuationReportTable.razor | 517 |

## Round 14, named

The top of the table is now the **project detail pages** — ProjectVariationDetail (837),
ProjectBidPackageInviteDetail (830), ProjectRequestDetail (802) — which share a shape (header,
correspondence thread, register tables, action modals) that two rounds of the panel recipe now
fit; the recipe is proven, so round 14 works down that trio. LabourOverview and
ProjectProgramme's Gantt components queue behind them, with ExcelWorkbookWriter and the two
big TriageQueue partials (Compose 742, Outbox 520) as the .cs tail.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
