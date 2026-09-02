# Refactor audit — baseline v17, after round 16

Generated 2026-09-02 from `refactor/round-16`, replacing the round-15 (v16) baseline report. The
audit carries the prose and functionNames checks introduced at v2.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 656, totalFiles: 3415, worstFileLines: 517 |
| functionShape | limit: 30, functionsOverLimit: 697, elseBlocks: 1170, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 43, maxWords: 5, maxLength: 40 |
| duplication | clones: 490, duplicatedLines: 6214, totalLines: 216883, duplicatedPercentage: 2.87 |
| naming | bannedAbbreviationHits: 468, unprefixedBooleans: 1502 |
| comments | explanatoryCommentLines: 13673, filesWithComments: 1874, taskMarkers: 48 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 49, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2395, deeplyIndentedLines: 2800, overlongLines: 1666, measurementIsHeuristic: True |
| inventory | pages: 92, components: 132, orphanComponents: 6, averagePageLines: 208 |

## Round 16 — the finance trio, the work orders, the directory, and the seams

Every target the v16 report named, in twelve commits: the three table-heavy finance pages by
the row-family recipe, the two pane-shaped pages by the panel recipe, and five `.cs` giants by
the partial-at-a-seam division. The worst file is a component for the first time, and no page
is over 500 lines.

- **ProfitSummary 736 → 195**: the table is `ProfitTable` with `ProfitTableRow` and
  `ProfitTotalsRow` (Features/Cvr), the `MarginLine`/`Memo` fragments in its `@code` are the
  `MarginLine` and `MemoLine` components, and the four panels — `RunningProfitPanel` (the basis
  switch bound back to the page, because every Xero panel reads it), `BudgetForecastBridge`,
  `TrajectoryPanel`, `CumulativePositionPanel` with a `CumulativeChartCard` per job — stand
  where their markup did. The cumulative panel owns its Refresh-from-Xero action and raises
  `OnSynced`; the page keeps the initial read. `ProfitRow`, the bridge, trajectory and
  cumulative records left the page as public types; `SignedMoney`, `Pct`, `Pc`, `DeltaK` and the
  chart colours joined `ProfitDisplay`; the selection's total is `ProfitRow.TotalOf`. The summary
  strip is `ProfitSummaryStrip` over the new **`FigureTile`** (Components) — the finance KPI
  tile whose label stays while its figure pulses — which the three finance strips had each
  hand-rolled per tile.
