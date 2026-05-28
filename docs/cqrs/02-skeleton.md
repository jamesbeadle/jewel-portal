# 2. The hand-rolled CQRS skeleton

Plain C# interfaces. No library. No reflection-based dispatch. No hidden behaviour pipeline. The shape is small enough to fit on one screen and every part of it reads as prose.

## Project structure — where the interfaces live

`api/JpmsApi.csproj` and `jpms/Jewel.JPMS.csproj` are independent projects today; they share nothing. A CQRS refactor that names commands and queries in one place and references those names from both sides therefore needs a third project — the only structural addition this plan introduces:

```
JewelEnterprises.sln
  Jewel.JPMS.Contracts       (NEW — netstandard2.1 class library)
    Cqrs/IQuery.cs
    Cqrs/ICommand.cs
    Cqrs/Acknowledgement.cs
    Projects/CreateProjectShell.cs   (command record)
    Projects/ListProjectsVisibleToUser.cs  (query record)
    ...one folder per feature
  Jewel.JPMS.Api             (existing — references Contracts)
  Jewel.JPMS                 (existing — references Contracts)
```

The Contracts project holds three things and nothing else: the two CQRS marker interfaces, every command and query record (POCOs, no behaviour), and the shared result models (the existing `jpms/Models/*.cs` types move here so they stop being parallel-defined). The four handler interfaces and the gate classes stay API-side; the transport (`IQueryClient`, `ICommandSender`) stays JPMS-side. Only the shared vocabulary moves into Contracts.

## The four core interfaces

Two marker interfaces (in Contracts) and two handler interfaces (in `api/Cqrs/`).

```csharp
public interface IQuery<TResult> { }

public interface ICommand<TResult> { }

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
```

A command that does not return a value uses `ICommand<Acknowledgement>`, where `Acknowledgement` is the single shared record:

```csharp
public sealed record Acknowledgement(string EntityId);
```

That gives every command a useful return without forcing an artificial `Unit` type.

## Gates — what runs before the handler

Three gates apply, in this order, to every entry point that mutates state. Queries skip validation but still run the first two.

1. **Authentication.** Resolve the signed-in user from `X-MS-CLIENT-PRINCIPAL` (Azure Static Web Apps) into a `SignedInUser`. If absent, return `401 Unauthorized`.
2. **Authorisation.** Each handler declares which roles may invoke it. The check is a single line at the top of the endpoint, reading `if (!authorisation.Allows(user, command)) return Forbidden();`. Roles come from the existing persona model in `docs/01-personas`.
3. **Validation.** Each command has a paired `XxxValidation` class with one method, `Check(command)`, returning a `ValidationOutcome`:

```csharp
public sealed record ValidationOutcome(IReadOnlyList<string> Errors)
{
    public bool HasFailed => Errors.Count > 0;
    public static ValidationOutcome Passed { get; } = new(Array.Empty<string>());
    public static ValidationOutcome Failed(params string[] errors) => new(errors);
}
```

The validator reads as a list of rules: "the project must exist", "the line total must be positive", "the sign-off may only happen once."

These three gates are written **into the entry point body**, not into a middleware pipeline. A reader of `UpsertProjectEndpoint` sees the three checks in sequence before the dispatch, exactly as `CLAUDE.md` describes: "the gates the request passes through, and they read as exactly that."

## The entry point shape

Every Azure Function becomes a thin endpoint class. Two responsibilities only: bind the request, run the gates, dispatch. The reference shape:

```csharp
public sealed class UpsertProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpsertProjectAuthorisation authorisation;
    private readonly UpsertProjectValidation validation;
    private readonly ICommandHandler<UpsertProject, Project> handler;

    public UpsertProjectEndpoint(
        SignedInUserResolver users,
        UpsertProjectAuthorisation authorisation,
        UpsertProjectValidation validation,
        ICommandHandler<UpsertProject, Project> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpsertProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects")] HttpRequest request)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UpsertProject>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var project = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(project);
    }
}
```

