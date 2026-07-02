using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Architects.Queries;

public sealed class GetArchitectByIdHandler : IQueryHandler<GetArchitectById, Architect?>
{
    private readonly JpmsContext context;
    public GetArchitectByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Architect?> HandleAsync(GetArchitectById query, CancellationToken cancellationToken)
    {
        var entity = await context.Architects.FindAsync(new object[] { query.ArchitectId }, cancellationToken);
        return entity?.ToModel();
    }
}
