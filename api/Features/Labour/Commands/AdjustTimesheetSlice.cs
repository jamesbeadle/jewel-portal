using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// PM correction before approval: change hours and/or re-code. Approved timesheets are
// immutable — their cost has posted; the correction path is reject-and-resubmit or a
// settlement variance, never editing posted cost.

public sealed class AdjustTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<AdjustTimesheet, TimesheetDetail> handler;
    public AdjustTimesheetEndpoint(SignedInUserResolver users, ICommandHandler<AdjustTimesheet, TimesheetDetail> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(AdjustTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "labour/timesheets/{timesheetId}")] HttpRequest request, string timesheetId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var body = await request.ReadFromJsonAsync<AdjustTimesheet>();
        if (body is null) return new BadRequestResult();
        var command = body with { TimesheetId = timesheetId };
        if (!LabourRules.IsValidHours(command.Hours)) return new BadRequestObjectResult(new[] { "Hours must be in half-hour steps of at least 0.5." });
        if (string.IsNullOrWhiteSpace(command.CostCode)) return new BadRequestObjectResult(new[] { "A cost code is required." });
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class AdjustTimesheetHandler : ICommandHandler<AdjustTimesheet, TimesheetDetail>
{
    private readonly JpmsContext context;
    public AdjustTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<TimesheetDetail> HandleAsync(AdjustTimesheet command, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");
        if (timesheet.Status == (int)TimesheetStatus.Approved)
            throw new InvalidOperationException("Approved timesheets can't be adjusted — their cost has already posted.");

        timesheet.Hours = command.Hours;
        timesheet.CostCode = command.CostCode;
        await context.SaveChangesAsync(cancellationToken);

        var workerName = timesheet.WorkerId == "" ? timesheet.PersonEmail
            : (await context.Workers.FindAsync(new object[] { timesheet.WorkerId }, cancellationToken))?.Name ?? timesheet.PersonEmail;
        return timesheet.ToDetail(workerName);
    }
}
