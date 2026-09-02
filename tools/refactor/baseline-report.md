# Refactor audit — baseline v19, after round 18

Generated 2026-09-02 from `refactor/round-18`, replacing the round-17 (v18) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 668, totalFiles: 3498, worstFileLines: 475 |
| functionShape | limit: 30, functionsOverLimit: 692, elseBlocks: 1153, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 478, duplicatedLines: 5893, totalLines: 218054, duplicatedPercentage: 2.7 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1544 |
| comments | explanatoryCommentLines: 13669, filesWithComments: 1912, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2365, deeplyIndentedLines: 2656, overlongLines: 1659, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 201 |

## Round 18 — the compose handler under test, the tool files flattened, and the api's giants

Round 18 started from main as merged and moved on (PR #22, then the timesheet fixes and the
valuation export's move into contracts), whose own commits had carried four held figures past
v18 — comments 13,672 → 13,746, member chains 2,384 → 2,390, deep indentation 2,798 → 2,869 and
files over 100 lines 658 → 661. Ten commits: every buildable target the v18 report named,
worst file first, with the round's first target pinned by tests before it was touched.

- **SendMailboxEmailHandler 453 → 110**, the one path that sends real email. First a
  characterisation test file (21 tests over a recording fake Graph client and the real
  collaborators on an in-memory database: the exact call sequence for a new email, a reply and
  a forward; what the staged draft carries; how a reply triages the thread; save-as-draft; a
  failed send; every validation refusal; filing to a record and the client wall; raising a
  request, rolled back when the draft cannot be staged and moved to Open once sent). Then the
  340-line ten-step `HandleAsync` became the orchestration over a `Compose` object carrying
  what each step settles, the steps in partials named for them — Compose, Body, Filing, Draft,
  Outcome — with the tests passing unchanged. One subtlety the tests record: a reply on a
  thread already carrying a record tag files its sent copy under that tag and the pathway
  without a Replied stamp, while the inbound thread is still tagged Replied.
- **LabourAndBackOfficeActions.Labour 453 → 0** and **.BackOffice 264 → 0**: nine partials by
  area (Timesheets, WorkerLinks, MonthEnd, CostCentres, Rates, HealthAndSafety,
  UsefulInformation, Platform, AccessRequests), the shape the other action sources have.
- **WorkOrderPoRenderer.Sections 448 → 0**: Header, Scope, Lines and Signatures partials;
  `AddLinedTable` builds both bordered tables' columns and repeating heading row; the 96-line
  lines table and the 84-line scope wording are methods under thirty each; four helpers that
  were byte-for-byte `JewelDocumentStyle`'s now come from there. Verified as the reconciliation
  renderer was: a released order and an empty draft render byte-identical PDFs old and new.
- **AiDeliveryTools 440 → 55**: a partial per area where each tool is a descriptor method plus a
  named handler, and each result row a projection method returning the same anonymous shape
  (a probe proved the JSON identical). The file held 210 of the codebase's lines over five
  levels of indentation; the handler bodies now sit at two.
- **Todos.razor.cs 444 → 177** + Filters, Scope and Add partials; `Add` (50 lines) is the
  checks, `PostNewItemAsync`, `IsShowing` and `NoteWhereItWent`.
- **TriageQueue.Apply 441 → 80** + ApplyPlan, ApplyFiling and ApplySends partials;
  `DoApplyAll` is the gate, busy flag and catch around `ApplyStepsAsync`; the refusal gauntlet's
  discard and staged-create runs are their own methods called in the same positions.
- **ProjectRequestDetail 434 → 262**: `RequestHeaderBar`, `RequestFactsStrip`,
  `CriticalPathNudge`, `RequestResponsePanel` and `VariationDraftModal` (the draft page-owned
  and bound back field by field) under Features/Requests, with `RequestDisplay` carrying the
  reference, date, dash and status colour they share.
- **RegistersSlices 433 → 0**: RegistersFeature, RegisterItemSlices, PolicyDocumentSlices and
  PolicySignOffSlices — the per-slice files Features/Labour/Commands already has.
- **Held and paid back**: comments 13,746 → 13,669 (below v18's 13,672): section headers the
  new filenames carry, twenty headers earlier divisions had left inside partials already named
  for them (an "Excel export" header inside an .Export.cs), twenty-four comment rulers made of
  dashes alone, and two lines describing what a logo call does. Member chains 2,390 → 2,365:
  `JewelDocumentStyle.CellIndent` names the 1.5 mm cell inset every generated table shares
  (twenty-five `Unit.FromMillimeter(1.5)` in the style and the two renderers divided this
  round — and the heuristic had been reading each decimal literal as a member access; both
  renderers re-render byte-identical against main after the rename, which caught a
  self-referencing first draft of the constant before it left the cloud). Deep
  indentation 2,869 → 2,656, functions over 30 lines 694 → 692 (the nine action arrays the
  heuristic reads as functions cost seven, paid back by real splits), `else` blocks
  1,156 → 1,153, duplication 2.78% → 2.70% (clones 485 → 478), overlong names 43, hex colours
  43, orphans 6. **Division signature**: filesOverLimit 661 → 668 — forty-two new files, fifteen
  of them over 100 (the largest LabourAndBackOfficeActions.MonthEnd 186, PolicyDocumentSlices
  157, .Timesheets 154, TriageQueue.ApplyFiling 147, WorkOrderPoRenderer.Header 146). Not
  gated but visible: unprefixedBooleans 1,528 → 1,544, the new components' `Busy`/`IsOpen`
  parameters and the compose object's `IsReply`/`IsForward` mixed — a naming pass is its own
  round.

## The journey so far

| Figure | 22 Aug (v1) | R12 (v13) | R13 (v14) | R14 (v15) | R15 (v16) | R16 (v17) | R17 (v18) | R18 (v19) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 929 | 837 | 785 | 736 | 517 | 475 | **475** † |
| Average page length | 544 | 266 | 262 | 246 | 231 | 208 | 203 | **201** |
| Duplication | 4.16% | 2.83% | 2.85% | 2.85% | 2.87% | 2.87% | 2.78% | **2.70%** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,170 | 1,156 | **1,153** |
| Overlong function names | — | 44 | 44 | 43 | 43 | 43 | 43 | **43** |
| Functions over 30 lines | — | 703 | 702 | 700 | 698 | 697 | 694 | **692** |
| Files over 100 lines | 385 | 626 | 629 | 638 | 644 | 656 | 658 | 668 ‡ |

† The worst file is still the worker's `GraphMailClient.cs`, which the cloud cannot build; the
worst *buildable* file fell 453 → 428. ‡ The division signature: explicit-interface components
and partials where pages and `.cs` giants stood. Deep indentation moved for the first time —
2,798 → 2,656 — because the delivery tools' eight-deep lambdas became methods.

## Worst files by length

| File | Lines |
| --- | --- |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 428 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 415 |
| jpms/Pages/ProjectVariations.razor | 411 |
| jpms/Pages/TriageQueue.razor | 409 |
| api/Features/Ai/Tools/AiRegisterTools.cs | 405 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 |
| jpms/Services/HttpLabourStore.cs | 400 |
| jpms/Components/ValuationReportTable.razor | 395 |
| jpms/Pages/XeroAllocation.razor | 389 |
| api/Data/JpmsContext.Model.cs | 386 |
| api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs | 381 |
| api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs | 378 |

## Round 19, named

The worst buildable files are a pair at 428: **AiFiledDocuments** (the AI source reader over
filed documents — six of the codebase's fifty listed long-chain lines and nine deep ones; the
delivery-tools recipe of a descriptor, a handler and row projections applies, and its two
"Listing"/"Opening" sections name the partials) and **ManualWorkOrderModal.razor.cs** (a
component code-behind: partial-at-a-seam — the packaging step, the attachments, the PO email
orchestration — with the form it hosts already its own component). **SetXeroAllocationHandler
(422)** is the allocation command's one handler — a per-action division (allocate, bucket, set
project, split, undo) with a characterisation test first, as the compose handler had; it writes
to Xero. **AiRegisterTools (405)** and **ValuationReportSnapshotRenderer.Sections (402)** take
the two recipes proved this round (tool descriptors and handlers; renderer partials verified
by masked-PDF comparison). On the jpms side **ProjectVariations (411)** and **TriageQueue
(409, a third visit — the three reading panes' thirteen shared pass-through parameters want a
typed email-pane state record)** are the pages left over 400; **SidebarFolders (415)** is a
catalogue whose ten folders could each be a partial with their own history, and
**HttpLabourStore (400)** a store to divide by endpoint family. Deferred with a design each:
ValuationReportTable's rows (one editor state across rows) and DocumentControl's filing forms
(forms owning their drafts). **GraphMailClient (475)** stays the worker's giant until a round
runs on the Mac.

One note for the Mac: `ValuationSnapshotExportTests.AWorkingCopyAndAFrozenSnapshotAreStampedDifferently`
expects "Prepared 02 Sep 2026" where this Linux ICU's en-GB abbreviates September as "Sept";
it fails identically on main, and the production Functions host is Linux too.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
