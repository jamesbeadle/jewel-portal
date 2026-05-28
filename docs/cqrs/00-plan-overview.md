# CQRS Migration Plan — Overview

This is the plan-only deliverable agreed at the start of the refactor: no code is changed yet. The plan walks the existing backend through the five stages defined in `CLAUDE.md`, identifies where the code has drifted, and prescribes a hand-rolled CQRS skeleton that pulls every operation back into one-to-one alignment with a user story.

The plan is split into five short files so each one stays readable on its own:

1. `01-audit.md` — what exists today across `api/Functions` and `jpms/Services`, and the four structural problems the refactor fixes.
2. `02-skeleton.md` — the hand-rolled CQRS skeleton: interfaces, dispatcher, gate pipeline, DI registration, file layout.
3. `03-catalogue.md` — the complete Command and Query catalogue. Every existing Azure Function entry point renamed and traced to the user story it serves.
4. `04-services.md` — how `jpms/Services` changes. The Store-per-area abstraction is split along the read/write seam to match the API.
5. `05-slices-and-sequence.md` — the migration sequence. Eight vertical slices ordered so the app stays shippable at every step, plus a checklist to verify each slice against `CLAUDE.md`.

## The principle that holds the plan together

`CLAUDE.md` is explicit: **every command and every query is a named intention that reads as a sentence, and *is* a user story made executable**. The plan is therefore not "introduce a CQRS library and refactor handlers into it" — it is "rename every backend operation so that the operation, its inputs, its gates, and its return type read like the user story they serve."

That principle drives four decisions that this plan never re-litigates:

- **No library.** Plain C# interfaces (`ICommand`, `IQuery`, `ICommandHandler<,>`, `IQueryHandler<,>`) and one explicit dispatcher. MediatR would hide the gates inside a behaviour pipeline; the gates need to be visible at the entry point.
- **Gates are written into each entry point, not pushed into middleware.** Authentication, authorisation and validation are named, ordered, and read in sequence above the dispatch call. The reader sees the gates the request passes through.
- **One handler per command, one handler per query.** Files are short (under 100 lines, almost always under 30), named after the command or query they serve. No `Service` classes that bundle five operations behind one interface.
- **JPMS Services follow the same split.** The current `IXxxStore` interfaces conflate reads and writes behind one cache + `OnChange` event. They are split into a typed query client and a typed command sender, with the cache moved into a per-query result store. See `04-services.md`.

## What is in scope and what is not

In scope:

- The fifteen `*Api.cs` files in `api/Functions` (sixty-nine entry points, all currently `AuthorizationLevel.Anonymous` with no validation).
- The fourteen `IXxxStore` interfaces in `jpms/Services` and their `Http*` and `InMemory*` implementations.
- The DI registration in `api/Program.cs` and `jpms/Program.cs`.
- The naming, file layout, and the introduction of an authentication gate and a per-command validation gate — both of which are currently absent.

Out of scope for this plan:

- The Razor pages themselves. The UI keeps calling the same logical operations under their new command/query names; the Service-layer change is transparent to the pages.
- The EF Core entity model. The refactor preserves entity shapes; it only changes how operations are dispatched against them.
- The Bluebeam Studio Projects integration noted in `06-backlog/must-have-v1.md`. That work lives downstream of this refactor.

## Why now

The audit (`01-audit.md`) shows the backend has drifted from the rule in `CLAUDE.md` that the backend is "a translation layer between the data structure and the UI, not a design problem." Today every entry point is anonymous, every validation is implicit, and every `Store` interface mixes the reads a view needs with the writes a form issues. The Razor pages can no longer be read alongside the API to see which user story each one serves — the names do not line up. CQRS is the corrective that re-establishes that alignment.
