using System.Text.Json;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpCommandSender : ICommandSender
{
    private readonly HttpClient httpClient;
    private readonly CommandRouteTable routes;
    private readonly IErrorSink errors;
    private readonly AppVersionService versions;

    public HttpCommandSender(HttpClient httpClient, CommandRouteTable routes, IErrorSink errors, AppVersionService versions)
    {
        this.httpClient = httpClient;
        this.routes = routes;
        this.errors = errors;
        this.versions = versions;
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var operation = command.GetType().Name;
        var route = routes.For(command.GetType());
        var path = route.PathFor(command);
        var message = new HttpRequestMessage(new HttpMethod(route.HttpMethod), path) { Content = JsonContent.Create(command, command.GetType()) };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (Exception transportFailure) when (transportFailure is not OperationCanceledException)
        {
            // The request never landed — nobody downstream can explain that better than we can here.
            errors.ReportRequestFailure(operation, route.HttpMethod, path, null, null, transportFailure);
            throw;
        }

        // The API stamps its build number on every response — see AppVersionService. Observed
        // before the status check so a failing command still counts as a sighting.
        versions.ObserveResponse(response);

        if (!response.IsSuccessStatusCode)
        {
            // Surface the endpoint's own message (e.g. a duplicate-reference rejection) so callers can
            // show it verbatim, rather than the opaque status-code text EnsureSuccessStatusCode throws.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = CleanErrorBody(body);
            var status = (int)response.StatusCode;

            if (DeservesAToast(status))
                errors.ReportRequestFailure(operation, route.HttpMethod, path, status, NullIfBlank(detail), null);

            throw new CommandFailedException(string.IsNullOrWhiteSpace(detail)
                ? DescribeBodilessFailure(status)
                : detail);
        }

        // A 200 whose body can't be read is still a failure the user can do nothing about where
        // they stand — report it like any other, or the only trace is a generic fallback banner.
        try
        {
            var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);
            if (result is null) throw new InvalidOperationException($"Command {command.GetType().Name} returned no body.");
            return result;
        }
        catch (Exception readFailure) when (readFailure is not OperationCanceledException)
        {
            errors.ReportRequestFailure(operation, route.HttpMethod, path, (int)response.StatusCode, null, readFailure);
            throw;
        }
    }

    /// <summary>
    /// Validation rejections (400 and 409) are the endpoint answering the user's question — "that
    /// reference is already taken" — and the dialog that sent the command already shows that text
    /// next to the field it belongs to. Raising a full-width toast on top would be shouting an
    /// answer the user has already read. Everything else is a failure they cannot act on where they
    /// are standing, and that is what the toast is for.
    /// </summary>
    private static bool DeservesAToast(int statusCode) => statusCode is not (400 or 409 or 422);

    // Endpoints return the message as a raw string, a JSON-quoted string, or — the validation
    // shape used across the labour and commercial slices — a JSON array of messages
    // (BadRequestObjectResult(new[] { "…" })). Unwrap all three so the user reads the sentence and
    // not its packaging; several messages join into one line rather than being silently truncated.
    private static string CleanErrorBody(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            try
            {
                var messages = JsonSerializer.Deserialize<string[]>(trimmed);
                if (messages is { Length: > 0 })
                    return string.Join(" ", messages.Where(message => !string.IsNullOrWhiteSpace(message)));
            }
            catch (JsonException)
            {
                // Not an array of strings (e.g. a ProblemDetails list) — fall through and show it raw.
            }
        }
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Replace("\\\"", "\"");
        return trimmed;
    }

    /// <summary>
    /// The gates return bare status results — <c>StatusCodeResult(403)</c>, <c>UnauthorizedResult</c>,
    /// <c>BadRequestResult</c> — with no body at all, so the caller has nothing to show but a number.
    /// "The request failed (403)." is what a dialog then either prints or, worse, papers over with a
    /// guess of its own; this says the actual thing that happened. The status stays in the text so
    /// the sentence is still diagnosable when it reaches a screenshot.
    /// </summary>
    private static string DescribeBodilessFailure(int status) => status switch
    {
        401 => "Your session has expired — sign in again and retry.",
        403 => "Your role doesn't have permission to do this. Ask an administrator if you think it should.",
        404 => "That endpoint wasn't found (404) — the page may be newer than the deployed API.",
        408 or 504 => "The server took too long to answer — try again in a moment.",
        >= 500 => $"The server hit an error ({status}) — the reference in the red bar at the top will identify it.",
        _ => $"The request was rejected ({status})."
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
