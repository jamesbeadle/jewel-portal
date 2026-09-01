# Refactor audit — baseline v7, after round 6

Generated 2026-09-01 from `refactor/round-6`, replacing the round-5 (v6) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 622, totalFiles: 3233, worstFileLines: 1029 |
| functionShape | limit: 30, functionsOverLimit: 699, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 581, duplicatedLines: 7493, totalLines: 220821, duplicatedPercentage: 3.39 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1331 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1723, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2819, overlongLines: 1782, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 279 |

## Round 6 — the workbench twins

The two pages that inherited the top of the table each gave up their self-contained pieces:

**ProjectBidPackageInviteDetail, 1,101 → 834** — three modals and a table became components,
each following the round-3 modal pattern (own state, reset on open, one pick or outcome back
to the page):

- **SubcontractorInvitePickerModal** — the directory search, trade filter, ticks and quick-add
  row; the page saves and invites the pick beside its sibling, the local-search confirm.
- **PackageDetailsEditorModal** — the summary + line-schedule drafts and the
  every-line-needs-a-cost-code rule; the page keeps the two save commands behind one Save.
- **TenderInviteComposerModal** — the whole invite flow: envelope, default letter, the draft
  persisted on the package (loaded on open, saved quietly on close) and the send itself, with
  the send error now shown inside the dialog. The page turns each outcome into its banner.
- **TenderQuoteComparisonTable** — one column per tender, the Won badge, the prospect's
  add-to-directory act and per-column Award.

**XeroAllocation, code-behind 686 → 224** — the catch-all divided by concern into Tabs (project
and labour grouping, tab switching, last-tab memory) and RowCoding (per-row picks, menus,
buckets, options, selection), with two strays (SendToProjectAsync, ResolveDisputeAsync)
rejoining their siblings in SendTo. Its markup gave up **DisputeDiscussionModal** (a pure view
— the page still re-resolves the line from the store each render) and **LedgerLineSummary**,
the supplier · description card that all four line modals had each drawn by hand.

Also carried into this branch: the round-5 worker compile-list fix for round 4's Xero read
partials (already applied to `main` for the deploy).

## The journey so far

| Figure | 22 Aug (v1) | R1 (v2) | R2 (v3) | R3 (v4) | R4 (v5) | R5 (v6) | R6 (v7) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,471 | 1,399 | 1,152 | 1,152 | 1,101 | **1,029** |
| Average page length | 544 | 390 | 356 | 333 | 284 | 283 | **279** |
| Duplication | 4.16% | 4.03% | 3.08% | 3.25% | 3.42% | 3.41% | **3.39%** |
| `else` blocks | 1,087 | 1,184 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | — | 45 | 45 | 45 | 44 | **44** |
| Functions over 30 lines | — | — | — | — | 700 | 700 | **699** |
| Files over 100 lines | 385 | 520 | 552 | 570 | 618 | 618 | 622 † |

† +4: dividing a 686-line code-behind into three concern partials and giving two big modals
their own components trades one giant for a few mid-size files — the division method's
signature, as every round. The worst file has fallen 4,961 → 1,029 while it happened.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/XeroAllocation.razor | 1029 |
| jpms/Pages/TriageQueue.razor | 1016 |
| jpms/Pages/LabourOverview.razor | 981 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |
| api/Data/JpmsContext.cs | 592 |

## Round 7, named

XeroAllocation.razor (1,029) still leads: what remains is the allocation table itself and the
invoice-document viewer that hosts the inline split and dispute editors — the page's real
workbench, extractable only as a set (the viewer, the split editor and the row controls share
one coding state). Behind it, LabourOverview (981) and ProjectRequestDetail (874) have not yet
been divided at all, and a codebase-wide `DateText` helper is now hand-written in at least five
places — a display module (the TriageEmailDisplay pattern) wants to absorb it.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
