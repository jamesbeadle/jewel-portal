using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The signed-in worker fixes one of their own rejected days. Only their own Rejected
// timesheets can be resubmitted; no deadline is enforced (scope §6). Back to Submitted.

public sealed class MyResubmitTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MyResubmitTimesheetHandler handler;
    public MyResubmitTimesheetEndpoint(SignedInUserResolver users, MyResubmitTimesheetHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MyResubmitTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/resubmit")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var command = await request.ReadFromJsonAsync<MyResubmitTimesheet>();
        if (command is null || string.IsNullOrWhiteSpace(command.TimesheetId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class MyResubmitTimesheetHandler : ICommandHandler<MyResubmitTimesheet, Acknowledgement>
{
    private readonly JpmsContext context;
    public MyResubmitTimesheetHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(MyResubmitTimesheet command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MyResubmitTimesheet requires the signed-in email — use the endpoint.");

    public async Task<Acknowledgement> HandleAsync(MyResubmitTimesheet command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);

        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException("Timesheet not found.");
        if (timesheet.WorkerId != worker.WorkerId)
            throw new InvalidOperationException("This timesheet isn't yours to resubmit.");
        if (timesheet.Status != (int)TimesheetStatus.Rejected)
            throw new InvalidOperationException("Only rejected timesheets can be resubmitted.");
        if (!LabourRules.IsValidHours(command.Hours))
            throw new InvalidOperationException("Hours must be in half-hour steps of at least 0.5.");
        if (string.IsNullOrWhiteSpace(command.CostCode))
            throw new InvalidOperationException("A cost code is required.");

        timesheet.Hours = command.Hours;
        timesheet.CostCode = command.CostCode;
        timesheet.Status = (int)TimesheetStatus.Submitted;
        timesheet.RejectionReason = "";
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(timesheet.TimesheetId);
    }
}
