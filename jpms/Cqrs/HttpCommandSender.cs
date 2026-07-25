using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpCommandSender : ICommandSender
{
    private readonly HttpClient httpClient;
    private readonly CommandRouteTable routes;
    private readonly IErrorSink errors;

    public HttpCommandSender(HttpClient httpClient, CommandRouteTable routes, IErrorSink errors)
    {
        this.httpClient = httpClient;
        this.routes = routes;
        this.errors = errors;
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
                ? $"The request failed ({status})."
                : detail);
        }

        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);
        if (result is null) throw new InvalidOperationException($"Command {command.GetType().Name} returned no body.");
        return result;
    }

    /// <summary>
    /// Validation rejections (400 and 409) are the endpoint answering the user's question — "that
    /// reference is already taken" — and the dialog that sent the command already shows that text
    /// next to the field it belongs to. Raising a full-width toast on top would be shouting an
    /// answer the user has already read. Everything else is a failure they cannot act on where they
    /// are standing, and that is what the toast is for.
    /// </summary>
    private static bool DeservesAToast(int statusCode) => statusCode is not (400 or 409 or 422);

    // Endpoints return the message either as a raw string or a JSON-quoted string; strip wrapping quotes
    // so the user sees clean text.
    private static string CleanErrorBody(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Replace("\\\"", "\"");
        return trimmed;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
