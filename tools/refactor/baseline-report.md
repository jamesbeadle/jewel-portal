# Refactor audit — baseline v5, after round 4

Generated 2026-09-01 from `refactor/round-4`, replacing the round-3 (v4) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 618, totalFiles: 3219, worstFileLines: 1152 |
| functionShape | limit: 30, functionsOverLimit: 700, elseBlocks: 1182, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 584, duplicatedLines: 7540, totalLines: 220495, duplicatedPercentage: 3.42 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1325 |
| comments | explanatoryCommentLines: 13751, filesWithComments: 1709, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2821, overlongLines: 1791, measurementIsHeuristic: True |
| inventory | pages: 92, components: 131, orphanComponents: 6, averagePageLines: 284 |

## Round 4 — the breadth round

Where rounds 1–3 went deep on the worst file, round 4 went wide:

- **37 more files got the markup/code-behind division** — every remaining page and component in
  the 300–700 line band with a movable `@code` block, from SideNav and RoleHome to the drawing,
  contract, reconciliation and correspondence panels. Average page length fell 333 → **284**
  (it was 544 at the very start — down 48%).
- **Six AI action catalogues divided by their own area groups** (SiteAndProgress,
  ProjectsAndTenders, VariationsAndValuations, SubcontractorsAndLeads at their `Area:`
  transitions; ProcurementActions at its own section dividers), and XeroClient.Reads divided
  again into Suppliers and Cash partials.
- **The header tax got a structural fix**: MoneyFormats, FileSizeFormat and the Excel service
  moved into the client's global usings, and 123 files dropped the import lines the division
  rounds had been re-stamping on every new partial.

## The journey so far

| Figure | 22 Aug (v1) | 1 Sep pre-refactor | R1 (v2) | R2 (v3) | R3 (v4) | R4 (v5) |
| --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 4,659 | 1,471 | 1,399 | 1,152 | **1,152** |
| Average page length | 544 | 522 | 390 | 356 | 333 | **284** |
| Duplication | 4.16% | 4.07% | 4.03% | 3.08% | 3.25% | **3.42%** ‡ |
| `else` blocks | 1,087 | 1,234 | 1,184 | 1,182 | 1,182 | **1,182** |
| Files over 100 lines | 385 | 471 | 520 | 552 | 570 | 618 † |

‡ The clone detector counts each new file's small header block; the big consolidations
(GlobalUsings ×3 projects, JewelDocumentStyle, MoneyFormats, the blob shell) still hold — the
codebase is ~40k lines larger than v1 measured, with fewer duplicated lines than it had then.

† The division method's signature, as every round: mass moves out of giants into mid-size
concern partials. The figure turns when partials divide below 100 — that is the long tail of
this campaign, not a single round.

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
| jpms/Pages/TriageQueue.Compose.cs | 737 |
| jpms/Pages/CashForecast.razor | 701 |
| jpms/Pages/XeroAllocation.razor.cs | 686 |
| jpms/Pages/WeeklyCashflow.razor | 683 |
| api/Features/Ai/Tools/AiRecordTools.cs | 631 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.cs | 623 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 607 |

## Round 5, named

The top of the table is now almost entirely the markup halves of the four biggest workbench and
detail pages — files that only shrink by componentising their sections. Round 5 is therefore the
**TriageQueue finale**: the new-email composer, the inline reply composer and the triage bar
share helpers (recipient parsing, HTML-content checks) that want to become one compose feature —
extracting them together takes TriageQueue.razor and TriageQueue.Compose.cs down hard, and the
same compose components then serve XeroAllocation's and ProjectRequestDetail's email surfaces.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
