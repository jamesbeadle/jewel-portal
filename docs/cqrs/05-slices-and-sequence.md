# 5. Migration sequence — slice-by-slice

The refactor lands in eight slices ordered so the app stays shippable at every step. Each slice introduces or completes a vertical column of the catalogue, with the JPMS Service-layer change for the same feature riding alongside in the same pull request.

The ordering follows `docs/site-map.md` § 4 (lifecycle order from CRM through closeout) with one inversion: **Slice 0 is the skeleton itself**, because nothing else can land until the four interfaces and the gate classes exist.

## Slice 0 — Skeleton and gate plumbing

Lands the entire `02-skeleton.md` contract without converting any feature yet.

What ships:

- `Jewel.JPMS.Contracts` (new netstandard2.1 project) with `IQuery`, `ICommand`, `Acknowledgement`. Empty feature folders ready to hold command and query records.
- `api/Cqrs/` with `IQueryHandler`, `ICommandHandler`, `ValidationOutcome`.
- `api/Gates/` with `SignedInUser`, `SignedInUserResolver` (parses `X-MS-CLIENT-PRINCIPAL`), and a `RoleSet` value type that authorisation classes compose, not inherit from — each `XxxAuthorisation` class holds its own static `RoleSet` of allowed roles and asks it `Includes(user.Role)`.
- `jpms/Cqrs/` with `IQueryClient`, `ICommandSender`, `IReadModelStore<T>`, and the two HTTP transports.
- `CommandRouteTable` and `QueryRouteTable` (empty — populated per slice).
- DI extension `services.AddCqrs()`.

What does not ship: any feature handler. The old `*Api.cs` files keep working in parallel; the skeleton sits alongside them, used by nothing yet. This slice is deliberately boring — its only job is to make the next seven slices each be a simple, mechanical conversion.

## Slice 1 — Projects (the hub)

Why first: every other feature attaches to a project. Convert the hub before the leaves.

Catalogue rows: `ListProjectsVisibleToUser`, `GetProjectById`, `CreateProjectShell`, `UpdateProjectDetails`, `SetProjectContractTerms`.

What changes:

- New `api/Features/Projects/` folder; old `Functions/ProjectsApi.cs` deleted.
- `jpms/Services/IProjectStore.cs`, `HttpProjectStore.cs`, `InMemoryProjectStore.cs` deleted; replaced by `ProjectListReadModel` and `ProjectDetailReadModel`.
- Razor pages `Projects.razor`, `ProjectDetail.razor`, `Portfolio.razor` updated to use `IQueryClient` and `ICommandSender` directly.

The slice ships with the first authorisation rule the codebase has ever had: `CreateProjectShell` is restricted to roles `P03` (Project Manager) and `P01` (Director).

## Slice 2 — Leads and CRM

Catalogue rows: 19 commands and queries under workflow 00 (see `03-catalogue.md`).

Why now: per `docs/site-map.md` § 4 Slice 1, "lifecycle begins at CRM". CRM is the most opinionated workflow — five sub-tabs, two outcome paths, a project-shell handoff — and lands in one slice so the language is established before the heavy commercial workflows arrive.

The handoff: `MarkLeadAsWon` invokes `CreateProjectShell` internally. Cross-feature handler invocation is allowed but is named — the handler reads `var newProject = await projectShellCreator.CreateAsync(...)` rather than reaching into another feature's handler class.

## Slice 3 — BoQ and Rate Library

Catalogue rows: BoQ (8 rows) + Rates (4 rows).

Why now: matches `site-map.md` § 4 Slice 2. Procurement, valuations, variations all reference BoQ line items. Land BoQ before the things that quote against it.

Special note: `SignOffBoqForProject` is the second authorisation gate — Director-only. The `BoqSignOff` value is the first place the codebase enforces a one-time write (the validator rejects the command if a sign-off already exists for this project).

## Slice 4 — Drawings

Catalogue rows: 7 rows under workflow 01.

`IssueDrawingRevision` is the first command with a non-trivial side effect: it supersedes the prior revision atomically (one transaction, two row updates, returns the new revision with the prior `IsCurrent = false`). The validator enforces "you cannot issue a revision lower than the current one."

