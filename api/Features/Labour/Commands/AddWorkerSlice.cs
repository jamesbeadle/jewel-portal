using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Gate classes added 2026-08-28 (the connector's add_worker action executes through them); the
// endpoint composes the SAME classes instead of its former inline checks, so the two entry paths
// cannot drift. Same role set as before — LabourRoleSets.ManageWorkers, never widened.

public sealed class AddWorkerAuthorisation
{
    public bool Allows(SignedInUser user, AddWorker command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class AddWorkerValidation
{
    // Exactly the endpoint's former inline checks.
    public ValidationOutcome Check(AddWorker command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Worker name is required.");
        if (command.HourlyRate <= 0m) errors.Add("Hourly rate must be greater than zero.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class AddWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddWorkerAuthorisation authorisation;
    private readonly AddWorkerValidation validation;
    private readonly ICommandHandler<AddWorker, Worker> handler;
    public AddWorkerEndpoint(SignedInUserResolver users, AddWorkerAuthorisation authorisation,
        AddWorkerValidation validation, ICommandHandler<AddWorker, Worker> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AddWorker))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AddWorker>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class AddWorkerHandler : ICommandHandler<AddWorker, Worker>
{
    private readonly JpmsContext context;
    public AddWorkerHandler(JpmsContext context) { this.context = context; }

    public async Task<Worker> HandleAsync(AddWorker command, CancellationToken cancellationToken)
    {
        var worker = new WorkerEntity
        {
            WorkerId = LabourIdentifierFactory.NextWorkerId(),
            Name = command.Name.Trim(),
            SubcontractorId = string.IsNullOrWhiteSpace(command.SubcontractorId) ? null : command.SubcontractorId,
            HourlyRate = command.HourlyRate,
            IsActive = true,
            ContactEmail = command.ContactEmail ?? "",
            ContactPhone = command.ContactPhone ?? "",
            IsSoleTrader = command.IsSoleTrader,
            EngagedFrom = command.EngagedFrom,
            EngagedTo = command.EngagedTo,
        };
        context.Workers.Add(worker);
        context.WorkerRateHistories.Add(new WorkerRateHistoryEntity
        {
            WorkerRateHistoryId = LabourIdentifierFactory.NextWorkerRateHistoryId(),
            WorkerId = worker.WorkerId,
            HourlyRate = command.HourlyRate,
            EffectiveFrom = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(cancellationToken);
        return worker.ToModel();
    }
}
