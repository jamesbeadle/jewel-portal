using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Rejection re-opens the day for the worker on the capture page. No deadline is enforced (site
// reality — scope §6); the rejected entry stays visible in the approval view until resolved,
// and the PM can always adjust-and-approve instead.

public sealed class RejectTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RejectTimesheet, TimesheetDetail> handler;
    public RejectTimesheetEndpoint(SignedInUserResolver users, ICommandHandler<RejectTimesheet, TimesheetDetail> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RejectTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/timesheets/{timesheetId}/rejection")] HttpRequest request, string timesheetId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var body = await request.ReadFromJsonAsync<RejectTimesheet>();
        if (body is null || string.IsNullOrWhiteSpace(body.Reason))
            return new BadRequestObjectResult(new[] { "A rejection reason is required." });
        try
        {
            return new OkObjectResult(await handler.HandleAsync(body with { TimesheetId = timesheetId }, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class RejectTimesheetHandler : ICommandHandler<RejectTimesheet, TimesheetDetail>
{
    private readonly JpmsContext context;
    public RejectTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<TimesheetDetail> HandleAsync(RejectTimesheet command, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");
        if (timesheet.Status == (int)TimesheetStatus.Approved)
            throw new InvalidOperationException("Approved timesheets can't be rejected — their cost has already posted.");

        timesheet.Status = (int)TimesheetStatus.Rejected;
        timesheet.IsApproved = false;
        timesheet.RejectionReason = command.Reason.Trim();
        await context.SaveChangesAsync(cancellationToken);

        var workerName = timesheet.WorkerId == "" ? timesheet.PersonEmail
            : (await context.Workers.FindAsync(new object[] { timesheet.WorkerId }, cancellationToken))?.Name ?? timesheet.PersonEmail;
        return timesheet.ToDetail(workerName);
    }
}
