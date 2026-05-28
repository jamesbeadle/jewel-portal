# 1. Audit — what the backend looks like today

Walking the chain backwards as `CLAUDE.md` prescribes: from existing code up to user stories, surfacing where each layer no longer traces cleanly to the one above it.

## The shape of `api/Functions`

Fifteen Azure Function classes, sixty-nine entry points. Every class follows the same template: inject `JpmsContext`, declare a handful of `[Function]` methods, each method either `Get`/`List`/`Find` against EF Core or `Upsert`/`Add`/`Record` against it. Representative example, `ProjectsApi.Upsert`:

```csharp
[Function("UpsertProject")]
public async Task<IActionResult> Upsert(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "projects")] HttpRequest request)
{
    var incoming = await request.ReadFromJsonAsync<ProjectEntity>();
    if (incoming is null) return new BadRequestResult();
    var existing = await context.Projects.FindAsync(incoming.ProjectId);
    if (existing is null) context.Projects.Add(incoming);
    else context.Entry(existing).CurrentValues.SetValues(incoming);
    await context.SaveChangesAsync();
    return new OkObjectResult(incoming);
}
```

The same eight-line pattern is duplicated dozens of times across the fifteen files. That duplication is the most visible symptom; the four structural problems below are the cause.

## The four structural problems

**1. No gates.** Every endpoint is `AuthorizationLevel.Anonymous`. There is no authentication check (the Static Web Apps `X-MS-CLIENT-PRINCIPAL` header is never read), no authorisation check (the persona system in `docs/01-personas` is invisible to the API), and no validation step beyond "did the body deserialise". `must-have-v1.md` lists OAuth sign-in and an admin-managed user directory as Foundations; the API does not honour either.

**2. Operations are not named as intentions.** `Upsert` is not a user story. `RecordBoqSignOff` is closer — it reads as something a Director does — but `UpsertValuation` covers at least two stories (US-07-03 "draft a valuation" and US-07-06 "issue a valuation") and silently chooses neither. The Razor page on the other side has to recreate the intention from context, which is exactly the friction `CLAUDE.md` calls out: "if a line of code doesn't make sense as a sentence, something is wrong."

**3. Reads and writes share a handler shape.** Every list endpoint returns the entity directly, every upsert endpoint accepts the entity directly. There is no distinction between the read model the UI consumes and the command shape the UI submits. Adding a computed field to a view today means adding it to the entity, which means it leaks into every write path that uses the same entity as its DTO.

**4. The JPMS Service layer mirrors the same conflation.** Each `IXxxStore` interface bundles three concerns: synchronous reads served from an internal cache, fire-and-forget writes that POST and refresh, and an `OnChange` event the UI subscribes to. `HttpProjectStore` is the canonical example:

```csharp
public IReadOnlyList<Project> All() {
    if (!hasLoaded) _ = LoadAsync();
    return cachedProjects;
}

public Project Upsert(Project project) {
    _ = UpsertAsync(project);
    return project;
}
```

The reader cannot tell whether `Upsert(project)` succeeded — it never could. Errors are swallowed in `LoadAsync`'s `catch` block. This shape has to change in lockstep with the API; if the API exposes named commands and queries but the Service layer still bundles them under `IProjectStore`, the call sites stay just as opaque.

## What the docs say the backend should look like

`docs/site-map.md` is the contract. Section 1 lists every route, and every route is annotated with the user stories it serves (`US-00-01`, `US-07-23`, etc.). `06-backlog/must-have-v1.md` lists the eleven Phase-1 workflows from CRM to Portfolio Reporting. The stories are real; the routes are real; the entities are real.

What is missing is the line between them. The Azure Functions today implement enough of the entity CRUD to keep the Razor pages alive, but no Azure Function is named after a user story. There is no `BookSiteVisit` for US-00-04, no `RaiseRfi` for US-05-05, no `ApproveValuation` for US-07-06, no `ConfirmPracticalCompletion` for US-08-anything. Those names are exactly what the CQRS refactor introduces. Every existing endpoint either becomes one or more named commands and queries, or is identified as missing-by-omission against a user story it should be serving.

## What the refactor preserves

- The EF Core entities in `api/Data/Entities` (no schema change).
- The HTTP routes, where possible. Where a route currently covers multiple intentions (`POST /api/leads` covers both "capture a new lead" and "edit an existing lead"), the route splits into two named routes.
- The Razor pages. They keep calling the same operations under new names via the new Service-layer shape.
- The `InMemory*` stores. They remain useful for offline-first scenarios but get the same read/write split as the `Http*` ones.

The next file (`02-skeleton.md`) defines the hand-rolled CQRS shape that replaces the current `*Api.cs` template.
