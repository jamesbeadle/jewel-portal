# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle Raised → Issued → Paid). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.
- **Variation** is the canonical term for the priced change item, and it is **one document with one number through every stage** — its `VariationOrderStatus` (Quoting → Issued → Awaiting AI → Approved / Rejected) is what says where it has got to. Never present "VOQ" and "VO" as two records or two ladder steps: the 2026-07-23 `UnifyVariationOrders` migration folded them into one row, and the UI followed. The record lineage is **three** stages — Request → RFI → Variation — with bid packages branching off the variation. A user always reads the number as `V72` (`VariationOrder.DisplayNumber`, and the `VariationRef` minted at approval, which is the same number). "VOQ" survives only in persisted identifiers and API surface: the `VariationOrderQuotes` table and its `VariationOrderQuoteId` column, the stored `Reference` (`VOQ-0072`), the `JPMS/VOQ-…` mail tags, the `/api/…/voq(s)/…` routes, `RecordType.VariationQuote`, and command names like `CreateVoqFromRfq`. The page route is `/projects/{id}/variations/{id}`; the old `/voq/{id}` route is kept on the same page so links already sent out still land.

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
