# Refactor audit — baseline v18, after round 17

Generated 2026-09-02 from `refactor/round-17`, replacing the round-16 (v17) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 658, totalFiles: 3458, worstFileLines: 475 |
| functionShape | limit: 30, functionsOverLimit: 694, elseBlocks: 1156, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 485, duplicatedLines: 6042, totalLines: 217198, duplicatedPercentage: 2.78 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1528 |
| comments | explanatoryCommentLines: 13672, filesWithComments: 1894, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2384, deeplyIndentedLines: 2798, overlongLines: 1655, measurementIsHeuristic: True |
| inventory | pages: 92, components: 133, orphanComponents: 6, averagePageLines: 203 |

## Round 17 — the valuation editors, three code-behinds, two api giants, four pane pages

Round 17 started from main as merged (PR #20 and the labour-overview hotfix, PR #21), whose own
commits had moved two held figures past v17 — comments 13,673 → 13,695 and member chains
2,395 → 2,396 — so the round had to pay those back before it could gain. Nine commits: every
buildable target the v17 report named, worst file first.

- **ValuationReportTable 517 → 395**: the three inline percent editors (line, roll-up, VO
  revise) — each an input, a tick, a cross and a focus-on-first-render — are one
  **`InlineValueEditor`** (Components) with `Value`/`ValueChanged`, `Error`, `SelectOnFocus`,
  `PreventBlur` and typed key/blur/save/cancel callbacks; the focus plumbing (`focusPending`,
  `ElementReference`, `OnAfterRenderAsync`) left three partials. `BulkPercentToolbar`,
  `ValuationSectionHeader` and `ValuationSummaryPanel` stand where their markup did;
  `ValuationReportDisplay` (Features/Commercial) carries `Gb`, `Num`, `Pct`, `SignedPct` and
  `PercentText`. Judged and deferred: the section's line and roll-up rows (the row-family
  recipe) — Enter-to-advance, save-on-blur and the roll-up reveal all read one editor state
  across rows, so a row component there is a state-ownership design, not an extraction.
- **CostCentreReconciliationRenderer 509 → 72** + Document (the records), Header, Lines,
  Summary and Helpers partials. The three lines tables share `AddLinesTable(section, (width,
  heading)…)`; `AddSummaryRow`, `AddHeaderTitle`/`AddHeaderStamp` and `NewDocument`/
  `AddA4Section`/`ToPdfBytes` put every function under thirty lines. Five private helpers were
  byte-for-byte the ones `JewelDocumentStyle` exports — this renderer and
  `ValuationReportSnapshotRenderer` both carried them, both now use the shared ones. Verified
  beyond the build: a sample document and an empty-centre variant rendered by the old and new
  code give byte-identical PDFs once timestamps, ids and font-subset tags are masked.
- **WorkOrderForm.razor.cs 507 → 112** + LineRow, Draft, Assistant and Fields partials.
  `LineRow.From(WorkOrderLine)` names the "1 item" placeholder test once; `ApplyAssistant`
  (69 lines) is `ApplyOrderProposal` + `ApplyLinesProposal` + `ApplyLineProposal`;
  `ValidationProblem`'s per-line rules are `MeasuredBreakdownProblem` and `PaidLineProblem`.
- **RequestsActions 492 → 42**: `Build()` concatenating Requests, RequestEmails,
  DocumentControl, Filing and Triage partials, as the sibling action sources already do; the
  Skipped register stays in the core file.
- **XeroAllocation 488 → 389** (second visit): `AllocationPageHeader`, the three selection bars
  as `LabourBulkActions`, `QueueBulkActions` (picks bound back to the page — the bulk commands
  read them) and `AllocatedBulkActions`, `AllocationTableHeader` (a switch over (title, width)
  pairs where the else-if ladder was; select-all a typed bool) and `BucketChipStrip` owning its
  chip class and totals.
- **ValuationInvoicesSection.razor.cs 476 → 99** + Menu, Forms, Commands and Export partials;
  `ValuationInvoiceDisplay` (badge, hover text, event names). `InvoiceMenuItems` (66 lines, an
  else-if ladder) is `LifecycleItems` (a switch over the status), `AmendItems` and
  `RecordItems` over one `Item(label, action, hint, group, destructive)` helper; the paid-amount
  rule both forms spelled out is `TryParsePaid` once.
- **ProjectLabour 472 → 298**: `TimesheetRow` (edit fields bound two-way to the page's edit
  state), `TimesheetApprovalFooter`, `ApprovalFailuresBanner`, `SiteRegisterPanel`,
  `SettlementSummaryTable`, `CoverInvoiceLinesTable` under Features/Labour.
- **TriageQueue 470 → 409**: `InboxPaneHeader` (strapline, tabs, load error; `QueueView` is a
  public enum in Features/Triage/Queue now); the email pane's three "nothing selected" branches
  are one, with the prompt chosen by view.
