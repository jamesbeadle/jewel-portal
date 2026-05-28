# 4. JPMS Services — how the Blazor layer changes

The recommendation: yes, the JPMS Service layer follows the API into CQRS. Not because it is fashionable, but because if the API exposes named commands and queries and the Service layer hides them again behind `IXxxStore`, the call sites in the Razor pages stay just as opaque as they are today. The whole point of the refactor is that a reader of `Pages/Projects.razor` should be able to see *which user story this button serves*. That requires the Service layer to speak the same language as the API.

## The shape that replaces `IXxxStore`

Today: one interface per entity, mixing reads and writes and an `OnChange` event:

```csharp
public interface IProjectStore
{
    IReadOnlyList<Project> All();
    Project? Find(string projectId);
    Project Upsert(Project project);
    event Action? OnChange;
}
```

After: three tiny interfaces. `IQuery<TResult>` and `ICommand<TResult>` come from `Jewel.JPMS.Contracts` (see `02-skeleton.md`); the three below are JPMS-only because they describe the transport, not the contract.

```csharp
public interface IQueryClient
{
    Task<TResult> AskAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}

public interface ICommandSender
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}

public interface IReadModelStore<TModel>
{
    TModel? Current { get; }
    event Action? OnChanged;
    Task RefreshAsync(IQuery<TModel> query, CancellationToken cancellationToken);
}
```

A Razor page consumes them like this:

```csharp
@inject IQueryClient Queries
@inject ICommandSender Commands

private IReadOnlyList<Project> projects = Array.Empty<Project>();

protected override async Task OnInitializedAsync()
{
    projects = await Queries.AskAsync(new ListProjectsVisibleToUser(), CancellationToken.None);
}

private async Task OnCreateProjectClicked()
{
    var newProject = await Commands.SendAsync(
        new CreateProjectShell(LeadId: selectedLead.LeadId, ProjectName: nameField),
        CancellationToken.None);
    projects = projects.Append(newProject).ToList();
}
```

The page now reads as a sentence: "ask the query client to list the projects visible to this user", "send the command to create a project shell from this lead." The name of the operation appears at the call site. The reader does not have to open `HttpProjectStore` to find out what `Upsert` means.

## Where the cache and `OnChange` go

Today's cache and `OnChange` event live inside each `Http*Store`. They are the wrong place for both. The cache belongs to a *view*, not to an *entity* — different views look at the same entity through different filters. `OnChange` belongs to a *query*, not to an *interface* — the page wants to know when the data it is showing has changed, not when anything anywhere has changed.

The new shape:

```csharp
public sealed class ProjectListReadModel : IReadModelStore<IReadOnlyList<Project>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<Project>? Current { get; private set; }
    public event Action? OnChanged;

    public ProjectListReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(IQuery<IReadOnlyList<Project>> query, CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(query, cancellationToken);
        OnChanged?.Invoke();
    }
}
```

One read-model store per view that wants live data. Cached, observable, and named after the view it serves. The Razor page subscribes to the read-model store, not to a global "something changed somewhere" stream.

## Wire format and transport

The `HttpQueryClient` and `HttpCommandSender` serialise the query or command record to JSON and POST it to a single dispatch endpoint, or — preferred for clarity — to the named REST route from the catalogue. The catalogue gives every command and query an explicit route, so the HTTP client can be table-driven from one place:

```csharp
public sealed class HttpCommandSender : ICommandSender
{
    private readonly HttpClient httpClient;
    private readonly CommandRouteTable routes;

    public async Task<TResult> SendAsync<TResult>(
        ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var route = routes.For(command.GetType());
        var response = await httpClient.PostAsJsonAsync(route.Path, command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken))!;
    }
}
```

`CommandRouteTable` is a single static dictionary populated next to the catalogue. The reader of the Service layer sees one transport class, one routing table — not fourteen `Http*Store` files each with their own GET-then-POST-then-refresh pattern.

## What happens to the existing `InMemory*` stores

They keep their value for two scenarios: the seed data path (`CommercialSeed.cs`, `LeadSeed.cs`, `CvrSeed.cs`) and offline-first preview. They become `InMemoryQueryClient` and `InMemoryCommandSender` — same two interfaces, served from in-memory dictionaries. The Razor page does not know it is being served from in-memory data instead of HTTP; it sees the same `IQueryClient` and the same `ICommandSender`. This is the only place where polymorphism appears in the design, and it appears because it is genuinely the same operation served two different ways.

## DI registration in JPMS

`jpms/Program.cs` shrinks dramatically. Today: fourteen `AddScoped<IXxxStore, HttpXxxStore>()` lines plus support services. After:

```csharp
builder.Services.AddScoped<IQueryClient, HttpQueryClient>();
builder.Services.AddScoped<ICommandSender, HttpCommandSender>();
builder.Services.AddSingleton<CommandRouteTable>();
builder.Services.AddSingleton<QueryRouteTable>();

builder.Services.AddScoped<ProjectListReadModel>();
builder.Services.AddScoped<LeadPipelineReadModel>();
builder.Services.AddScoped<BoqLinesReadModel>();
// ...one per live view that wants cached, observable data
```

Two transports, two routing tables, and one read-model store per live view. Down from ~50 service registrations to a list whose growth tracks the views, not the entities. The DI file becomes a literal index of "what live data does the app render?"

## What this does and does not solve

It does:

- Put the same vocabulary at the call site, the transport, and the API handler. The story flows through the codebase in one consistent name.
- Move the cache from "per entity" (wrong axis) to "per view" (right axis).
- Make it impossible for a Razor page to call `Upsert` when it actually means `IssueValuation` — the name does not exist.

It does not:

- Replace authentication on the wire. JPMS already runs inside the same Azure Static Web App as the API; the `X-MS-CLIENT-PRINCIPAL` header is forwarded automatically.
- Add optimistic concurrency or change the offline-sync model. Those are separate concerns and live on the slice plan as follow-ups.
- Introduce SignalR or any push transport for `OnChanged`. The read-model store still refreshes on demand; the event just fires when the local cache changes after a refresh.

The Service-layer shape and the API shape are now symmetric. Next file: the migration sequence that lands this without breaking the live app.
