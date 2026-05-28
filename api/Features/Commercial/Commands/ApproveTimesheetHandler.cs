using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ApproveTimesheetHandler : ICommandHandler<ApproveTimesheet, Timesheet>
{
    private readonly JpmsContext context;
    public ApproveTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<Timesheet> HandleAsync(ApproveTimesheet command, CancellationToken cancellationToken)
    {
        var entity = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");
        entity.IsApproved = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
