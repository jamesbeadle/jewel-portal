using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AddProgrammeTaskHandler : ICommandHandler<AddProgrammeTask, ProgrammeTask>
{
    private readonly JpmsContext context;
    public AddProgrammeTaskHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeTask> HandleAsync(AddProgrammeTask command, CancellationToken cancellationToken)
    {
        var entity = new ProgrammeTaskEntity
        {
            ProgrammeTaskId = SiteIdentifierFactory.NextProgrammeTaskId(),
            ProjectId = command.ProjectId,
            Title = command.Title,
            PlannedStart = command.PlannedStart,
            PlannedEnd = command.PlannedEnd,
            ProgressPercent = 0,
            BoqLineItemId = command.BoqLineItemId
        };
        context.ProgrammeTasks.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
