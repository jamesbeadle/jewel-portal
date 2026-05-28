using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class UpdateProgrammeTaskHandler : ICommandHandler<UpdateProgrammeTask, ProgrammeTask>
{
    private readonly JpmsContext context;
    public UpdateProgrammeTaskHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeTask> HandleAsync(UpdateProgrammeTask command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeTasks.FindAsync(new object[] { command.ProgrammeTaskId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Programme task {command.ProgrammeTaskId} not found.");
        entity.Title = command.Title;
        entity.PlannedStart = command.PlannedStart;
        entity.PlannedEnd = command.PlannedEnd;
        entity.ProgressPercent = command.ProgressPercent;
        entity.BoqLineItemId = command.BoqLineItemId;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
