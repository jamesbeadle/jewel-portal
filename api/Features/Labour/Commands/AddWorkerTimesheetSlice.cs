using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// PM enters hours on a worker's behalf — the missed-sign-out path (scope §5: "attendance left
// open overnight is flagged in the PM approval view; the PM enters/adjusts hours on the
// worker's behalf"). Creates a Submitted timesheet; approval still goes through the normal gate.

public sealed class AddWorkerTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<AddWorkerTimesheet, TimesheetDetail> handler;
    public AddWorkerTimesheetEndpoint(SignedInUserResolver users, ICommandHandler<AddWorkerTimesheet, TimesheetDetail> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(AddWorkerTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/labour/timesheets")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var body = await request.ReadFromJsonAsync<AddWorkerTimesheet>();
        if (body is null || string.IsNullOrWhiteSpace(body.WorkerId)) return new BadRequestResult();
        var command = body with { ProjectId = projectId };
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

public sealed class AddWorkerTimesheetHandler : ICommandHandler<AddWorkerTimesheet, TimesheetDetail>
{
    private readonly JpmsContext context;
    public AddWorkerTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<TimesheetDetail> HandleAsync(AddWorkerTimesheet command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");

        var timesheet = new TimesheetEntity
        {
            TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
            ProjectId = command.ProjectId,
            PersonEmail = worker.ContactEmail,
            WorkerId = worker.WorkerId,
            WorkedOn = SiteClock.WorkDateOf(command.WorkedOn),
            Hours = command.Hours,
            CostCode = command.CostCode,
            Status = (int)TimesheetStatus.Submitted,
            IsApproved = false,
        };
        context.Timesheets.Add(timesheet);
        await context.SaveChangesAsync(cancellationToken);
        return timesheet.ToDetail(worker.Name);
    }
}
