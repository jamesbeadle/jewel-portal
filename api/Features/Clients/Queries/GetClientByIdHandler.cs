using Jewel.JPMS.Contracts.Clients;

namespace Jewel.JPMS.Api.Features.Clients.Queries;

public sealed class GetClientByIdHandler : IQueryHandler<GetClientById, Client?>
{
    private readonly JpmsContext context;
    public GetClientByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Client?> HandleAsync(GetClientById query, CancellationToken cancellationToken)
    {
        var entity = await context.Clients.FindAsync(new object[] { query.ClientId }, cancellationToken);
        return entity?.ToModel();
    }
}
