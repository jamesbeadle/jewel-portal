using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpQueryClient : IQueryClient
{
    private readonly HttpClient httpClient;
    private readonly QueryRouteTable routes;

    public HttpQueryClient(HttpClient httpClient, QueryRouteTable routes)
    {
        this.httpClient = httpClient;
        this.routes = routes;
    }

    public async Task<TResult> AskAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var route = routes.For(query.GetType());
        var path = route.PathFor(query);
        var result = await httpClient.GetFromJsonAsync<TResult>(path, cancellationToken);
        return result!;
    }
}
