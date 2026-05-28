using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class GetDrawingByIdHandler : IQueryHandler<GetDrawingById, Drawing?>
{
    private readonly JpmsContext context;

    public GetDrawingByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing?> HandleAsync(GetDrawingById query, CancellationToken cancellationToken)
    {
        var entity = await context.Drawings
            .FirstOrDefaultAsync(drawing => drawing.DrawingId == query.DrawingId, cancellationToken);
        return entity?.ToModel();
    }
}
