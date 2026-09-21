using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Retires a worker (data protection, 2026-09-21): the answer for a worker with history, whom
// DeleteWorker rightly refuses. Their contact details are cleared and their engagement closed;
// their name, rate history and every timesheet stay, because recorded cost, the site register
// and the CIS returns are built on them. Retiring twice is harmless.

public sealed class RetireWorkerAuthorisation
{
    public bool Allows(SignedInUser user, RetireWorker command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class RetireWorkerValidation
{
    public ValidationOutcome Check(RetireWorker command) =>
        string.IsNullOrWhiteSpace(command.WorkerId)
            ? new ValidationOutcome(new[] { "WorkerId is required." })
            : ValidationOutcome.Passed;
}

public sealed class RetireWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RetireWorkerAuthorisation authorisation;
    private readonly RetireWorkerValidation validation;
    private readonly ICommandHandler<RetireWorker, Worker> handler;

    public RetireWorkerEndpoint(
        SignedInUserResolver users, RetireWorkerAuthorisation authorisation,
        RetireWorkerValidation validation, ICommandHandler<RetireWorker, Worker> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RetireWorker))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers/{workerId}/retire")] HttpRequest request, string workerId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RetireWorker(workerId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException refusal)
        {
            return new BadRequestObjectResult(new[] { refusal.Message });
        }
    }
}

public sealed class RetireWorkerHandler : ICommandHandler<RetireWorker, Worker>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public RetireWorkerHandler(JpmsContext context, AuditTrail audit) { this.context = context; this.audit = audit; }

    public async Task<Worker> HandleAsync(RetireWorker command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException("Worker not found.");
        var now = DateTimeOffset.UtcNow;

        worker.ContactEmail = "";
        worker.ContactPhone = "";
        worker.IsActive = false;
        worker.EngagedTo ??= now;
        worker.RetiredAt ??= now;
        await context.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(AuditEventType.WorkerRetired,
            $"{worker.Name} retired: contact details cleared, engagement closed; name and timesheets kept.",
            cancellationToken: cancellationToken);
        return worker.ToModel();
    }
}
