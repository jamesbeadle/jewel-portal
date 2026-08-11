# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle: raised & sent in one move — Submitted, awaiting approval — → Approved → Issued → Paid, with Raised surviving as a draft/recovery state; one click per material stage, driven from the claim card on the valuation page). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.
- **Variation** is the canonical term for the priced change item, and it is **one document with one number through every stage** — its `VariationOrderStatus` (Quoting → Issued → Awaiting AI → Approved / Rejected) is what says where it has got to. Never present "VOQ" and "VO" as two records or two ladder steps: the 2026-07-23 `UnifyVariationOrders` migration folded them into one row, and the UI followed. The record lineage is **three** stages — Request → RFI → Variation — with bid packages branching off the variation. A user always reads the number as `V72` (`VariationOrder.DisplayNumber`, and the `VariationRef` minted at approval, which is the same number). "VOQ" survives only in persisted identifiers and API surface: the `VariationOrderQuotes` table and its `VariationOrderQuoteId` column, the stored `Reference` (`VOQ-0072`), the `JPMS/VOQ-…` mail tags, the `/api/…/voq(s)/…` routes, `RecordType.VariationQuote`, and command names like `CreateVoqFromRfq`. The page route is `/projects/{id}/variations/{id}`; the old `/voq/{id}` route is kept on the same page so links already sent out still land.

## Record tabs & the in-view toolbar (jpms)

- **The request chain renders as document tabs, not chips.** `RecordTabBar` (Components) is on
  every page in the chain — Request → official stage (RFI/NOD/EOT) → Variation, bid packages after
  a divider. Only records that EXIST get a tab; the action that creates the next stage lives on the
  current stage's tab, never on a placeholder. On the request page the Request and official tabs
  are local panes (`LocalRequestTabs` + `OnSelect`); the variation and bid-package tabs navigate to
  those records' own pages, which render the same bar — moving along the chain reads as switching
  tabs. Deep-link the official pane with `?tab=official`.
- **Two dates, two meanings.** `Issued` is the official date the correspondent/client was notified
  — it is what lists lead with, and it is user-editable (requests) or stamped by the status
  transition (variations). `Created` is the system's own stamp (`Request.RaisedAt`,
  `VariationOrder.CreatedAt`) — shown only as a secondary fact on detail pages, and never as a
  list's lead date. Don't label `CreatedAt` "Raised".
- **In-view menu options are a `Toolbar` of icon buttons** (`ToolbarButton`, glyphs from
  `ActionIcon`, hover text mandatory), grouped by related functionality with `ToolbarDivider` —
  e.g. document actions (download PDF, email) | data actions (export, refresh). Underlined text
  links and one-off `btn-secondary`s are not the way to add a view action any more. The labelled
  `btn-primary` next to a toolbar stays reserved for the view's one primary act of creation
  ("Raise request"). `ExportToExcelButton` already renders as a toolbar button — keep passing
  `ShowIncludeAllRows`/`IncludeAllLabel` and it offers the current-view / include-all choice as a
  menu. Never wrap a toolbar in a LoadGate; pass `Disabled`/`Busy` to the buttons instead.

## Loading states (jpms)

- **Never render a figure, a row count or an empty state from a store that has not loaded.** A `0`
  that silently becomes `47` a second later is worse than no number: the reader has already believed
  it. Stores expose `IsLoaded`; read models expose a nullable `Current` (null = no fetch has landed).
- The pulsing jewel is the only loading mark. Three sizes, three places:
  - `LoadingScreen` — whole page, nothing to show yet.
  - `LoadGate` — a region or panel. `Prominent="true"` for a main panel (roughly a third of the
    screen or more), `Overlay="true"` to float over content that is being refreshed rather than
    replaced. `Panel` takes `IsLoading` and wraps its body in one.
  - `Stat` / `MetricStat` — `IsLoading` swaps the figure for the jewel and keeps the label.
- **A panel reveals itself in one piece.** If a panel reads several stores, gate it on all of them
  at once with `LoadState.UntilAll(a.IsLoaded, b.IsLoaded)` (or `UntilAllPresent(x.Current, …)`)
  rather than letting each half appear on its own.
- **Restraint: one jewel per screen, near enough.** A gate is for a REGION that will definitely
  render something and occupies real space. Three pulsing diamonds stacked down one page is worse
  than the zeros they replaced — the eye is drawn to the waiting rather than the work. In
  particular:
  - **Never gate a control.** A filter, a picker, a form field: render it `disabled` with a
    "Loading…" placeholder option instead. That says "not ready" in the control's own language,
    holds the layout still, and cannot be used to make a wrong choice.
  - **Never gate a single line of text.** A count or strapline simply does not render until it is
    known — a line of muted text arriving late is invisible, a spinner in its place is an event.
  - **Never gate a conditional panel.** If the panel only exists when there is something to show,
    its absence during the load is the same as its absence after it; a gate there announces a wait
    for something that usually never arrives.
- **`sessionReady` is not `dataReady`.** A page's auth flag says the session has been checked and
  the user is signed in — nothing more. It gates the RequestAccessView branch and the page chrome
  (title, intro, footnotes, tab nav), which need no data. Every data-bearing panel gates on its own
  sources. Naming the flag `isLoaded` and setting it before the awaits is what produced the
  zero-then-value flash this convention exists to prevent.
- **A failed fetch must open the gate.** Pair each gate with a `dataFailed` flag set in a
  `try/catch` around the awaited queries (and `|| X.LastRefreshFailed(id)` for read models that
  record failure rather than throwing), so the panel says what went wrong instead of pulsing
  forever. The error toast already carries the reference and the detail.
