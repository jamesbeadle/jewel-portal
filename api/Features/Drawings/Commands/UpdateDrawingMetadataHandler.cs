using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UpdateDrawingMetadataHandler
    : ICommandHandler<UpdateDrawingMetadata, Drawing>
{
    private readonly JpmsContext context;

    public UpdateDrawingMetadataHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing> HandleAsync(UpdateDrawingMetadata command, CancellationToken cancellationToken)
    {
        var entity = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Document {command.DrawingId} not found.");

        entity.DrawingCode = (command.DrawingCode ?? "").Trim();
        entity.Title = (command.Title ?? "").Trim();

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
