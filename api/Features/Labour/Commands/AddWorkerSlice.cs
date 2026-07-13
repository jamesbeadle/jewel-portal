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

public sealed class AddWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<AddWorker, Worker> handler;
    public AddWorkerEndpoint(SignedInUserResolver users, ICommandHandler<AddWorker, Worker> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(AddWorker))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var command = await request.ReadFromJsonAsync<AddWorker>();
        if (command is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.Name)) return new BadRequestObjectResult(new[] { "Worker name is required." });
        if (command.HourlyRate <= 0m) return new BadRequestObjectResult(new[] { "Hourly rate must be greater than zero." });
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
