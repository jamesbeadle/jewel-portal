using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Architects.Queries;

public sealed class ListArchitectsHandler : IQueryHandler<ListArchitects, IReadOnlyList<Architect>>
{
    private readonly JpmsContext context;
    public ListArchitectsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Architect>> HandleAsync(ListArchitects query, CancellationToken cancellationToken)
    {
        var architects = await context.Architects
            .OrderBy(architect => architect.Name)
            .ToListAsync(cancellationToken);

        return architects.Select(architect => architect.ToModel()).ToList().AsReadOnly();
    }
}
