using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Clients;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Clients.Queries;

public sealed class ListClientsHandler : IQueryHandler<ListClients, IReadOnlyList<Client>>
{
    private readonly JpmsContext context;
    public ListClientsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Client>> HandleAsync(ListClients query, CancellationToken cancellationToken)
    {
        var clients = await context.Clients.AsNoTracking()
            .OrderBy(client => client.Name)
            .ToListAsync(cancellationToken);

        return clients.Select(client => client.ToModel()).ToList().AsReadOnly();
    }
}