- **Backing fields are nullable, not `Array.Empty<T>()`.** An empty list is a real answer that sums
  to a real-looking zero; `null` is the only honest "not fetched yet". Expose a non-null accessor
  (`Rows => rows ?? Array.Empty<T>()`) for the computations and gate on the nullable field.
- Signals to gate on: `IsLoaded` / `LoadedFor(key)` / `XxxLoadedFor(key)` on stores and read models,
  `AsyncQueryCache.Has(key)` underneath most of them, or `Current is not null` on a read model.
  If the signal you need is missing, add it — do not gate on a proxy that happens to correlate.
- `wwwroot/index.html`'s boot screen mirrors `LoadingScreen.razor` exactly, so the handover from
  static HTML to Blazor is invisible.

## Error reporting (jpms)

- `ErrorReporter` holds the single current error; `ErrorToast` renders it full-width along the top
  of both layouts. One at a time, newest wins.
- Every report carries a short reference (`JPMS-7F3A2C`), the time, the signed-in user, the page,
  the endpoint + status, the server's own message and the stack — copyable in one click, so a user
  can forward something actionable rather than "it went red".
- What reaches the toast: all query failures, command failures **except** 400/409/422 (those are
  validation answers the calling dialog already shows next to the field), and any unhandled
  exception — caught either by `ReportingErrorBoundary` in `App.razor` or by
  `ErrorReportingLoggerProvider`, which watches the framework's own error logging because Blazor
  WASM has no usable `AppDomain.UnhandledException`.
- Blazor's `#blazor-error-ui` strip is last-resort only: it appears when the renderer itself has
  stopped and the only honest option left is Reload.

## Front-end data-loading convention (jpms, Blazor WASM)

- Stores that back synchronous render-time reads (e.g. `ForProject`, `LinesFor`, `PackagesFor`) fetch at most once per key to avoid render → fetch → render loops. Every project tab page must therefore call the store's `Refresh(projectId)` once from `OnInitializedAsync` (never from render) so navigating between tabs revalidates cached data in the background (stale-while-revalidate). Follow this pattern when adding new tabs or stores.
- The router (`App.razor`) uses `KeyedPageRouteView`, which keys each page by its type + route parameter values. Navigating between two URLs of the same route template (e.g. the project header's prev/next arrows) therefore recreates the page and re-runs `OnInitializedAsync`, so the convention above fires there too — pages never need `OnParametersSetAsync` guards for route-value changes.

## Project ordering (jpms)

- **Every list of projects is in one order: live work first.** `ProjectOrdering.InWorkOrder()`
  (contracts/Models) sorts by a coarse four-band rank — Pre-Construction/Procurement/Mobilisation/
  Live Delivery/Close-Out (0) → Defects Period (1) → Lead (2) → Completed (3) — then A–Z by name,
  then by reference. The bands are deliberately coarse so a project moving from Procurement to
  Mobilisation does not jump the list mid-build.
- It is applied **once**, in `ListProjectsVisibleToUserHandler`, so everything reading
  `ProjectListReadModel` inherits it. Callers that narrow the list (`.Where(Stage != Completed)`)
  re-apply `.InWorkOrder()` after the filter; nothing sorts projects by its own rule. If a list
  needs a different order, it needs a reason written next to it.
- **Completed projects are ordered last, not hidden** — except from the side-nav switcher, the
  header's prev/next cycle and the finance overview, which are about work in progress. The
  switcher carries a per-user "Show completed" toggle (`ProjectStageFilter`, decision 2026-08-03)
  that adds completed projects back into the picker, the prev/next cycle and project-scoped
  navigation (`CurrentProjectService.ResolveFor(projects, includeCompleted)`) so their records —
  the valuation report above all — stay reachable after handover; the finance overview ignores
  the toggle. Anywhere costs or history are recoded (Xero allocation, audit trail) the full list
  stays available.
- `SearchSelect` already leads its unfiltered list with a blank entry labelled with its
  `Placeholder`, which *is* the clear/"All …" row. Do not prepend another one — that is what put
  "All projects" in the Xero allocation filter twice.

## Database migrations (prod)

- **Every schema change ships with its apply commands, immediately.** When work adds an EF
  migration, hand the user the exact ready-to-run commands in the same reply as the code — never
  leave the database update as a follow-up. The database is updated *before or with* the deploy
  (additive/expand first), because people are using the system and the deployed code must never
  query columns that don't exist yet. That is exactly what broke sign-in on 2026-07-30.
- **Scoped scripts only — the full idempotent script is permanently broken against prod.**
  `20260702170000_SeparateArchitectsFromClients` embeds raw SQL reading `Clients.ArchitectEmail`,
  which a later step drops; SQL Server fails that batch at *compile time* on any database where
  the column is already gone, so `dotnet ef migrations script --idempotent` (unscoped) can never
  run again. Always generate from the last applied migration:
  1. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"`
  2. `cd api && dotnet ef migrations script <that-id> --idempotent -o migrate.sql`
  3. `sqlcmd -S sql-jpms-prod-54cf9e.database.windows.net -d jpms -U jpmsadmin -i migrate.sql -b -o migrate.log`
  4. Read `migrate.log` — `-b` stops at the first error, and "completed" printed by an *earlier*
     script run proves nothing about the current one.
- **Raw SQL inside migrations must survive the column being dropped later.** Wrap data-moving SQL
  in `EXEC sp_executesql N'...'` so it compiles only when the guard actually runs; inline raw SQL
  referencing columns that a later migration drops is what poisoned the full script.
- One-off data fixes (seeds, role grants, remaps) stay as reviewed scripts under `infra/` /
  `scripts/` run via sqlcmd — they are not EF migrations and must never touch schema.
