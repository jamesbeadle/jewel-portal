# Refactor audit — baseline v8, after round 7

Generated 2026-09-01 from `refactor/round-7`, replacing the round-6 (v7) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 624, totalFiles: 3245, worstFileLines: 1016 |
| functionShape | limit: 30, functionsOverLimit: 697, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 44, maxWords: 5, maxLength: 40 |
| duplication | clones: 581, duplicatedLines: 7532, totalLines: 220933, duplicatedPercentage: 3.41 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1333 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1731, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2400, deeplyIndentedLines: 2819, overlongLines: 1784, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 275 |

## Round 7 — three pages lighter, one date to rule them

- **DateText joined the global usings** (DateFormats, beside MoneyFormats): six files had
  hand-written the same "d MMM yyyy or em-dash" helper. The two DateTimeOffset variants keep
  their own zone handling locally, deliberately. SignedNet — the credit-note negation — got
  one Xero home too.
- **XeroAllocation finished shedding its dialogs**: the split editor became SplitEditorForm
  (editing the page's draft list in place, so a split started in the invoice viewer still
  carries on in the standalone modal), and the document viewer's fetch/chips/iframe became
  the self-contained InvoiceDocumentPreview. The page's `@code` block is gone — pure markup
  over its code-behind, 1,095 → 930.
- **LabourOverview's first division, 981 → 786**: four of its six markup fragments became
  components under Features/Labour (WorkerPlacementStrip, SiteVisitsDetail,
  SettlementVerdictPill, SettlementScheduleDetail), the settlement state that had grown
  inside `@code` moved to its own concern partial, and the status/absence words joined a
  shared LabourDisplay module.
- **ProjectRequestDetail's email-this-record modal became EmailDraftStagingModal**
  (Features/Requests) — the chain-or-fresh picker and result card as a pure view, 874 → 805.

## The journey so far

| Figure | 22 Aug (v1) | R2 (v3) | R3 (v4) | R4 (v5) | R5 (v6) | R6 (v7) | R7 (v8) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 1,399 | 1,152 | 1,152 | 1,101 | 1,029 | **1,016** |
| Average page length | 544 | 356 | 333 | 284 | 283 | 279 | **275** |
| Duplication | 4.16% | 3.08% | 3.25% | 3.42% | 3.41% | 3.39% | **3.41%** ‡ |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,182** |
| Overlong function names | — | 45 | 45 | 45 | 44 | 44 | **44** |
| Functions over 30 lines | — | — | — | 700 | 700 | 699 | **697** |
| Files over 100 lines | 385 | 552 | 570 | 618 | 618 | 622 | 624 † |

‡ Same clone count, +39 lines: removing the twins' identical DateText from AgedPayables and
AgedReceivables made their surrounding mirror-regions contiguous, so the detector now measures
one longer clone where two shorter ones sat either side of the helper. The twins remain a
deliberate mirror (ACCPAY vs ACCREC); no new duplication involves any of this round's files —
verified against the clone pairs directly.

† The division method's signature, as every round: three new 100–130-line components in
exchange for three pages each ~200 lines lighter.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/TriageQueue.razor | 1016 |
| jpms/Pages/XeroAllocation.razor | 930 |
| jpms/Pages/ProfitSummary.razor | 846 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 834 |
| jpms/Pages/ProjectRequestDetail.razor | 805 |
| jpms/Pages/LabourOverview.razor | 786 |
| jpms/Pages/ProjectProgramme.razor | 752 |
| jpms/Pages/TriageQueue.Compose.cs | 726 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |
| api/Data/JpmsContext.cs | 592 |

## Round 8, named

TriageQueue.razor (1,016) is back on top — its remaining mass is the inbox list pane and the
email pane, both candidates for the Queue component family that already holds TriageEmailRow
and TriageMessageDetail. Behind it, ProfitSummary (846) and ProjectVariationDetail (839) await
their first serious division, and the api's long tail (AiRecordTools, WorkOrderPoRenderer,
SendMailboxEmailHandler) has not moved since round 4 — a backend round is due.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
