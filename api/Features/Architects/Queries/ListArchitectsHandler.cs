using Jewel.JPMS.Contracts.Architects;

namespace Jewel.JPMS.Api.Features.Architects.Queries;

public sealed class ListArchitectsHandler : IQueryHandler<ListArchitects, IReadOnlyList<Architect>>
{
    private readonly JpmsContext context;
    public ListArchitectsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Architect>> HandleAsync(ListArchitects query, CancellationToken cancellationToken)
    {
        var architects = await context.Architects.AsNoTracking()
            .OrderBy(architect => architect.Name)
            .ToListAsync(cancellationToken);

        return architects.Select(architect => architect.ToModel()).ToList().AsReadOnly();
    }
}
