# Refactor audit — baseline v24, after round 20

Generated 2026-09-17 from `refactor/round-20` (off `main` at db02819), replacing the v23 baseline adopted at the head of the same branch.

## Headline

**Code quality score 74.9% → 75.3%.** **794 of 4,102 source files are over the 100-line limit (19.4%)**; the worst file is 557 lines.

The round opened on a red gate, so it began by adopting the drifted reading as **v23 — adopted from drift**: since the v22 baseline of 15 September the features shipped had grown files over the limit 787 → 799, functions over the limit 816 → 836, `else` blocks 1,174 → 1,187, explanatory comment lines 16,224 → 16,313, long member chains 3,018 → 3,171, deeply indented lines 3,111 → 3,125 and overlong function names 57 → 58. v24 is what the round earned from there.

## Code quality score

| Element | Reading | Score | Weight | 0% at |
| --- | --- | --- | --- | --- |
| **Standard baseline checks** | | **68.0%** | **60** | |
| Files over the line limit | 794 in 4102 files | 61.3% | 10 | 50% of files |
| Worst file, in limits over | 4.57 | 49.2% | 5 | 9 |
| Functions over the line limit | 836 in 8138 functions | 58.9% | 8 | 25% of functions |
| Else blocks | 1186 in 12046 branches | 80.3% | 5 | 50% of branches |
| Duplication % | 2.34 | 88.3% | 8 | 20 |
| Explanatory comment lines | 16311 in 275.33 thousand lines | 0.0% | 4 | 50 per thousand lines |
| Inline magic values | 134 in 275.33 thousand lines | 97.6% | 4 | 20 per thousand lines |
| Orphan components and functions | 27 in 8282 components and functions | 96.7% | 4 | 10% of components and functions |
| Long member chain lines | 3165 in 275.33 thousand lines | 61.7% | 4 | 30 per thousand lines |
| Deeply indented lines | 3125 in 275.33 thousand lines | 62.2% | 4 | 30 per thousand lines |
| Overlong function names | 58 in 8138 functions | 92.9% | 4 | 10% of functions |
| **Design pattern file count** | | **84.0%** | **10** | |
| Files the patterns predict but are missing | 187 in 2338 predicted files | 84.0% | 10 | 50% of predicted files |
| Entities outside their expected file count | not measured | not measured | — | 50% of entities |
| **Prose** | | **92.6%** | **20** | |
| Conditions with calls tangled inside calls | 278 in 12046 branches | 90.8% | 8 | 25% of branches |
| Conditions compared to a raw literal | 246 in 12046 branches | 91.8% | 6 | 25% of branches |
| Accessor names that want to be a property | 34 in 8138 functions | 95.8% | 6 | 10% of functions |

Each element scores 100% with no offenders and falls in a straight line to 0% when its offenders, measured against the size of the codebase, reach the figure in the last column. The score is the weighted average of the elements that could be measured; an element that could not be measured lends its weight to the rest. Weights and zero points are set in `tools/refactor/rules.json` under `score.elements`. The offenders behind every reading are in `tools/refactor/audit-output/audit.json`.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 794, totalFiles: 4102, totalLines: 275335, worstFileLines: 557, worstFileTimesOverLimit: 4.57 |
| functionShape | limit: 30, functionsOverLimit: 836, totalFunctions: 8138, elseBlocks: 1186, ifBlocks: 12046, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 58, maxWords: 5, maxLength: 40 |
| accessorNames | gluedAccessorNames: 34, measurementIsHeuristic: True |
| duplication | clones: 553, duplicatedLines: 6280, totalLines: 268394, duplicatedPercentage: 2.34 |
| naming | bannedAbbreviationHits: 701, unprefixedBooleans: 1913 |
| comments | explanatoryCommentLines: 16311, filesWithComments: 2258, taskMarkers: 53 |
| magicValues | inlineHexColours: 40, inlineStyleAttributes: 64, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 3165, deeplyIndentedLines: 3125, overlongLines: 1840, measurementIsHeuristic: True |
| conditions | tangledConditionLines: 278, literalComparisonLines: 246, measurementIsHeuristic: True |
| orphans | orphanFunctions: 21, functionsExamined: 8138 |
| designPatterns | roleFamilies: 154, predictedFiles: 2338, predictedFilesMissing: 187, entities: 0, entitiesOutOfRange: 0, measurementIsHeuristic: True |
| inventory | pages: 104, components: 144, orphanComponents: 6, averagePageLines: 193 |
| fileAreas | totalFiles: 4962, api: 2144, frontend: 1217, contracts: 629, database: 269, connector: 213, docs: 187, tooling: 136, tests: 114, infrastructure: 40, worker: 13 |

## Round 20 — the Sales pages become components

