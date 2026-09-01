# Refactor audit — baseline v15, after round 14

Generated 2026-09-01 from `refactor/round-14`, replacing the round-13 (v14) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 638, totalFiles: 3316, worstFileLines: 785 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 486, duplicatedLines: 6166, totalLines: 215987, duplicatedPercentage: 2.85 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1461 |
| comments | explanatoryCommentLines: 13727, filesWithComments: 1795, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2398, deeplyIndentedLines: 2801, overlongLines: 1726, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 246 |

## Round 14 — the project detail trio

The three record pages at the top of the v14 table went through the panel recipe together —
the third round running it, and the first to take three pages in one:

- **ProjectVariationDetail 837 → 302**: the document pane, details card, request-repair panel,
  lines table, approved-figures panel, approve offer, staged build-up, agreed tender, delete
  panel and decline modal — eleven components in `Features/Variations/`. Where a panel's
  state parks with nothing else (the section editor, the estimate, the staged build-up, the
  tender pick) the component owns it and hands the saved record back; where the status pill
  opens a confirm, the flag is bound.
- **ProjectBidPackageInviteDetail 830 → 266**: the four tab bodies (tender list, details,
  submissions, documents) and four inline modals (line coverage, link drawings, work-order
  email, delete) — eight components in `Features/Procurement/`.
- **ProjectRequestDetail 802 → 435**: the official form (owning its rows), the party panel
  (turning the composite select into typed picks), the variation card, and the three edit
  dialogs (owning their drafts and building their own commands; the page keeps one shared
  send that answers whether the edit took) — seven components in `Features/Requests/`.
- **The pattern that arrived**: all three pages repeated the same reply widget over their
  tagged-email list. `RecordCorrespondencePanel` (Features/Triage/Panels) is that widget once —
  the compose state its own, the tag-read list left to the page, since each record reads by
  its own tags (the variation merges two). Two of the three pages use it now; the request page
  and TenderEnquiryEmailsPanel are next.
- **Held**: comments 13,728 → 13,727, duplication 2.85% → 2.85%, member chains 2,400 → 2,398,
  overlong names 44 → 43. **Division signature**: filesOverLimit 629 → 638 — twelve of the
  twenty-six new components clear 100 lines (the official form panel is 254, an editor with
  two views); the three pages together shed 1,466 lines.

## The journey so far

| Figure | 22 Aug (v1) | R9 (v10) | R10 (v11) | R11 (v12) | R12 (v13) | R13 (v14) | R14 (v15) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 958 | 954 | 954 | 929 | 837 | **785** |
| Average page length | 544 | 274 | 272 | 272 | 266 | 262 | **246** |
| Duplication | 4.16% | 3.20% | 2.81% | 2.82% | 2.83% | 2.85% | **2.85%** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 44 | **43** |
| Functions over 30 lines | — | 703 | 700 | 704 | 703 | 702 | **700** |
| Files over 100 lines | 385 | 621 | 618 | 621 | 626 | 629 | 638 † |

† The division signature: explicit-interface components where page markup stood. Three rounds
of the panel recipe have now taken the five worst pages from 954/929/837/830/802 to
470/495/302/266/435; the average page is down 16 lines in one round, the biggest single-round
move since round 6.

## Worst files by length

| File | Lines |
| --- | --- |
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
| jpms/Pages/Subcontractors.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |

## Round 15, named

LabourOverview (785) and ProjectProgramme (750) lead — the labour page's week/settlement views
and the programme's Gantt chart are the two remaining pages with a clear pane structure the
recipe fits. Behind them the finance pages (ProfitSummary 736, CashForecast 665,
WeeklyCashflow 604) share the table-heavy shape the CashflowEntryRow family already serves.
TriageQueue.Compose (742) is the first .cs at the top: an orchestrator whose step methods want
their own file, not a page's. ExcelWorkbookWriter and AiCommercialTools stay on the list.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
