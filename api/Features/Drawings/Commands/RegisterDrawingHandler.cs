using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class RegisterDrawingHandler
    : ICommandHandler<RegisterDrawing, Drawing>
{
    private readonly JpmsContext context;

    public RegisterDrawingHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing> HandleAsync(RegisterDrawing command, CancellationToken cancellationToken)
    {
        if (command.DrawingFolderId is not null)
        {
            var folder = await context.DrawingFolders.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.DrawingFolderId == command.DrawingFolderId, cancellationToken);
            if (folder is null || folder.ProjectId != command.ProjectId)
                throw new InvalidOperationException("The folder does not exist on this project.");
        }

        var entity = new DrawingEntity
        {
            DrawingId = DrawingIdentifierFactory.NextDrawingId(),
            ProjectId = command.ProjectId,
            DrawingCode = command.DrawingCode,
            Title = command.Title,
            // A new drawing has no approved revision yet; the label is set on first approval.
            CurrentApprovedRevisionLabel = null,
            CreatedAt = DateTimeOffset.UtcNow,
            DrawingFolderId = command.DrawingFolderId
        };
        context.Drawings.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