- **DocumentControl 455 → 345**: `DocumentListItem`, `DocumentOutcomeCard`, `DocumentPreview`,
  `SourceEmailCard` and `DocumentControlDisplay` under Features/DocumentControl. The filing
  form (three destinations over the page's draft state, and a 351-line Filing partial) stays:
  moving it means the forms owning their drafts, a round of its own.
- **Held and improved**: comments 13,695 → 13,672 (below v17's 13,673: section headers the
  filenames carry, headers the component names carry, a duplicated fold header), `else` blocks
  1,170 → 1,156, functions over 30 lines 697 → 694 (the five `XActions() => new AiAction[]`
  arrays the length heuristic reads as functions cost four, paid back by real splits), member
  chains 2,396 → 2,384, deep indentation 2,800 → 2,798, duplication 2.87% → 2.78% (clones
  490 → 485: the identical helpers and using blocks went), overlong names 43, hex colours 43,
  orphans 6. **Division signature**: filesOverLimit 656 → 658 — forty-three new components,
  partials and modules, six of them over 100 (RequestsActions.Requests 181,
  WorkOrderForm.Assistant 163, CostCentreReconciliationRenderer.Helpers 128,
  ValuationInvoicesSection.Forms 126 and .Commands 115, WorkOrderForm.Draft 104) beside the
  ten files they were cut from. Not gated but visible: unprefixedBooleans 1,502 → 1,528 — the
  new components' `Busy`, `Selected`, `Open` parameters follow the parameter names the existing
  components use rather than the Is-prefix rule; a naming pass is its own round.

## The journey so far

| Figure | 22 Aug (v1) | R11 (v12) | R12 (v13) | R13 (v14) | R14 (v15) | R15 (v16) | R16 (v17) | R17 (v18) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 954 | 929 | 837 | 785 | 736 | 517 | **475** † |
| Average page length | 544 | 272 | 266 | 262 | 246 | 231 | 208 | **203** |
| Duplication | 4.16% | 2.82% | 2.83% | 2.85% | 2.85% | 2.87% | 2.87% | **2.78%** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | 1,170 | **1,156** |
| Overlong function names | — | 44 | 44 | 44 | 43 | 43 | 43 | **43** |
| Functions over 30 lines | — | 704 | 703 | 702 | 700 | 698 | 697 | **694** |
| Files over 100 lines | 385 | 621 | 626 | 629 | 638 | 644 | 656 | 658 ‡ |

† The worst file is now the worker's `GraphMailClient.cs`, which the cloud cannot build (the
worker needs `Microsoft.ApplicationInsights.WorkerService`, absent from the Mac's package cache
too); the worst *buildable* file fell 517 → 453. ‡ The division signature: explicit-interface
components and partials where pages and `.cs` giants stood. Duplication moved down for the
first time since round 10, and `else` blocks for the second round running.

## Worst files by length

| File | Lines |
| --- | --- |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 453 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.Sections.cs | 448 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Labour.cs | 445 |
| jpms/Pages/Todos.razor.cs | 444 |
| jpms/Pages/TriageQueue.Apply.cs | 441 |
| api/Features/Ai/Tools/AiDeliveryTools.cs | 440 |
| jpms/Pages/ProjectRequestDetail.razor | 434 |
| api/Features/Registers/RegistersSlices.cs | 433 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 428 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| api/Features/Xero/Ledger/SetXeroAllocationHandler.cs | 422 |
| jpms/Services/Navigation/SidebarFolders.cs | 415 |
| jpms/Pages/ProjectVariations.razor | 411 |
| jpms/Pages/TriageQueue.razor | 409 |

## Round 18, named

The worst buildable file is **SendMailboxEmailHandler (453)**, whose `HandleAsync` is one
340-line, ten-step pipeline (validate → attachments → body → filing → draft → send → tag →
audit) threading two dozen locals; the split is a `ComposeContext` record carrying that state
with a method per step — and, with no test over it and real email at the end, the round should
add the characterisation test first (a fake Graph client, the three shapes: reply, forward,
new). **WorkOrderPoRenderer.Sections (448)** wants the same `AddLinesTable`/row-helper division
the reconciliation renderer just had, verified the same way (masked-PDF comparison).
**LabourAndBackOfficeActions.Labour (445)** and **AiDeliveryTools (440)** divide per action /
per tool as their siblings did. On the jpms side, **Todos (444 code-behind)**,
**TriageQueue.Apply (441)** and **ManualWorkOrderModal (428 code-behind)** are `.cs` giants for
partial-at-a-seam; **ProjectRequestDetail (434)** and **ProjectVariations (411)** are the two
pages left over 400, pane-shaped. Deferred with a design each: ValuationReportTable's rows (one
editor state across rows), DocumentControl's filing forms (forms owning their drafts), and the
three Control Centre reading panes' shared thirteen pass-through parameters (a typed
email-pane state record). **GraphMailClient (475)** stays the worker's giant until a round runs
on the Mac, where it can be built.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
