using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Chase-list dismissals (2026-08-31, the accountant's month-end doc, item G): the chase list is
// derived on every read and could never be cleared except by recording a timesheet or an absence
// one modal at a time. A dismissal is the FD/PM's decision that a reviewed day needs neither —
// persisted with a mandatory reason, written to the audit trail, and naturally superseded the
// moment a timesheet or absence lands on the day. The generator excludes dismissed days from the
// list and from the unconfirmed-cost accrual, so the confidence figures follow the decision.

// ---- DismissLabourChaseDay (id-keyed; the overview page's command) ----------------------------

public sealed class DismissLabourChaseDayAuthorisation
{
    // Same gate as the overview that shows the chase list.
    public bool Allows(SignedInUser user, DismissLabourChaseDay command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class DismissLabourChaseDayValidation
{
    public ValidationOutcome Check(DismissLabourChaseDay command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerId)) errors.Add("workerId is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — a dismissal is a decision, and the reason is the record of it.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class DismissLabourChaseDayEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DismissLabourChaseDayAuthorisation authorisation;
    private readonly DismissLabourChaseDayValidation validation;
    private readonly DismissLabourChaseDayHandler handler;
    public DismissLabourChaseDayEndpoint(SignedInUserResolver users, DismissLabourChaseDayAuthorisation authorisation,
        DismissLabourChaseDayValidation validation, DismissLabourChaseDayHandler handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DismissLabourChaseDay))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/chase/dismiss")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<DismissLabourChaseDay>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(
                command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException guard)
        {
            return new BadRequestObjectResult(new[] { guard.Message });
        }
    }
}

public sealed class DismissLabourChaseDayHandler : ICommandHandler<DismissLabourChaseDay, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public DismissLabourChaseDayHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public Task<Acknowledgement> HandleAsync(DismissLabourChaseDay command, CancellationToken cancellationToken) =>
        HandleAsync(command, command.DismissedByEmail, cancellationToken);

    public async Task<Acknowledgement> HandleAsync(DismissLabourChaseDay command, string dismissedByEmail, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");
        var date = SiteClock.WorkDateOf(command.Date);

        // One row per worker per day; dismissing again refreshes the reason and the actor.
        var existing = await context.LabourChaseDismissals
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.Date == date, cancellationToken);
        var entity = existing ?? new LabourChaseDismissalEntity
        {
            LabourChaseDismissalId = LabourIdentifierFactory.NextLabourChaseDismissalId(),
            WorkerId = command.WorkerId,
            Date = date,
        };
        entity.Reason = command.Reason.Trim();
        entity.DismissedByEmail = dismissedByEmail;
        entity.DismissedAt = DateTimeOffset.UtcNow;
        if (existing is null) context.LabourChaseDismissals.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            AuditEventType.LabourChaseDayDismissed,
            $"{worker.Name} {date:ddd dd MMM yyyy} dismissed from the chase list — {entity.Reason}",
            actorEmail: dismissedByEmail,
            cancellationToken: cancellationToken);

        return new Acknowledgement(entity.LabourChaseDismissalId);
    }
}

// ---- dismiss_labour_chase_day (by name, connector) --------------------------------------------

public sealed class DismissLabourChaseDayByNameAuthorisation
{
    public bool Allows(SignedInUser user, DismissLabourChaseDayByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class DismissLabourChaseDayByNameValidation
{
    public ValidationOutcome Check(DismissLabourChaseDayByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — a dismissal is a decision, and the reason is the record of it.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class DismissLabourChaseDayByNameHandler : ICommandHandler<DismissLabourChaseDayByName, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly DismissLabourChaseDayHandler dismiss;
    public DismissLabourChaseDayByNameHandler(JpmsContext context, DismissLabourChaseDayHandler dismiss)
    { this.context = context; this.dismiss = dismiss; }

    public async Task<Acknowledgement> HandleAsync(DismissLabourChaseDayByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "dismissing their chase day");
        return await dismiss.HandleAsync(
            new DismissLabourChaseDay(worker.WorkerId, command.Date, command.Reason),
            command.DismissedByEmail, cancellationToken);
    }
}

// ---- restore_labour_chase_day (by name, connector) --------------------------------------------

public sealed class RestoreLabourChaseDayByNameAuthorisation
{
    public bool Allows(SignedInUser user, RestoreLabourChaseDayByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class RestoreLabourChaseDayByNameValidation
{
    public ValidationOutcome Check(RestoreLabourChaseDayByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RestoreLabourChaseDayByNameHandler : ICommandHandler<RestoreLabourChaseDayByName, Acknowledgement>
{
    private readonly JpmsContext context;
    public RestoreLabourChaseDayByNameHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RestoreLabourChaseDayByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "restoring their chase day");
        var date = SiteClock.WorkDateOf(command.Date);
        var existing = await context.LabourChaseDismissals
            .FirstOrDefaultAsync(row => row.WorkerId == worker.WorkerId && row.Date == date, cancellationToken);
        if (existing is not null)
        {
            context.LabourChaseDismissals.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(worker.WorkerId);
    }
}