The first five steps of the plan, in order, all Pass 1 — and all five were the Sales section's long views, so the round reads as one piece of work: every Sales page over the limit is now a short composition of named components that own their own commands, under `jpms/Features/Sales` beside the components that were already there. Every block moved verbatim; names were corrected after the move; no logic was rewritten. Checked by compiling contracts and the whole portal (Razor included) after every step — 0 errors, 0 unknown components — and by a line-for-line "did every original line land somewhere" pass per page; then an independent read of all five breakouts against their originals, whose four real findings were put back before the baseline (below). The test project cannot run in the cloud sandbox (nuget.org is blocked, so no xunit) and nothing in it covers these pages; `api`, `contracts`, `worker` and `tests` are untouched by the round.

- **`jpms/Pages/SalesLeadDetail.razor` 599 → 98**: the header with its Actions menu (`LeadPageHeader`), the details (`LeadDetailsPanel`), the timeline with its kind colours (`LeadTimelinePanel`), the stage ladder (`LeadStageLadder`) and the four dialogs — move (`LeadStageMoveDialog`), Won (`LeadWinDialog`), delete (`LeadDeleteDialog`), log (`LeadActivityLogDialog`) — became components. Each dialog took its command, its draft fields, its busy flag and its error with it, and seeds itself on opening exactly as the page's `OpenX` methods did. The page keeps loading and the open flags (`SalesLeadDetail.razor.cs`, `SalesLeadDetail.Dialogs.cs`).
- **`jpms/Pages/SalesStrategyDetail.razor` 526 → 86**: header (`StrategyPageHeader`), research notice, funnel tiles, approach plan panel, leads table (which took `OpenLead`), the argument, the funnel (which took `FunnelSteps`), and the two plan dialogs. The plan edit dialog owns `SavePlanAsync` and its draft; research, status and drafting stay the page's because they share one busy flag and one notice under the header and drafting outlives its dialog — cutting them out needed seven or more parameters, which is the wrong seam. Code behind in three partials: loading, `Research` (the polling, verbatim), `Commands`.
- **`jpms/Pages/Imagine.razor` 462 → 91** (the public page): the submission form (`ImagineSubmissionForm`: photos, the in-browser resize, consent, submit) and the rounds list (`ImagineRoundList`: like, comment, revise, with `ImagineRoundOutcome` and `ImagineRoundPhotos`). What ran even after a failed post still does (photos cleared, the revise box closed). The page keeps the load and the polling.
- **`jpms/Pages/SalesInbox.razor` 457 → 87**: the message list (took `When`), the opened thread (`SalesInboxThread`: reply, log, toggle, with its logging in `SalesInboxThread.Logging.cs`), one message of a thread, and the lead picker (took `MatchesLeadSearch` and the once-per-visit leads read). The thread resets per open from the page's open count, as `OpenAsync` did, without being re-created — so the picker's list and an in-flight command survive an open exactly as before.
- **`jpms/Pages/SalesEstimateDetail.razor` 442 → 88**: the breakdown editor, its section editor and its line row, the details panel and the document text panel. The page's private `SectionDraft` / `LineDraft` classes became `EstimateBreakdownDraft` / `EstimateSectionDraft` / `EstimateLineDraft`, and the seeding, the reorder and the payload moved onto the draft (`From`, `Move`, `ToPayload`) — verbatim bodies. The two saves stay the page's: the Save button lives in the header and both share the busy flag and the notices.
- **Overflow put in order as it appeared** (each would otherwise have been declared in three files): the lead's headline — contact name, else company — is `LeadHeadline.Headline()`; the sales team / deciders mirror is `SalesAccess.CanWork` / `CanDecide`; the public imagine API (base address, image address, the post with its two failure sentences) is `ImagineRequests`, which `ImagineProposal` now uses too; `Pence` joined `EstimateFigures`; the estimate page's private `Money` was the shared `WholeMoney` (C0, en-GB) under another name and is gone. Seven more copies of the headline rule and four of the roles mirror remain in files this round did not open — they are Pass 2 steps.
- **Put back**: nothing was restored after two attempts. Four findings from the independent read were put back as they were before the baseline: imagine polling had started on every view change rather than only when a round is asked for; the inbox thread had been re-created per open, which re-read the leads list per email and dropped an in-flight busy; the document text had seeded on first render rather than with the page's seed; a plan save had stopped clearing the page's error as it started.
- **Held**: functions over the limit 836, deeply indented lines 3,125, magic values, orphans (6 components, 21 functions), overlong names 58, tangled conditions 278, literal comparisons 246, glued accessors 34, predicted files missing 187. **Improved**: files over the limit 799 → 794, worst file 599 → 557, average page length 212 → 193, `else` blocks 1,187 → 1,186, comment lines 16,313 → 16,311, long member chains 3,171 → 3,165, duplication 2.35% → 2.34%.
- **Division signature**: audited source files 4,053 → 4,102 — five views became 49 more files, none over the limit and none with a function over the limit. No clone pair was introduced; duplication moved by the denominator only and is not claimed as a gain.
- **Left as it was, knowingly** (behaviour, not tidiness): a dialog's busy flag no longer disables the ladder and the Actions menu *behind* the open dialog (unreachable except by cancelling mid-save); a 200 with a JSON `null` body on the public page now leaves the view standing rather than showing "link isn't valid" (the API never answers that). And one fault the move exposed and did **not** fix, because a round never changes behaviour: on the estimate page, if the refresh fails on the way in from the lead page, the three document-text boxes stay blank, and saving one of them sends the other two blank — wiping the record's build time and exclusions. That wants a `FIX:` task.

