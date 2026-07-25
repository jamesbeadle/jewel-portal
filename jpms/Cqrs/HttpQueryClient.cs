using System.Net;
using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpQueryClient : IQueryClient
{
    private readonly HttpClient httpClient;
    private readonly QueryRouteTable routes;
    private readonly IErrorSink errors;

    public HttpQueryClient(HttpClient httpClient, QueryRouteTable routes, IErrorSink errors)
    {
        this.httpClient = httpClient;
        this.routes = routes;
        this.errors = errors;
    }

    public async Task<TResult> AskAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var operation = query.GetType().Name;
        var route = routes.For(query.GetType());
        var path = route.PathFor(query);

        try
        {
            var result = await httpClient.GetFromJsonAsync<TResult>(path, cancellationToken);
            return result!;
        }
        catch (OperationCanceledException)
        {
            // Navigating away mid-fetch is not a fault.
            throw;
        }
        catch (HttpRequestException requestFailure)
        {
            // Every query failure is worth reporting: unlike a command, a failed read has no dialog
            // standing behind it to explain itself, and the page it feeds would otherwise just sit
            // there looking empty — the exact "zeroes that never fill in" this is meant to end.
            var status = requestFailure.StatusCode is HttpStatusCode code ? (int)code : (int?)null;
            errors.ReportRequestFailure(operation, "GET", path, status, null, requestFailure);
            throw;
        }
        catch (Exception failure)
        {
            errors.ReportRequestFailure(operation, "GET", path, null, null, failure);
            throw;
        }
    }
}
