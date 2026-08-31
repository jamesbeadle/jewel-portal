using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpClientPortalStore : IClientPortalStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpClientPortalStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<ClientPortalRequest>> ListRequestsAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListMyClientRequests(), cancellationToken);

    public Task<ClientPortalRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetMyClientRequest(requestId), cancellationToken);

    public Task<IReadOnlyList<RequestMessage>> ListRequestMessagesAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListMyClientRequestMessages(requestId), cancellationToken);

    public Task<RequestMessage> PostRequestMessageAsync(
        string requestId, string body, string? parentMessageId, CancellationToken cancellationToken = default) =>
        // ClientId and the author are resolved server-side from the session.
        commands.SendAsync(new PostMyClientRequestMessage(requestId, body, parentMessageId), cancellationToken);

    public Task<IReadOnlyList<ClientPortalVariationOrder>> ListVariationOrdersAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListMyClientVariationOrders(), cancellationToken);

    public Task<ClientPortalVariationOrder?> GetVariationOrderAsync(string variationOrderId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetMyClientVariationOrder(variationOrderId), cancellationToken);

    public Task<IReadOnlyList<VariationOrderMessage>> ListVariationOrderMessagesAsync(string variationOrderId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListMyClientVariationOrderMessages(variationOrderId), cancellationToken);

    public Task<VariationOrderMessage> PostVariationOrderMessageAsync(
        string variationOrderId, string body, string? parentMessageId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new PostMyClientVariationOrderMessage(variationOrderId, body, parentMessageId), cancellationToken);
}
