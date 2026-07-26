using System.Net;
using System.Text.Json;
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
            // The emptiness has to be decided on the BYTES, not the headers. Content-Length is
            // absent on a chunked response — which is how Static Web Apps' managed Functions
            // routinely answer — so a header-only check reads "unknown length" as "not empty",
            // falls through to the deserialiser and throws the very JsonException it was added to
            // prevent. Reading the body first costs one string per query and closes the hole.
            if (response.StatusCode == HttpStatusCode.NoContent) return default!;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json)) return default!;

            return JsonSerializer.Deserialize<TResult>(json, WebJson)!;
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

    /// <summary>The same options ReadFromJsonAsync applied by default (camelCase in, camelCase out),
    /// held once so switching to the explicit deserialiser changed nothing about how payloads are
    /// read. A shared instance is required — building JsonSerializerOptions per call is slow and
    /// defeats the serializer's own metadata cache.</summary>
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
}
