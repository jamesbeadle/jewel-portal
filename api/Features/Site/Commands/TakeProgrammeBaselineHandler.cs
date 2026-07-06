using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Commands;

// Snapshots every live programme task under a named baseline. Movement (and the contemporaneous
// delay evidence behind NOD/EOT claims) is measured against the latest baseline, so retaking one
// deliberately resets the yardstick — hence the Director/PM-only authorisation.
public sealed class TakeProgrammeBaselineHandler : ICommandHandler<TakeProgrammeBaseline, ProgrammeBaseline>
{
    private readonly JpmsContext context;
    public TakeProgrammeBaselineHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeBaseline> HandleAsync(TakeProgrammeBaseline command, CancellationToken cancellationToken)
    {
        var tasks = await context.ProgrammeTasks
            .Where(t => t.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);
        if (tasks.Count == 0)
            throw new InvalidOperationException("There are no programme tasks to baseline yet.");

        var baseline = new ProgrammeBaselineEntity
        {
            ProgrammeBaselineId = SiteIdentifierFactory.NextProgrammeBaselineId(),
            ProjectId = command.ProjectId,
            Label = command.Label,
            TakenByEmail = command.TakenByEmail,
            TakenAt = DateTimeOffset.UtcNow
        };
        context.ProgrammeBaselines.Add(baseline);

        foreach (var task in tasks)
        {
            context.ProgrammeBaselineTasks.Add(new ProgrammeBaselineTaskEntity
            {
                ProgrammeBaselineTaskId = SiteIdentifierFactory.NextProgrammeBaselineTaskId(),
                ProgrammeBaselineId = baseline.ProgrammeBaselineId,
                ProgrammeTaskId = task.ProgrammeTaskId,
                Title = task.Title,
                PlannedStart = task.PlannedStart,
                PlannedEnd = task.PlannedEnd
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return baseline.ToModel();
    }
}
