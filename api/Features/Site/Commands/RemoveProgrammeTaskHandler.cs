using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Site;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Removes a live programme task together with any dependency links that touch it (a link without
// both ends is meaningless). Baseline snapshots are deliberately kept: they are the
// contemporaneous record movement is measured against, and ProgrammeMovementCalculator simply
// skips snapshot rows whose live task no longer exists.
public sealed class RemoveProgrammeTaskHandler : ICommandHandler<RemoveProgrammeTask, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProgrammeTaskHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProgrammeTask command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeTasks
            .FirstOrDefaultAsync(t => t.ProgrammeTaskId == command.ProgrammeTaskId, cancellationToken);
        if (entity is not null)
        {
            var links = await context.ProgrammeTaskLinks
                .Where(l => l.PredecessorTaskId == command.ProgrammeTaskId || l.SuccessorTaskId == command.ProgrammeTaskId)
                .ToListAsync(cancellationToken);
            context.ProgrammeTaskLinks.RemoveRange(links);
            context.ProgrammeTasks.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ProgrammeTaskId);
    }
}
