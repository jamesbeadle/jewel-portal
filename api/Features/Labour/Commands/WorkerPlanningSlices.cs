using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Worker planning: contracted days, CIS status, absence. All ManageWorkers-gated — these are
// forecast inputs that carry £ implications, owned by the same roles as rates.

public sealed class SetWorkerContractEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetWorkerContract, Acknowledgement> handler;
    public SetWorkerContractEndpoint(SignedInUserResolver users, ICommandHandler<SetWorkerContract, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetWorkerContract))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers/contract")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SetWorkerContract>();
        if (command is null) return new BadRequestResult();
        if (command.ContractedDaysPerMonth < 0m || command.ContractedDaysPerMonth > 31m)
            return new BadRequestObjectResult(new[] { "Contracted days must be between 0 and 31." });
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class SetWorkerContractHandler : ICommandHandler<SetWorkerContract, Acknowledgement>
{
    private readonly JpmsContext context;
    public SetWorkerContractHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetWorkerContract command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken);
        if (worker is null) throw new InvalidOperationException($"Worker {command.WorkerId} does not exist.");
        context.WorkerContracts.Add(new WorkerContractEntity
        {
            WorkerContractId = LabourIdentifierFactory.NextWorkerContractId(),
            WorkerId = command.WorkerId,
            ContractedDaysPerMonth = command.ContractedDaysPerMonth,
            EffectiveFrom = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.WorkerId);
    }
}

public sealed class SetWorkerCisStatusEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetWorkerCisStatus, Acknowledgement> handler;
    public SetWorkerCisStatusEndpoint(SignedInUserResolver users, ICommandHandler<SetWorkerCisStatus, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetWorkerCisStatus))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers/cis")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SetWorkerCisStatus>();
        if (command is null) return new BadRequestResult();
        if (command.CisRatePercent is not (0m or 20m or 30m))
            return new BadRequestObjectResult(new[] { "CIS rate must be 0 (gross), 20 (standard) or 30 (unverified)." });
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class SetWorkerCisStatusHandler : ICommandHandler<SetWorkerCisStatus, Acknowledgement>
{
    private readonly JpmsContext context;
    public SetWorkerCisStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetWorkerCisStatus command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken);
        if (worker is null) throw new InvalidOperationException($"Worker {command.WorkerId} does not exist.");
        context.WorkerCisStatuses.Add(new WorkerCisStatusEntity
        {
            WorkerCisStatusId = LabourIdentifierFactory.NextWorkerCisStatusId(),
            WorkerId = command.WorkerId,
            CisRatePercent = command.CisRatePercent,
            VerifiedRef = command.VerifiedRef ?? "",
            EffectiveFrom = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.WorkerId);
    }
}

public sealed class RecordWorkerAbsenceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordWorkerAbsenceHandler handler;
    public RecordWorkerAbsenceEndpoint(SignedInUserResolver users, RecordWorkerAbsenceHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RecordWorkerAbsence))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/absences")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RecordWorkerAbsence>();
        if (command is null) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class RecordWorkerAbsenceHandler : ICommandHandler<RecordWorkerAbsence, WorkerAbsence>
{
    private readonly JpmsContext context;
    public RecordWorkerAbsenceHandler(JpmsContext context) { this.context = context; }

    public Task<WorkerAbsence> HandleAsync(RecordWorkerAbsence command, CancellationToken cancellationToken) =>
        HandleAsync(command, recordedByEmail: "", cancellationToken);

    public async Task<WorkerAbsence> HandleAsync(RecordWorkerAbsence command, string recordedByEmail, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken);
        if (worker is null) throw new InvalidOperationException($"Worker {command.WorkerId} does not exist.");
        var date = SiteClock.WorkDateOf(command.Date);

        // One absence per worker per date — recording again replaces the kind and note.
        var existing = await context.WorkerAbsences
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.Date == date, cancellationToken);
        var entity = existing ?? new WorkerAbsenceEntity
        {
            WorkerAbsenceId = LabourIdentifierFactory.NextWorkerAbsenceId(),
            WorkerId = command.WorkerId,
            Date = date,
        };
        entity.Kind = (int)command.Kind;
        entity.Note = command.Note ?? "";
        entity.RecordedByEmail = recordedByEmail;
        entity.RecordedAt = DateTimeOffset.UtcNow;
        if (existing is null) context.WorkerAbsences.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new WorkerAbsence(entity.WorkerAbsenceId, entity.WorkerId, worker.Name, entity.Date,
            (AbsenceKind)entity.Kind, entity.Note, entity.RecordedByEmail, entity.RecordedAt);
    }
}

public sealed class RemoveWorkerAbsenceEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RemoveWorkerAbsence, Acknowledgement> handler;
    public RemoveWorkerAbsenceEndpoint(SignedInUserResolver users, ICommandHandler<RemoveWorkerAbsence, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RemoveWorkerAbsence))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/absences/remove")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RemoveWorkerAbsence>();
        if (command is null) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class RemoveWorkerAbsenceHandler : ICommandHandler<RemoveWorkerAbsence, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveWorkerAbsenceHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveWorkerAbsence command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkerAbsences.FindAsync(new object[] { command.WorkerAbsenceId }, cancellationToken);
        if (entity is not null)
        {
            context.WorkerAbsences.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.WorkerAbsenceId);
    }
}
