using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Cqrs;

public sealed class HttpCommandSender : ICommandSender
{
    private readonly HttpClient httpClient;
    private readonly CommandRouteTable routes;

    public HttpCommandSender(HttpClient httpClient, CommandRouteTable routes)
    {
        this.httpClient = httpClient;
        this.routes = routes;
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var route = routes.For(command.GetType());
        var path = route.PathFor(command);
        var message = new HttpRequestMessage(new HttpMethod(route.HttpMethod), path) { Content = JsonContent.Create(command, command.GetType()) };
        var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface the endpoint's own message (e.g. a duplicate-reference rejection) so callers can
            // show it verbatim, rather than the opaque status-code text EnsureSuccessStatusCode throws.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = CleanErrorBody(body);
            throw new CommandFailedException(string.IsNullOrWhiteSpace(detail)
                ? $"The request failed ({(int)response.StatusCode})."
                : detail);
        }
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);
        if (result is null) throw new InvalidOperationException($"Command {command.GetType().Name} returned no body.");
        return result;
    }

    // Endpoints return the message either as a raw string or a JSON-quoted string; strip wrapping quotes
    // so the user sees clean text.
    private static string CleanErrorBody(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            trimmed = trimmed[1..^1].Replace("\\\"", "\"");
        return trimmed;
    }
}
