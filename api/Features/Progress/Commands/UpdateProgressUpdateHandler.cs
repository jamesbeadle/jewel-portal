using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class UpdateProgressUpdateHandler
    : ICommandHandler<UpdateProgressUpdate, ProgressUpdate>
{
    private readonly JpmsContext context;

    public UpdateProgressUpdateHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressUpdate> HandleAsync(UpdateProgressUpdate command, CancellationToken cancellationToken)
    {
        var update = await context.ProgressUpdates.FindAsync(new object[] { command.ProgressUpdateId }, cancellationToken);
        if (update is null) throw new InvalidOperationException($"Progress update {command.ProgressUpdateId} not found.");

        update.Title = command.Title.Trim();
        update.Description = command.Description.Trim();
        update.WorkDate = command.WorkDate;
        await context.SaveChangesAsync(cancellationToken);

        var photos = await context.ProgressPhotos
            .Where(row => row.ProgressUpdateId == command.ProgressUpdateId)
            .OrderBy(row => row.SortOrder)
            .ToListAsync(cancellationToken);
        return update.ToModel(photos.Select(photo => photo.ToModel()).ToList());
    }
}
