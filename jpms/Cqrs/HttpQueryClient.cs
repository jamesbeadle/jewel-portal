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
            // throws. An empty body behind a success status means "none", not a broken response.
            //
            // Emptiness is decided on the BYTES, not the headers. Content-Length is absent on a
            // chunked response — which is how Static Web Apps' managed Functions routinely answer —
            // so a header-only check reads "unknown length" as "not empty", falls through to the
            // deserialiser and throws the very JsonException it was added to prevent.
            if (response.StatusCode == HttpStatusCode.NoContent) return Nothing<TResult>();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json)) return Nothing<TResult>();

            // A literal "null" body lands here too, so both roads to "no answer" meet in one place.
            var result = JsonSerializer.Deserialize<TResult>(json, WebJson);
            return result ?? Nothing<TResult>();
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

    /// <summary>The same options ReadFromJsonAsync applied by default, held once so moving to the
    /// explicit deserialiser changed nothing about how payloads are read.</summary>
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// What a query answers with when the server sent nothing.
    ///
    /// A single-record query may legitimately answer null — "this project has no retention terms".
    /// A LIST query must NEVER answer null, and that distinction is the whole point of this method.
    /// Pages dereference a list result directly (<c>unassigned.Count</c> in the triage queue), so a
    /// null list is a NullReferenceException at RENDER time — long after the load method's own
    /// try/catch could have helped, because nothing threw. The page's fallback never runs, the
    /// error boundary takes the whole screen, and the cause looks nothing like its origin. An empty
    /// body for a list means "none", and "none" is an empty list.
    ///
    /// The empty collection is built by deserialising "[]" rather than by reflection: the
    /// serialiser already has to instantiate this exact type on the normal path, so there is no
    /// extra trimming risk in a WASM publish. A result shaped like a dictionary can't be filled
    /// from "[]", so that falls back to null rather than throwing on the way out.
    /// </summary>
    private static TResult Nothing<TResult>()
    {
        var type = typeof(TResult);
        if (type == typeof(string) || !typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return default!;

        try { return JsonSerializer.Deserialize<TResult>("[]", WebJson)!; }
        catch { return default!; }
    }
}
