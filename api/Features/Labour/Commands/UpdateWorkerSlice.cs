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

public sealed class UpdateWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<UpdateWorker, Worker> handler;
    public UpdateWorkerEndpoint(SignedInUserResolver users, ICommandHandler<UpdateWorker, Worker> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(UpdateWorker))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "labour/workers/{workerId}")] HttpRequest request, string workerId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var body = await request.ReadFromJsonAsync<UpdateWorker>();
        if (body is null) return new BadRequestResult();
        var command = body with { WorkerId = workerId };
        if (string.IsNullOrWhiteSpace(command.Name)) return new BadRequestObjectResult(new[] { "Worker name is required." });
        if (command.HourlyRate <= 0m) return new BadRequestObjectResult(new[] { "Hourly rate must be greater than zero." });
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
