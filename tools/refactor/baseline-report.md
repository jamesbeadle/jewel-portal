# Refactor audit — baseline v2, after round 1

Generated 2026-09-01 from `main` (0c88419), replacing the 2026-08-22 baseline report. The audit
now carries two checks the first baseline did not have: **prose** (long member chains, deep
indentation, overlong lines) and **functionNames** (a name over 5 words / 40 characters signals
the function wants to be Class.method).

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 520, totalFiles: 3070, worstFileLines: 1471 |
| functionShape | limit: 30, functionsOverLimit: 671, elseBlocks: 1184, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 45, maxWords: 5, maxLength: 40 |
| duplication | clones: 756, duplicatedLines: 8864, totalLines: 219680, duplicatedPercentage: 4.03 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1324 |
| comments | explanatoryCommentLines: 13773, filesWithComments: 1577, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2401, deeplyIndentedLines: 2766, overlongLines: 1783, measurementIsHeuristic: True |
| inventory | pages: 92, components: 129, orphanComponents: 6, averagePageLines: 390 |

## Round 1 — what it changed

Three columns, because two things happened between the baselines: the codebase **grew** (2,573 →
3,070 audited files; 13 new pages) and then round 1 refactored it. "1 Sep before" is the same
tree round 1 started from, measured with the same extended audit, so the right-hand movement is
the refactor's alone.

| Figure | 22 Aug baseline | 1 Sep before | 1 Sep after round 1 | Round-1 movement |
| --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 4,659 | **1,471** | −68% |
| Average page length | 544 | 522 | **390** | −25% |
| Duplication | 4.16% | 4.07% | **4.03%** | −0.04 pts while adding ~60 files |
| `else` blocks | 1,087 | 1,234 | **1,184** | −50 |
| Functions over 30 lines | 602 | 673 | **671** | −2 |
| Unprefixed booleans | 1,165 | 1,343 | **1,324** | −19 |
| Inline hex colours | 50 | 50 | **43** | −7 |
| Long member chains (new) | — | 2,415 | **2,401** | −14 |
| Overlong function names (new) | — | 45 | **45** | unchanged — round 2 material |
| Files over 100 lines | 385 | 471 | **520** | +49 — see note |
| Deeply indented lines (new) | — | 2,688 | **2,766** | +78 — see note |

The two figures that moved the wrong way are artefacts of the method, accepted knowingly in the
locking commits: **files over the limit** rose because ten giant files became roughly sixty
mid-size concern partials — the total mass shrank, but each part still exceeds 100 lines until
the next rounds of division; **deeply indented lines** rose because ~10,000 lines of `@code`
moved from `.razor` files (which that check cannot see) into `.cs` partials (which it can) — the
measurement got more honest, the code did not get deeper.

### The ten breakdowns

Every step was a build-and-test-verified division; each file's history is one commit per step on
the merged `refactor/prose-standard` branch.

| Target (22 Aug size) | Now: largest part | Parts |
| --- | --- | --- |
| TriageQueue.razor (4,961) | 1,152 markup | 10 files: markup + 9 concern partials |
| ProjectBidPackageInviteDetail.razor (3,219) | 1,399 markup | 11 files |
| ProjectRequestDetail.razor (2,618) | 874 markup | 8 files |
| XeroAllocation.razor (2,175) | 1,095 markup | 8 files |
| XeroClient.cs (2,157) | 749 (Reads partial) | 10 files: interface, null client, exception, core + 6 API-area partials |
| ProfitSummary.razor (1,837) | 859 markup | 8 files |
| LabourOverview.razor (1,719) | 981 markup | 5 files |
| ChatPanel.razor (1,548) | — removed from the codebase before round 1; ProjectVariationDetail (1,583 → 839) substituted | 7 files |
| ProjectWorkOrders.razor (1,505) | 552 (code-behind) | 5 files |
| CashForecast.razor (1,436) | 701 markup | 6 files |

### The abstractions round 1 extracted

Shared widgets: `ApprovedSessionGate` (the session/approval scaffold, adopted by 24 pages),
`TriageEmailRow`, `EmailListPager`, `TriageMessageDetail`, `NoticePanel`. Display modules, each
replacing pasted copies: `MoneyFormats` (37 copies), `FileSizeFormat` (14), `TriageEmailDisplay`,
`TriagePathways`, `JewelDocumentStyle` (six PDF renderers' palette/font/layout scaffolding), plus
`GlobalUsings` for the client (182 files' worth of repeated using blocks). The full pattern
write-up is `docs/refactor/design-patterns.md`.

A note on the word "performance": these are code-quality measures — readability, size,
duplication. Runtime behaviour was deliberately unchanged (that was the constraint every commit
was verified against); any runtime effect of round 1 is incidental and unmeasured.

## Worst files by length

The old top ten are gone from this list; what remains is round 2's queue.

| File | Lines |
| --- | --- |
| api/Features/MailboxIntake/Graph/MailboxGraphClient.cs | 1471 |
| jpms/Pages/ProjectProgramme.razor | 1413 |
| jpms/Pages/WeeklyCashflow.razor | 1410 |
| jpms/Pages/ProjectBidPackageInviteDetail.razor | 1399 |
| api/Features/Ai/Tools/AiToolCatalogue.cs | 1199 |
| jpms/Components/ValuationReportTable.razor | 1169 |
| jpms/Pages/TriageQueue.razor | 1152 |
| jpms/Pages/ProjectValuation.razor | 1130 |
| jpms/Pages/XeroAllocation.razor | 1095 |
| jpms/Pages/ProjectLabour.razor | 1002 |
| jpms/Pages/Subcontractors.razor | 984 |
| jpms/Components/FinancialsTable.razor | 982 |
| jpms/Pages/LabourOverview.razor | 981 |
| contracts/Ai/ModalCatalog.cs | 937 |
| jpms/Pages/DocumentControl.razor | 898 |
| jpms/Pages/ProjectRequestDetail.razor | 874 |
| jpms/Pages/ProfitSummary.razor | 859 |
| jpms/Pages/ProjectVariationDetail.razor | 839 |
| jpms/Pages/ProjectVariations.razor | 813 |
| jpms/Pages/Todos.razor | 785 |

Round 2 starts at the top of this table: MailboxGraphClient (a backend client, the XeroClient
recipe applies directly), then ProjectProgramme and WeeklyCashflow (pages, the TriageQueue
recipe), and the markup halves of round 1's own breakdowns, which shrink further as their
sections become widgets.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
