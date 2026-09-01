using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// PM correction before approval: change hours and/or re-code. Approved timesheets are
// immutable — their cost has posted; the correction path is reject-and-resubmit or a
// settlement variance, never editing posted cost.

// Gate classes added 2026-08-28, closing the convention gap (endpoint composes them instead of
// inline checks). Same role set as before — LabourRoleSets.ApproveTimesheets, never widened. No
// registry action yet: the command is keyed by an opaque TimesheetId the connector cannot
// resolve, and adjustment/coding is the approver's portal activity (the queue carries the
// context) — expose via a by-name/date wrapper if a need appears.

public sealed class AdjustTimesheetAuthorisation
{
    public bool Allows(SignedInUser user, AdjustTimesheet command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class AdjustTimesheetValidation
{
    // Exactly the endpoint's former inline checks.
    public ValidationOutcome Check(AdjustTimesheet command)
    {
        var errors = new List<string>();
        if (!LabourRules.IsValidHours(command.Hours)) errors.Add("Hours must be in half-hour steps of at least 0.5.");
        if (string.IsNullOrWhiteSpace(command.CostCode)) errors.Add("A cost code is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class AdjustTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AdjustTimesheetAuthorisation authorisation;
    private readonly AdjustTimesheetValidation validation;
    private readonly ICommandHandler<AdjustTimesheet, TimesheetDetail> handler;
    public AdjustTimesheetEndpoint(SignedInUserResolver users, AdjustTimesheetAuthorisation authorisation,
        AdjustTimesheetValidation validation, ICommandHandler<AdjustTimesheet, TimesheetDetail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AdjustTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "labour/timesheets/{timesheetId}")] HttpRequest request, string timesheetId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<AdjustTimesheet>();
        if (body is null) return new BadRequestResult();
        var command = body with { TimesheetId = timesheetId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
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
