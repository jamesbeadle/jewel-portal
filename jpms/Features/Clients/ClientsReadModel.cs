using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Clients;

public sealed class ClientsReadModel
{
    private readonly IQueryClient queries;
    private IReadOnlyList<Client> clients = Array.Empty<Client>();

    public ClientsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Client> Current => clients;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        clients = await queries.AskAsync(new ListClients(), cancellationToken);
        OnChanged?.Invoke();
    }
}
