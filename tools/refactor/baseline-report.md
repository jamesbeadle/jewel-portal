# Refactor audit — baseline v13, after round 12

Generated 2026-09-01 from `refactor/round-12`, replacing the round-11 (v12) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 626, totalFiles: 3278, worstFileLines: 929 |
| functionShape | limit: 30, functionsOverLimit: 703, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 478, duplicatedLines: 6094, totalLines: 215122, duplicatedPercentage: 2.83 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1393 |
| comments | explanatoryCommentLines: 13728, filesWithComments: 1759, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2803, overlongLines: 1770, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 266 |

## Round 12 — the triage queue's fragments become components

The queue's list of pane fragments — sat at the top of this report since the audit began — is
gone. TriageQueue.razor fell 954 → 470, and for the first time it is not the worst file in the
codebase:

- **The record-linking vocabulary moved first**: RecordTypeOptions and its label family
  (pathway, singular, plural) are now `Features/Triage/RecordLinkVocabulary`, a static module
  both the page and the new panels share — the move that unblocked everything below.
- **Ten components where four fragments stood**: TriageBar and OutboxOnlyBar (the strip above
  the split); QueueEmailReadingPane, TaggedEmailManagePanel, DiscardedEmailPanel and
  EmailMirrorPane (the email window's four modes); QueueInboxList, DiscardedInboxList and
  TaggedInboxBrowser (the inbox pane's three lists); TriageNoticesStack (the cleared-selection
  notices). Each is a pure view with an explicit `[Parameter]` interface — the decisions stay
  on the page because parking a triage snapshots the page's fields — and the styling helpers
  (DiscardTabClass, SortLinkClass, PathwayFilterChipClass) travelled to the components whose
  controls they style. The page's reply composer passes into the reading pane as child
  content: one editor, one draft, still page-owned.
- **Division signatures, honestly noted**: filesOverLimit 621 → 626 (ten new 40–230-line
  components where 480 lines of fragments stood) and duplication 2.82% → 2.83% (jscpd's new
  clone pairs are the panels' shared thread-view `[Parameter]` blocks — the explicit
  interfaces themselves, inspected pair by pair; no logic is cloned). unprefixedBooleans
  1348 → 1393 is the same story: the new parameters (`Busy`, `Arrived`, `HasNext`) follow the
  component conventions the codebase already has.

## The journey so far

| Figure | 22 Aug (v1) | R7 (v8) | R8 (v9) | R9 (v10) | R10 (v11) | R11 (v12) | R12 (v13) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,016 | 958 | 958 | 954 | 954 | **929** |
| Average page length | 544 | 275 | 275 | 274 | 272 | 272 | **266** |
| Duplication | 4.16% | 3.41% | 3.44% | 3.20% | 2.81% | 2.82% | 2.83% † |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 44 | **44** |
| Functions over 30 lines | — | 697 | 700 | 703 | 700 | 704 | **703** |
| Files over 100 lines | 385 | 624 | 628 | 621 | 618 | 621 | 626 † |

† The division signature: ten explicit-interface components where four page fragments stood,
and jscpd counting their shared parameter blocks. The worst file is now XeroAllocation.razor —
the first time since the audit began that it isn't TriageQueue.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/XeroAllocation.razor | 929 |
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

## Round 13, named

XeroAllocation.razor (929) takes the top spot and the same treatment: its allocation mega-row
— four per-status variants over ~15 page members, with the per-line picks already gathered in
the RowCoding partial — becomes a row-coordinator component the way CashflowEntryRow models
it. Behind it, the project detail pages (ProjectVariationDetail, ProjectBidPackageInviteDetail,
ProjectRequestDetail) share a shape — header, tabs, register tables — that round 12's panel
recipe now fits. ProjectProgramme's Gantt chart components and ExcelWorkbookWriter stay on the
list.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