## Slice 5 — Procurement and Subcontractors

Catalogue rows: 14 rows under workflow 03.

The big rule lands here: `AwardBidPackage` runs a compliance gate inside its validator that checks the subcontractor's `ComplianceDocument` set is complete and unexpired. This is the first cross-feature validation; it pulls from the Subcontractors feature's read model rather than duplicating compliance logic.

## Slice 6 — H&S, Mobilisation, and the mobile site app

Catalogue rows: 12 rows under workflow 04 plus 5 rows under workflow 06 (the site app).

Why now: `must-have-v1.md` calls out the mobilisation checklist as a hard gate. This slice introduces the second one-time write (`OpenMobilisationGate`) and the first command that the **mobile PWA** sends from offline (`SyncOfflineSiteQueue`). The catalogue makes the offline-replay shape obvious: each queued offline action is just a command record, so the replay endpoint is one handler that loops through commands and dispatches each one.

## Slice 7 — Commercial, CVR, and Cashflow

Catalogue rows: 24 rows under workflow 07. The biggest single slice.

Why last among the core workflows: PVR, CVR, Prelims, EOTs, and Cashflow all reference outputs from the prior slices (BoQ, Procurement, Site, Changes). They cannot land usefully until those exist in the new shape.

The Director-only gates accumulate here: `IssueValuation`, `ApproveTimesheet`, `CaptureCvrSnapshot`, `GrantEot`. By the end of this slice the role model is fully exercised against the catalogue and every persona from `docs/01-personas` has at least one command they own.

## Slice 8 — Closeout

Catalogue rows: 13 rows under workflow 08.

The cleanest slice — closeout has the fewest cross-references. Lands last because by the time projects reach closeout in production, the whole pipeline above has to be in place anyway.

## After Slice 8 — the catch-all

What is left when the eight slices land:

- Reports (`/reports/valuations`, `/reports/timesheets`) — these are query-only and follow the pattern from Slice 7. No new structure.
- Portfolio routes (`/portfolio/*`) — pure queries, table-driven, also follow Slice 7's pattern.
- Portals (`/portal/architect`, `/portal/client`, `/portal/subcontractor`) — same commands and queries, different authorisation gates. These were already in the catalogue; the portals just use a subset.

There is no Slice 9. Once Slice 8 is in, the catalogue is the whole API.

## Per-slice verification checklist

Run this against the slice before merging. Every answer must be "yes" — anything else means the slice has drifted from `CLAUDE.md`.

1. Does every new file stay under 100 lines?
2. Does every command or query name read as the user story it serves, without needing the story ID to explain it?
3. Is every handler under 30 lines?
4. Does every endpoint show all four gates in sequence at the top: authenticate, parse, authorise, validate?
5. Are there any `else` blocks that could be early returns?
6. Did this slice introduce any `Service`, `Manager`, `Helper`, `Utils`, or `Common` class? (Should be zero.)
7. Are any inline literals — HTTP status codes, role names, magic strings — left un-named?
8. Does every command and query in this slice trace to a row in `03-catalogue.md`? If new ones appeared, did they get added to the catalogue?
9. Did the JPMS Razor pages get updated in the same pull request, so the call sites speak the new vocabulary?
10. Does the slice leave the build green and the existing app still bootable from `dotnet run` on both projects?

## What "done" means

The refactor is complete when:

- `api/Functions/` is empty (the directory can be removed).
- `jpms/Services/I*Store.cs` and the `Http*Store` / `InMemory*Store` files are gone, replaced by `IQueryClient`, `ICommandSender`, and per-view `*ReadModel` classes.
- `api/Program.cs` reads as a list of `services.AddXxxFeature()` lines, one per workflow.
- `jpms/Program.cs` reads as a list of read-model registrations, one per live view.
- Every row in `03-catalogue.md` has a corresponding handler file and a corresponding entry point file.
- A reader can open any Razor page, scroll to the click handler, and see the user story the click serves spelled out in the command name.

That is the test the whole plan exists to pass.
