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
            using var response = await httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();

            // "Nothing recorded yet" is a legitimate answer to the nullable queries — a project with
            // no retention terms, a lead with no outcome — but it never arrives as JSON. ASP.NET
            // renders OkObjectResult(null) as 204 with an empty body, and deserialising nothing
            // throws. An empty body behind a success status means null, not a broken response.
            if (IsEmpty(response)) return default!;

            var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);
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

    /// <summary>A 204, or a 200 the endpoint sent with no bytes behind it. Either way there is
    /// nothing to deserialise. ContentLength is null on a chunked response, which is not empty.</summary>
    private static bool IsEmpty(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0;
}