- **CashForecast 665 → 220**: `CashForecastTable` with `ForecastCategoryRow` (the `CategoryRow`
  fragment, now a component), `ForecastProjectLine` (the FD's two knobs on it),
  `OverheadsRow` and the directors' `ClosingBalanceRow`; `ForecastKpiStrip` over `FigureTile`;
  `CombinedStatementCard`; `ProjectCashTable` with `ProjectCashRow` and `ProjectCashTotalsRow`.
  `ForecastView`, `ForecastRows` and `CashRow` are public records in Features/Cashflow. Every
  state the rows read stays on the page, where the reloads and the storage live.
- **WeeklyCashflow 604 → 175**: `WeeklyCashflowGrid` — one `CashflowBandSection` per band
  (the `BandSection` fragment), net movement, `WeeklyClosingBalanceRow` — takes the page's state
  and handlers once and wires every band from them; `WeeklyKpiStrip` over `FigureTile` retired
  the `LoadingFigure` fragment; `CashflowItemModal` and `SupplierGroupsModal` own their fields,
  their save and their store injection — the ItemDialog and GroupsDialog partials moved whole
  into their code-behinds. `WeeklyCashflowBands` carries the band's reading rules.
- **ProjectWorkOrders 548 → 193** (its core partial 451 → 160): `DraftWorkOrdersPanel` owns the
  two-click decision, the commands and the purchase-order email an approval promises;
  `RejectedWorkOrdersList`, `CancelledWorkOrdersList`, `UnpricedWorkOrdersList`;
  `DeleteWorkOrderModal` owns the one decision that leaves no row behind; `WorkOrdersTable` with
  `WorkOrderGroupRow` and `WorkOrderLineRow`, the footer a `WorkOrderGroup` like any row. The
  Labels partial is `WorkOrderDisplay`, with `PurchaseOrderPath` where four places spelled the
  URL by hand; the core divided into Rows, Menus and Dialogs partials.
- **Subcontractors 517 → 158**: `ClientsDirectoryTable`, `ArchitectsDirectoryTable`,
  `StaffDirectoryTable`, `CompaniesDirectoryTable` with `CompanyDirectoryRow`; `XeroImportModal`
  and `ConsolidateRecordsModal` own their state; `DirectoryDisplay` carries the label, location,
  dash and category list. The Consolidation partial, which also carried the export, is now
  Selection and Export.
- **ExcelWorkbookWriter 589 → 46** + Package/Worksheet/Cells/Helpers partials and
  `ExcelStyleRegistry` as a type of its own. **AiCommercialTools 560 → 48**, **AiSourceTools
  502 → 58**, **AiSourceReader 518 → 95** — a partial per tool, per concern and per format,
  `Build()` concatenating the tool methods as `AiRecordTools` does; each partial names only the
  usings it needs (the identical using blocks had been clone pairs). **TriageQueue.Outbox
  520 → 69**: the file was named for its first fifty lines; the rest is Decisions, Todos, Views
  and ListReads. The worker's hand-picked compile list carries none of the split api files.
- **Held**: comments 13,726 → 13,673 (section headers the filenames carry, fragments whose
  component names say it, one dangling comment), `else` blocks 1,182 → 1,170 (the tile branches
  became `FigureTile`'s one), functions over 30 lines 698 → 697, member chains 2,398 → 2,395,
  deep indentation 2,801 → 2,800, overlong names 43, hex colours 43, orphan components 6.
  Duplication 2.87% → 2.87% (clones 488 → 490): the new pairs are pass-through `[Parameter]`
  blocks — `CashflowBandSection` ↔ `WeeklyCashflowGrid`, `WorkOrdersTable` ↔
  `WorkOrderGroupRow` — and the combined statement card's two supplier lines against the
  project Cashflow tab's statement it mirrors by design. **Division signature**: filesOverLimit
  644 → 656 — twelve more files over 100 where five pages and five `.cs` giants stood, the
  largest of them the cumulative panel (130), the combined statement card (115) and the two
  Weekly Cashflow modal code-behinds (139, 137).

## The journey so far

| Figure | 22 Aug (v1) | R10 (v11) | R11 (v12) | R12 (v13) | R13 (v14) | R14 (v15) | R15 (v16) | R16 (v17) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Worst file (lines) | 4,961 | 954 | 954 | 929 | 837 | 785 | 736 | **517** |
| Average page length | 544 | 272 | 272 | 266 | 262 | 246 | 231 | **208** |
| Duplication | 4.16% | 2.81% | 2.82% | 2.83% | 2.85% | 2.85% | 2.87% | **2.87%** |
| `else` blocks | 1,087 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | 1,182 | **1,170** |
| Overlong function names | — | 44 | 44 | 44 | 44 | 43 | 43 | **43** |
| Functions over 30 lines | — | 700 | 704 | 703 | 702 | 700 | 698 | **697** |
| Files over 100 lines | 385 | 618 | 621 | 626 | 629 | 638 | 644 | 656 † |

† The division signature: explicit-interface components where page markup stood. Five rounds of
the panel and row-family recipes have taken the twelve worst pages under 500; the worst file is
now a component, and the average page has fallen 64 lines in five rounds. `else` blocks moved
for the first time since round 10.

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Components/ValuationReportTable.razor | 517 |
| api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs | 509 |
| jpms/Components/WorkOrderForm.razor.cs | 507 |
| api/Features/Ai/Tools/Actions/RequestsActions.cs | 492 |
| jpms/Pages/XeroAllocation.razor | 488 |
| jpms/Components/ValuationInvoicesSection.razor.cs | 476 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/ProjectLabour.razor | 472 |
| jpms/Pages/TriageQueue.razor | 470 |
| jpms/Pages/DocumentControl.razor | 455 |
| api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs | 453 |
| api/Features/Procurement/Documents/WorkOrderPoRenderer.Sections.cs | 448 |
| api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Labour.cs | 445 |
| jpms/Pages/TriageQueue.Apply.cs | 445 |
| jpms/Pages/Todos.razor.cs | 444 |

## Round 17, named

The worst file is a component now: **ValuationReportTable (517, six partials already beside
it)** wants the row-family recipe — a row per line kind (contract works, PC sums, contingency,
variations), the roll-up rows, and the percent editor as a component — and
**ValuationInvoicesSection (302 + 476)** and **WorkOrderForm (205 + 507)** are the other two
components carrying a page's worth of code-behind: partial-at-a-seam for the `.cs`, panes for
the markup. The three remaining pages over 450 — **XeroAllocation (488, second visit)**,
**ProjectLabour (472)** and **DocumentControl (455, with a 351-line Filing partial)** — are
pane-shaped. On the api side, **CostCentreReconciliationRenderer (509)** and
**RequestsActions (492)** want the Sections/Helpers and per-action divisions the renderers and
catalogues have already had, and **GraphMailClient (475)** is the worker's one giant (the worker
cannot be built in the cloud without `Microsoft.ApplicationInsights.WorkerService`, which the
Mac's package cache does not hold — a split there must be verified on the Mac). Two DRY moves
are now visible and small: the project Cashflow tab's statement and `CombinedStatementCard`
render the same statement, and the twin closing-balance rows (monthly, weekly) share their
shape.

Full detail, including every offender list, is in `audit.json`; the gate ratchets against
`baseline.json`, which this report accompanies.