## The journey so far

| Figure | 22 Aug (v1) | R17 (v18) | R18 (v19) | 7 Sep (v20) | R19 (v21) | 15 Sep (v22) | 17 Sep (v23) | R20 (v24) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Code quality score | — | — | — | — | — | — | 74.9% | **75.3%** |
| Files over 100 lines | 385 | 658 | 668 | 713 | **709** | 787 † | 799 † | **794** |
| Worst file (lines) | 4,961 | 475 | 475 | 621 | **525** | 604 † | 599 † | **557** |
| Average page length | 544 | 203 | 201 | 209 | **209** | 213 † | 212 † | **193** |
| Duplication | 4.16% | 2.78% | 2.70% | 2.44% | **2.44%** | **2.37%** | 2.35% † | **2.34%** |
| `else` blocks | 1,087 | 1,156 | 1,153 | 1,133 | **1,131** | 1,174 † | 1,187 † | **1,186** |
| Functions over 30 lines | — | 694 | 692 | 731 | **728** | 816 † | 836 † | **836** |

† Adopted from feature drift; the figures are the floor the next round ratchets from. The code quality score was first measured on 17 September (project-process kit 1.3.0); its entity file-count element is not measured in this repository yet, so its weight is lent to the rest.

## Worst files by length

| File | Lines |
| --- | --- |
| `api/Data/JpmsContext.Model.cs` | 557 |
| `api/Features/Sales/Documents/EstimateDocumentRenderer.cs` | 507 |
| `worker/MailboxIntake/Graph/GraphMailClient.cs` | 475 |
| `jpms/Services/Navigation/SidebarFolders.cs` | 454 |
| `jpms/Components/ProjectDetailsEditor.razor` | 440 |
| `jpms/Services/HttpLabourStore.cs` | 436 |
| `jpms/Pages/XeroAllocation.razor` | 435 |
| `jpms/Pages/AdminKpis.razor` | 429 |
| `jpms/Components/ManualWorkOrderModal.razor.cs` | 428 |
| `jpms/Pages/SubcontractorDetail.razor` | 419 |
| `jpms/Pages/ProjectVariations.razor` | 417 |
| `jpms/Pages/CostCodes.razor` | 415 |
| `jpms/Pages/DocumentControl.Filing.cs` | 409 |
| `jpms/Pages/ProjectValuation.razor` | 405 |
| `jpms/Pages/TriageQueue.razor` | 403 |

## Next round, named

1. Break `jpms/Components/ProjectDetailsEditor.razor` (440 lines) into components. Component-sized blocks: lines 41–65 (25 lines, taking OnPartyChanged); lines 67–80 (14 lines, taking OnOnBehalfOfClientChanged); lines 82–103 (22 lines, taking OnOrganisationChanged); lines 134–157 (24 lines, taking OnXeroContactChanged).
2. Break `jpms/Pages/XeroAllocation.razor` (435 lines) into components. Component-sized blocks: lines 66–103 (38 lines); lines 162–177 (16 lines); lines 191–209 (19 lines); lines 212–232 (21 lines).
3. Break `jpms/Pages/AdminKpis.razor` (429 lines) into components. Component-sized blocks: lines 88–101 (14 lines); lines 102–167 (66 lines, taking OpenInControlCentre, StartEdit, StartRemove, ConfirmRemoveAsync); lines 178–204 (27 lines, taking SaveEditAsync); lines 206–229 (24 lines, taking AddPersonAsync).
4. Break `jpms/Pages/SubcontractorDetail.razor` (419 lines) into components. Component-sized blocks: lines 41–84 (44 lines); lines 101–150 (50 lines); lines 154–198 (45 lines); lines 203–279 (77 lines).
5. Break `jpms/Pages/ProjectVariations.razor` (417 lines) into components. Component-sized blocks: lines 27–47 (21 lines); lines 51–76 (26 lines); lines 82–94 (13 lines); lines 96–113 (18 lines).

The worst file is now `api/Data/JpmsContext.Model.cs` at 557 lines — one 536-line `OnModelCreating`, a Pass 3 division by entity configuration; the worst view is `jpms/Components/ProjectDetailsEditor.razor` at 440.
