# Jewel Bespoke Build — working notes

## Terminology

- **Programme** is the canonical term for the project's plan of work and the project tab that holds it (the programme itself, its claims documents, and its correspondence). Never call it "Schedule" (or US-spelled "Program") in UI copy, code identifiers, routes, or docs. "Scheduling"/"schedule" survive only in persisted backend identifiers (e.g. `RecordType.Scheduling`, the `JPMS/SCH-` mail tag, API routes), immutable EF migrations, and the distinct retention-release concept `RetentionSchedule`, which is not the programme.
- **Valuation invoice** is the canonical term for an amount of money Jewel has claimed for the client to pay (raised against the current valuation; lifecycle Raised → Issued → Paid). Never introduce "cash call", "payment application", "application for payment", or "client invoice" for this concept in UI copy, code identifiers, or docs. "Cash call" survives only in historical meeting notes and immutable EF migrations. See `docs/00-business-context/glossary.md`.

## Front-end data-loading convention (jpms, Blazor WASM)

- Stores that back synchronous render-time reads (e.g. `ForProject`, `LinesFor`, `PackagesFor`) fetch at most once per key to avoid render → fetch → render loops. Every project tab page must therefore call the store's `Refresh(projectId)` once from `OnInitializedAsync` (never from render) so navigating between tabs revalidates cached data in the background (stale-while-revalidate). Follow this pattern when adding new tabs or stores.
- The router (`App.razor`) uses `KeyedPageRouteView`, which keys each page by its type + route parameter values. Navigating between two URLs of the same route template (e.g. the project header's prev/next arrows) therefore recreates the page and re-runs `OnInitializedAsync`, so the convention above fires there too — pages never need `OnParametersSetAsync` guards for route-value changes.
