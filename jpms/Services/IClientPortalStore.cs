using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// The client portal's reads and writes. Everything is scoped server-side to the signed-in
/// client's own projects (Gates/ClientScope) — there is nothing to key by here, and nothing a
/// caller could pass to see another client's records.
/// </summary>
public interface IClientPortalStore
{
    Task<IReadOnlyList<ClientPortalRequest>> ListRequestsAsync(CancellationToken cancellationToken = default);
    Task<ClientPortalRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestMessage>> ListRequestMessagesAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Adds to the request's shared thread; a parent id makes it a reply.</summary>
    Task<RequestMessage> PostRequestMessageAsync(string requestId, string body, string? parentMessageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientPortalVariationOrder>> ListVariationOrdersAsync(CancellationToken cancellationToken = default);
    Task<ClientPortalVariationOrder?> GetVariationOrderAsync(string variationOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrderMessage>> ListVariationOrderMessagesAsync(string variationOrderId, CancellationToken cancellationToken = default);

    /// <summary>Adds to the order's shared thread; a parent id makes it a reply.</summary>
    Task<VariationOrderMessage> PostVariationOrderMessageAsync(string variationOrderId, string body, string? parentMessageId, CancellationToken cancellationToken = default);
}
