using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Site;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Removes a baseline and its task snapshots. Movement is always measured against the latest
// remaining baseline, so removing the current yardstick makes the previous one the yardstick
// again (or leaves the programme un-baselined). Like taking one, this deliberately resets the
// yardstick delay evidence is measured against — hence the Director/PM-only authorisation.
public sealed class RemoveProgrammeBaselineHandler : ICommandHandler<RemoveProgrammeBaseline, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProgrammeBaselineHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProgrammeBaseline command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeBaselines
            .FirstOrDefaultAsync(b => b.ProgrammeBaselineId == command.ProgrammeBaselineId, cancellationToken);
        if (entity is not null)
        {
            var snapshotTasks = await context.ProgrammeBaselineTasks
                .Where(t => t.ProgrammeBaselineId == command.ProgrammeBaselineId)
                .ToListAsync(cancellationToken);
            context.ProgrammeBaselineTasks.RemoveRange(snapshotTasks);
            context.ProgrammeBaselines.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ProgrammeBaselineId);
    }
}
