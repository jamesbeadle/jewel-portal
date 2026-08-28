using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Gate classes added 2026-08-28, closing the convention gap (endpoint composes them instead of
// inline checks). Same role set as before — LabourRoleSets.ManageWorkers, never widened. No
// registry action yet: the command is keyed by an opaque WorkerId the connector cannot resolve —
// expose via a by-name wrapper (the SubmitWorkerWeekByName pattern) if a need appears.

public sealed class UpdateWorkerAuthorisation
{
    public bool Allows(SignedInUser user, UpdateWorker command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class UpdateWorkerValidation
{
    // Exactly the endpoint's former inline checks.
    public ValidationOutcome Check(UpdateWorker command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Worker name is required.");
        if (command.HourlyRate <= 0m) errors.Add("Hourly rate must be greater than zero.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateWorkerAuthorisation authorisation;
    private readonly UpdateWorkerValidation validation;
    private readonly ICommandHandler<UpdateWorker, Worker> handler;
    public UpdateWorkerEndpoint(SignedInUserResolver users, UpdateWorkerAuthorisation authorisation,
        UpdateWorkerValidation validation, ICommandHandler<UpdateWorker, Worker> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateWorker))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "labour/workers/{workerId}")] HttpRequest request, string workerId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<UpdateWorker>();
        if (body is null) return new BadRequestResult();
        var command = body with { WorkerId = workerId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class UpdateWorkerHandler : ICommandHandler<UpdateWorker, Worker>
{
    private readonly JpmsContext context;
    public UpdateWorkerHandler(JpmsContext context) { this.context = context; }

    public async Task<Worker> HandleAsync(UpdateWorker command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");

        // A rate change appends to history (effective now). Approved timesheets keep their
        // snapshotted rate; unapproved ones will pick up the new rate at approval.
        if (worker.HourlyRate != command.HourlyRate)
        {
            context.WorkerRateHistories.Add(new WorkerRateHistoryEntity
            {
                WorkerRateHistoryId = LabourIdentifierFactory.NextWorkerRateHistoryId(),
                WorkerId = worker.WorkerId,
                HourlyRate = command.HourlyRate,
                EffectiveFrom = DateTimeOffset.UtcNow,
            });
        }

        worker.Name = command.Name.Trim();
        worker.HourlyRate = command.HourlyRate;
        worker.IsActive = command.IsActive;
        worker.SubcontractorId = string.IsNullOrWhiteSpace(command.SubcontractorId) ? null : command.SubcontractorId;
        worker.ContactEmail = command.ContactEmail ?? "";
        worker.ContactPhone = command.ContactPhone ?? "";
        await context.SaveChangesAsync(cancellationToken);
        return worker.ToModel();
    }
}
