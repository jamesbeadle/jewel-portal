using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SubmitTimesheetHandler : ICommandHandler<SubmitTimesheet, Timesheet>
{
    private readonly JpmsContext context;
    public SubmitTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<Timesheet> HandleAsync(SubmitTimesheet command, CancellationToken cancellationToken)
    {
        var entity = new TimesheetEntity
        {
            TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
            ProjectId = command.ProjectId,
            PersonEmail = command.PersonEmail,
            WorkedOn = command.WorkedOn,
            Hours = command.Hours,
            CostCode = command.CostCode,
            IsApproved = false
        };
        context.Timesheets.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
