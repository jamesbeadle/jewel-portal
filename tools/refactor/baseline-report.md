# Refactor audit — baseline v22, adopted 15 September 2026

Generated 2026-09-15 from `main` (c428de0), replacing the v21 baseline of 7 September. **Adopted
from drift, not earned by a round** — the second such reading after v20. Duplication was measured
(jscpd 5.2.1); every other check is the same heuristic as v21, now run by the project-process
kit's copy of the audit (`tools/refactor/kit-version` 1.0.0), which reads the C# figures exactly
as the v21 code did — verified by running both against this commit.

## Headline

**787 of 3,963 source files are over the 100-line limit (19.9%)**; the worst file is 604 lines.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 787, totalFiles: 3963, worstFileLines: 604 |
| functionShape | limit: 30, functionsOverLimit: 816, elseBlocks: 1174, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 57, maxWords: 5, maxLength: 40 |
| duplication | clones: 536, duplicatedLines: 6143, totalLines: 259625, duplicatedPercentage: 2.37 |
| naming | bannedAbbreviationHits: 675, unprefixedBooleans: 1886 |
| comments | explanatoryCommentLines: 16224, filesWithComments: 2280, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 3018, deeplyIndentedLines: 3111, overlongLines: 1830, measurementIsHeuristic: True |
| inventory | pages: 102, components: 144, orphanComponents: 6, averagePageLines: 213 |

## Where v22 came from

Between v21 (7 September, after round 19) and this reading, eight days of feature work landed on
`main` with no gate running in CI — the audit existed only as a script a refactor session ran by
hand. 349 source files arrived (3,614 → 3,963) and 78 of them are over the limit: the Sales pane
and estimates (`SalesLeadDetail` 505 → 604, `SalesEstimateDetail` 442, `EstimateDocumentRenderer`
507), the H&S audit, the supplier account, the work-order and valuation tagging changes, and the
3D model on the lead page. Eight of the eleven ratcheted figures moved the wrong way; duplication
(2.44% → 2.37%), inline hex colours (43 → 40) and orphan components (10 → 6) improved.

| Ratcheted figure | v21 | **v22** | Δ |
| --- | --- | --- | --- |
| Files over 100 lines | 709 | 787 | +78 |
| Worst file (lines) | 525 | 604 | +79 |
| Functions over 30 lines | 728 | 816 | +88 |
| `else` blocks | 1,131 | 1,174 | +43 |
| Duplication | 2.44% (459 clones) | **2.37% (536 clones)** | better |
| Explanatory comment lines | 14,501 | 16,224 | +1,723 |
| Inline hex colours | 43 | **40** | −3 |
| Orphan components | 10 | **6** | −4 |
| Long member-chain lines | 2,567 | 3,018 | +451 |
| Deeply indented lines | 2,815 | 3,111 | +296 |
| Overlong function names | 41 | 57 | +16 |

This is the last reading that can drift unnoticed. From this commit `.github/workflows/code-audit.yml`
runs the audit and the gate on every pull request and every push to `main`, so the next figure to
move the wrong way fails the check that introduced it; and Your Business Today raises
`REFACTOR: round N` on the project every ten pushes to `main`, so the extraction loop runs on a
rhythm rather than when someone remembers. The v20 finding about the orphans still stands in part:
`TabRow`, `FilterChips` and `ConfirmDialog` have since found callers; the six orphans now are
`ClientDefectForm`, `ChatIcon`, `MetaRow`, `Pagination`, `ProjectDetailView` and
`SubcontractorTable` — either a page should compose them or they should go.

## The journey so far

| Figure | 22 Aug (v1) | R17 (v18) | R18 (v19) | 7 Sep (v20) | R19 (v21) | 15 Sep (v22) |
| --- | --- | --- | --- | --- | --- | --- |
| Files over 100 lines | 385 | 658 | 668 | 713 | **709** | 787 † |
| Worst file (lines) | 4,961 | 475 | 475 | 621 | **525** | 604 † |
| Average page length | 544 | 203 | 201 | 209 | **209** | 213 † |
| Duplication | 4.16% | 2.78% | 2.70% | 2.44% | **2.44%** | **2.37%** |
| `else` blocks | 1,087 | 1,156 | 1,153 | 1,133 | **1,131** | 1,174 † |
| Functions over 30 lines | — | 694 | 692 | 731 | **728** | 816 † |

† Adopted from feature drift with no gate in CI; the figures above the line are the floor the
next round ratchets from.

## Worst files by length

| File | Lines | Note |
| --- | --- | --- |
| jpms/Pages/SalesLeadDetail.razor | 604 | jpms — grew 99 lines since v21 (estimates, proposals, the 3D model panel) |
| jpms/Pages/SalesStrategyDetail.razor | 526 | jpms |
| api/Data/JpmsContext.Model.cs | 517 | one `OnModelCreating` — a partial per entity area |
| api/Features/Sales/Documents/EstimateDocumentRenderer.cs | 507 | api — renderer recipe, new since v21 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 | worker |
| jpms/Pages/Imagine.razor | 462 | jpms |
| jpms/Pages/SalesInbox.razor | 457 | jpms |
| jpms/Services/Navigation/SidebarFolders.cs | 449 | jpms |
| jpms/Pages/SalesEstimateDetail.razor | 442 | jpms — new since v21 |
| jpms/Services/HttpLabourStore.cs | 436 | jpms |
| jpms/Pages/XeroAllocation.razor | 435 | jpms |
| jpms/Pages/AdminKpis.razor | 429 | jpms |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 | jpms |
| jpms/Components/ProjectDetailsEditor.razor | 428 | jpms |
| jpms/Pages/SubcontractorDetail.razor | 419 | jpms |
| jpms/Pages/ProjectVariations.razor | 417 | jpms |
| jpms/Pages/CostCodes.razor | 415 | jpms |
| jpms/Pages/DocumentControl.Filing.cs | 409 | jpms |
| jpms/Pages/ProjectValuation.razor | 405 | jpms |
| jpms/Pages/TriageQueue.razor | 403 | jpms |

## Next round, named

Round 20 is the first the cadence raises. The Sales pages lead if the round can build jpms:
**SalesLeadDetail (604)** and **SalesStrategyDetail (526)**, then **SalesEstimateDetail (442)**,
**Imagine (462)** and **SalesInbox (457)** — markup plus concern partials per
`docs/refactor/design-patterns.md` §1, the panels extracted as widgets under `jpms/Features/Sales`.
On the api side the list stands from v21 with one addition: **JpmsContext.Model (517)** — a partial
per entity area; **EstimateDocumentRenderer (507)** and the two renderers from v21 by the renderer
recipe (partials, verified by masked-PDF comparison old against new); **GraphMailClient (475)** by
the `XeroClient` division. Worst-first among files the round's environment can build is the rule;
what it cannot build, it names in its report.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
