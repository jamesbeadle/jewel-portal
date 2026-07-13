using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Anonymous, token-authenticated (SiteAccessGate): a worker fixes a rejected day. Only their
// own Rejected timesheets on this project can be resubmitted; no deadline is enforced (site
// reality — scope §6). Resubmission returns the timesheet to Submitted for re-approval.

public sealed class ResubmitTimesheetEndpoint
{
    private readonly SiteAccessGate gate;
    private readonly ICommandHandler<ResubmitTimesheet, Acknowledgement> handler;
    public ResubmitTimesheetEndpoint(SiteAccessGate gate, ICommandHandler<ResubmitTimesheet, Acknowledgement> handler)
    { this.gate = gate; this.handler = handler; }

    [Function(nameof(ResubmitTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-labour/{token}/resubmit")] HttpRequest request, string token)
    {
        var access = await gate.ResolveAsync(token, request.HttpContext.RequestAborted);
        if (access is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<ResubmitTimesheet>();
        if (body is null || string.IsNullOrWhiteSpace(body.WorkerId) || string.IsNullOrWhiteSpace(body.TimesheetId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(body with { Token = token }, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class ResubmitTimesheetHandler : ICommandHandler<ResubmitTimesheet, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SiteAccessGate gate;
    public ResubmitTimesheetHandler(JpmsContext context, SiteAccessGate gate) { this.context = context; this.gate = gate; }

    public async Task<Acknowledgement> HandleAsync(ResubmitTimesheet command, CancellationToken cancellationToken)
    {
        var access = await gate.ResolveAsync(command.Token, cancellationToken)
            ?? throw new InvalidOperationException("Site access token is not valid.");

        var timesheet = await context.Timesheets.FindAsync(new object[] { command.TimesheetId }, cancellationToken)
            ?? throw new InvalidOperationException("Timesheet not found.");

        if (timesheet.ProjectId != access.ProjectId || timesheet.WorkerId != command.WorkerId)
            throw new InvalidOperationException("This timesheet is not yours to resubmit.");
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
