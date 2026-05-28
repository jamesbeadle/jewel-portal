using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UpdateDrawingMetadataHandler
    : ICommandHandler<UpdateDrawingMetadata, Drawing>
{
    private readonly JpmsContext context;

    public UpdateDrawingMetadataHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing> HandleAsync(UpdateDrawingMetadata command, CancellationToken cancellationToken)
    {
        var entity = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        entity.DrawingCode = command.DrawingCode;
        entity.Title = command.Title;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
