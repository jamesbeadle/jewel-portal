using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// The drawing-folder command handlers, kept in one file because the whole feature is four small
// writes over one table (create, rename, delete, move-drawing) — the same reasoning as the
// contracts living together in DrawingFolders.cs. Folders nest via ParentDrawingFolderId.

public sealed class CreateDrawingFolderHandler
    : ICommandHandler<CreateDrawingFolder, DrawingFolder>
{
    private readonly JpmsContext context;

    public CreateDrawingFolderHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingFolder> HandleAsync(CreateDrawingFolder command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        var parentId = string.IsNullOrWhiteSpace(command.ParentDrawingFolderId) ? null : command.ParentDrawingFolderId;

        if (parentId is not null)
        {
            var parent = await context.DrawingFolders.AsNoTracking()
                .FirstOrDefaultAsync(folder => folder.DrawingFolderId == parentId, cancellationToken);
            if (parent is null || parent.ProjectId != command.ProjectId)
                throw new InvalidOperationException("The parent folder does not exist on this project.");
        }

        // Same name at the same level returns the existing folder — the inline "New folder…"
        // path must not split one discipline across two folders because of a retype.
        var existing = await DrawingFolderSiblings.FindByNameAsync(context, command.ProjectId, parentId, name, cancellationToken);
        if (existing is not null) return existing.ToModel();

        var entity = new DrawingFolderEntity
        {
            DrawingFolderId = DrawingIdentifierFactory.NextDrawingFolderId(),
            ProjectId = command.ProjectId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            ParentDrawingFolderId = parentId
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
        var duplicate = await DrawingFolderSiblings.FindByNameAsync(
            context, entity.ProjectId, entity.ParentDrawingFolderId, name, cancellationToken);
        if (duplicate is not null && duplicate.DrawingFolderId != entity.DrawingFolderId)
            throw new InvalidOperationException($"There is already a folder named “{name}” at this level.");

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

        // Nothing inside is lost: drawings and sub-folders move up one level into the deleted
        // folder's parent (null = Ungrouped / top level). The bulk updates commit on their own,
        // so the three writes share a transaction — a failed delete must not leave the contents
        // re-parented under a folder that still exists.
        // The context retries on transient SQL failures, and a user transaction must run inside
        // that strategy (as DeleteProjectHandler does) or EF refuses it.
        var parentId = entity.ParentDrawingFolderId;
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.Drawings
                .Where(drawing => drawing.DrawingFolderId == command.DrawingFolderId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(drawing => drawing.DrawingFolderId, parentId),
                    cancellationToken);
            await context.DrawingFolders
                .Where(folder => folder.ParentDrawingFolderId == command.DrawingFolderId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(folder => folder.ParentDrawingFolderId, parentId),
                    cancellationToken);

            context.DrawingFolders.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
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
