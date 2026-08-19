using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// The drawing-folder command handlers, kept in one file because the whole feature is four small
// writes over one table (create, rename, delete, move-drawing) — the same reasoning as the
// contracts living together in DrawingFolders.cs.

public sealed class CreateDrawingFolderHandler
    : ICommandHandler<CreateDrawingFolder, DrawingFolder>
{
    private readonly JpmsContext context;

    public CreateDrawingFolderHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingFolder> HandleAsync(CreateDrawingFolder command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();

        // Same name on the same project returns the existing folder — the inline "New folder…"
        // path must not split one discipline across two folders because of a retype.
        var existing = await context.DrawingFolders
            .FirstOrDefaultAsync(folder => folder.ProjectId == command.ProjectId
                && folder.Name.ToLower() == name.ToLower(), cancellationToken);
        if (existing is not null) return existing.ToModel();

        var entity = new DrawingFolderEntity
        {
            DrawingFolderId = DrawingIdentifierFactory.NextDrawingFolderId(),
            ProjectId = command.ProjectId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.DrawingFolders.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class RenameDrawingFolderHandler
    : ICommandHandler<RenameDrawingFolder, DrawingFolder>
{
    private readonly JpmsContext context;

    public RenameDrawingFolderHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingFolder> HandleAsync(RenameDrawingFolder command, CancellationToken cancellationToken)
    {
        var entity = await context.DrawingFolders.FindAsync(new object[] { command.DrawingFolderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Drawing folder {command.DrawingFolderId} not found.");

        var name = command.Name.Trim();
        var duplicate = await context.DrawingFolders.AsNoTracking()
            .AnyAsync(folder => folder.ProjectId == entity.ProjectId
                && folder.DrawingFolderId != entity.DrawingFolderId
                && folder.Name.ToLower() == name.ToLower(), cancellationToken);
        if (duplicate) throw new InvalidOperationException($"The project already has a folder named “{name}”.");

        entity.Name = name;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class DeleteDrawingFolderHandler
    : ICommandHandler<DeleteDrawingFolder, Acknowledgement>
{
    private readonly JpmsContext context;

    public DeleteDrawingFolderHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteDrawingFolder command, CancellationToken cancellationToken)
    {
        var entity = await context.DrawingFolders.FindAsync(new object[] { command.DrawingFolderId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Drawing folder {command.DrawingFolderId} not found.");

        // The drawings survive — they drop back to the register's Ungrouped section.
        await context.Drawings
            .Where(drawing => drawing.DrawingFolderId == command.DrawingFolderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(drawing => drawing.DrawingFolderId, (string?)null),
                cancellationToken);

        context.DrawingFolders.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.DrawingFolderId);
    }
}

public sealed class MoveDrawingToFolderHandler
    : ICommandHandler<MoveDrawingToFolder, Drawing>
{
    private readonly JpmsContext context;

    public MoveDrawingToFolderHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing> HandleAsync(MoveDrawingToFolder command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        if (command.DrawingFolderId is not null)
        {
            var folder = await context.DrawingFolders.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.DrawingFolderId == command.DrawingFolderId, cancellationToken);
            if (folder is null) throw new InvalidOperationException($"Drawing folder {command.DrawingFolderId} not found.");
            if (folder.ProjectId != drawing.ProjectId)
                throw new InvalidOperationException("The folder belongs to a different project.");
        }

        drawing.DrawingFolderId = command.DrawingFolderId;
        await context.SaveChangesAsync(cancellationToken);
        return drawing.ToModel();
    }
}