The class is under 40 lines including the constructor. It does one thing — translate an HTTP request into a dispatched command — and the four gate calls read top-to-bottom like the user story they enforce: "the user must be signed in, the body must parse, the user must be allowed to do this, the command must be valid, then dispatch."

Note: `[HttpTrigger(AuthorizationLevel.Anonymous, ...)]` stays. Azure Functions treats this attribute as transport-level. The real authentication and authorisation live in the named gate classes above.

## The handler shape

Handlers contain the domain logic and the EF Core calls. Nothing else. They do not bind HTTP, they do not authorise, they do not validate. The reference shape:

```csharp
public sealed class UpsertProjectHandler : ICommandHandler<UpsertProject, Project>
{
    private readonly JpmsContext context;

    public UpsertProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(UpsertProject command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null)
        {
            entity = ProjectEntity.FromCommand(command);
            context.Projects.Add(entity);
        }
        else entity.ApplyFrom(command);

        await context.SaveChangesAsync(cancellationToken);
        return Project.FromEntity(entity);
    }
}
```

Two notable shapes from `CLAUDE.md`:

- `ProjectEntity.FromCommand(command)` and `entity.ApplyFrom(command)` are static factories on the entity type, not magic AutoMapper calls. The mapping is one line and visible.
- The handler returns the `Project` *model* (the shape the UI consumes), not the `ProjectEntity` *table row*. This is the read/write split: commands accept commands, handlers return models.

## File layout

```
api/
  Cqrs/
    IQuery.cs
    ICommand.cs
    IQueryHandler.cs
    ICommandHandler.cs
    SignedInUser.cs
    SignedInUserResolver.cs
    ValidationOutcome.cs
    Acknowledgement.cs
  Features/
    Projects/
      Commands/
        UpsertProject.cs                       (the record)
        UpsertProjectHandler.cs
        UpsertProjectAuthorisation.cs
        UpsertProjectValidation.cs
        UpsertProjectEndpoint.cs
      Queries/
        GetProjectsForUser.cs
        GetProjectsForUserHandler.cs
        GetProjectsForUserEndpoint.cs
        GetProject.cs
        GetProjectHandler.cs
        GetProjectEndpoint.cs
    Leads/
      Commands/...
      Queries/...
    Boq/
      ...
```

One folder per feature, mirroring `docs/site-map.md` workflow numbering. Inside each feature, `Commands/` and `Queries/` separate the two halves of CQRS at the directory level. Each file is named after the command or query it serves, and every file is under 100 lines — most under 30.

## Dependency injection

DI registration becomes mechanical and is the only place where the system has any "magic". One extension method per feature:

```csharp
public static IServiceCollection AddProjectsFeature(this IServiceCollection services)
{
    services.AddScoped<ICommandHandler<UpsertProject, Project>, UpsertProjectHandler>();
    services.AddScoped<UpsertProjectAuthorisation>();
    services.AddScoped<UpsertProjectValidation>();

    services.AddScoped<IQueryHandler<GetProjectsForUser, IReadOnlyList<Project>>, GetProjectsForUserHandler>();
    services.AddScoped<IQueryHandler<GetProject, Project?>, GetProjectHandler>();
    return services;
}
```

`Program.cs` becomes a short list of `services.AddProjectsFeature()`, `services.AddLeadsFeature()`, etc. — one line per feature. No assembly scanning, no reflection: the reader can see every handler in the file.

## What this skeleton deliberately does not include

- No mediator. The endpoint depends on the handler directly via `ICommandHandler<TCommand, TResult>`. DI resolves the right implementation. There is no `IMediator.Send(...)` indirection.
- No behaviour pipeline. The gates run in the endpoint, in order, visible.
- No reflection-based command bus, no source generators. If you can see the file, you can see what runs.
- No separate read database. CQRS here is a structural split, not a data-store split. EF Core remains the single store; the read/write distinction is in *naming and shape*, not in *infrastructure*.

This is the smallest skeleton that delivers the four properties `CLAUDE.md` demands: visible gates, named intentions, separated reads and writes, and prose-like flow. The next file (`03-catalogue.md`) populates this skeleton with every command and query the existing API and Service layer demand.
