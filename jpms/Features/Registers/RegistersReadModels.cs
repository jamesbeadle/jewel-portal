using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Registers;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Registers;

/// <summary>The company registers (scope §8). Single-key caches, nullable until first load.</summary>
public sealed class RegisterItemsReadModel
{
    private readonly IQueryClient queries;
    public RegisterItemsReadModel(IQueryClient queries) { this.queries = queries; }
    public event Action? OnChanged;
    public IReadOnlyList<RegisterItem>? Current { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListRegisterItems(), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class PolicyDocumentsReadModel
{
    private readonly IQueryClient queries;
    public PolicyDocumentsReadModel(IQueryClient queries) { this.queries = queries; }
    public event Action? OnChanged;
    public IReadOnlyList<PolicyDocument>? Current { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListPolicyDocuments(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The signed-in user's own acknowledgements — outstanding first.</summary>
public sealed class MyPolicySignOffsReadModel
{
    private readonly IQueryClient queries;
    public MyPolicySignOffsReadModel(IQueryClient queries) { this.queries = queries; }
    public event Action? OnChanged;
    public IReadOnlyList<PolicySignOff>? Current { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListMyPolicySignOffs(), cancellationToken);
        OnChanged?.Invoke();
    }
}

public static class RegistersRouteRegistration
{
    public static IServiceCollection AddRegistersReadModels(this IServiceCollection services)
    {
        services.AddScoped<RegisterItemsReadModel>();
        services.AddScoped<PolicyDocumentsReadModel>();
        services.AddScoped<MyPolicySignOffsReadModel>();
        return services;
    }

    public static void RegisterRegistersRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListRegisterItems, IReadOnlyList<RegisterItem>>(
            QueryRoute.Static("/api/registers/items"));
        queries.Register<ListPolicyDocuments, IReadOnlyList<PolicyDocument>>(
            QueryRoute.Static("/api/registers/policies"));
        queries.Register<ListPolicySignOffs, IReadOnlyList<PolicySignOff>>(
            new QueryRoute("/api/registers/policies/{policyDocumentId}/sign-offs",
                query => $"/api/registers/policies/{((ListPolicySignOffs)query).PolicyDocumentId}/sign-offs"));
        queries.Register<ListMyPolicySignOffs, IReadOnlyList<PolicySignOff>>(
            QueryRoute.Static("/api/my/policy-sign-offs"));

        commands.Register<SaveRegisterItem, RegisterItem>(CommandRoute.Post("/api/registers/items"));
        commands.Register<DeactivateRegisterItem, Acknowledgement>(CommandRoute.Post("/api/registers/items/deactivate"));
        commands.Register<PublishPolicyDocument, PolicyDocument>(CommandRoute.Post("/api/registers/policies"));
        commands.Register<SignPolicy, PolicySignOff>(CommandRoute.Post("/api/my/policy-sign-offs/sign"));
    }
}
