using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class RegisterDrawingHandler
    : ICommandHandler<RegisterDrawing, Drawing>
{
    private readonly JpmsContext context;

    public RegisterDrawingHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing> HandleAsync(RegisterDrawing command, CancellationToken cancellationToken)
    {
        var entity = new DrawingEntity
        {
            DrawingId = DrawingIdentifierFactory.NextDrawingId(),
            ProjectId = command.ProjectId,
            DrawingCode = command.DrawingCode,
            Title = command.Title,
            CurrentRevision = command.InitialRevisionLabel,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Drawings.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
