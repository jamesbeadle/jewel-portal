using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

// Queues rows-only messages for every succeeded extraction that has a structure blob and (unless
// Force) no RowsWrittenAt stamp. Nothing on the rows changes here — the worker reads each blob
// and writes the rows, so the api call returns in the time it takes to send the messages, never
// the time it takes to read a project's worth of blobs (the SWA gateway's 45 s).
public sealed class RebuildDrawingDataRowsHandler : ICommandHandler<RebuildDrawingDataRows, DrawingDataRowsRebuild>
{
    private readonly JpmsContext context;
    private readonly IDrawingExtractionQueue queue;
    private readonly AuditActor actor;

    public RebuildDrawingDataRowsHandler(JpmsContext context, IDrawingExtractionQueue queue, AuditActor actor)
    {
        this.context = context; this.queue = queue; this.actor = actor;
    }

    public async Task<DrawingDataRowsRebuild> HandleAsync(RebuildDrawingDataRows command, CancellationToken cancellationToken)
    {
        // A blank project means every project — the connector sends "" as readily as null.
        var projectId = string.IsNullOrWhiteSpace(command.ProjectId) ? null : command.ProjectId.Trim();
        var candidates = await context.DrawingExtractions.AsNoTracking()
            .Where(row => row.Status == (int)DrawingExtractionStatus.Succeeded)
            .Where(row => row.StructureBlobRef != null)
            .Where(row => projectId == null || row.ProjectId == projectId)
            .Select(row => new { row.DrawingRevisionId, row.RowsWrittenAt })
            .ToListAsync(cancellationToken);

        var toQueue = candidates.Where(row => command.Force || row.RowsWrittenAt is null).ToList();
        foreach (var row in toQueue)
            await queue.EnqueueAsync(
                new DrawingExtractionMessage(row.DrawingRevisionId, actor.Email, RowsOnly: true), cancellationToken);

        return new DrawingDataRowsRebuild(toQueue.Count, candidates.Count - toQueue.Count);
    }
}

public sealed class RebuildDrawingDataRowsAuthorisation
{
    public bool Allows(SignedInUser user, RebuildDrawingDataRows command) =>
        DrawingExtractionRoles.AllowedToExtract.IncludesAny(user.Roles);
}

public sealed class RebuildDrawingDataRowsValidation
{
    // ProjectId is optional (blank = every project) and Force is a flag — nothing to reject.
    public ValidationOutcome Check(RebuildDrawingDataRows command) => ValidationOutcome.Passed;
}
