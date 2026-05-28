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
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: cancellationToken);
        if (result is null) throw new InvalidOperationException($"Command {command.GetType().Name} returned no body.");
        return result;
    }
}
