using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Deletes a worker outright — but only while they have no history. Once timesheets or
// attendance exist, the record underpins posted/pending cost and the site register, so it
// can only be deactivated (Edit → Active off), never deleted.

public sealed class DeleteWorkerEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<DeleteWorker, Acknowledgement> handler;
    public DeleteWorkerEndpoint(SignedInUserResolver users, ICommandHandler<DeleteWorker, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(DeleteWorker))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "labour/workers/{workerId}")] HttpRequest request, string workerId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(new DeleteWorker(workerId), request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class DeleteWorkerHandler : ICommandHandler<DeleteWorker, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteWorkerHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteWorker command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException("Worker not found.");

        var hasTimesheets = await context.Timesheets.AnyAsync(
            timesheet => timesheet.WorkerId == command.WorkerId, cancellationToken);
        var hasAttendance = await context.SiteAttendances.AnyAsync(
            attendance => attendance.WorkerId == command.WorkerId, cancellationToken);
        if (hasTimesheets || hasAttendance)
            throw new InvalidOperationException(
                $"{worker.Name} has timesheet or site-register history, so they can't be deleted — their record backs recorded cost. Set them Inactive instead (Edit → untick Active).");

        var assignments = await context.ProjectWorkerAssignments
            .Where(assignment => assignment.WorkerId == command.WorkerId)
            .ToListAsync(cancellationToken);
        context.ProjectWorkerAssignments.RemoveRange(assignments);

        var rateHistory = await context.WorkerRateHistories
            .Where(history => history.WorkerId == command.WorkerId)
            .ToListAsync(cancellationToken);
        context.WorkerRateHistories.RemoveRange(rateHistory);

        context.Workers.Remove(worker);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.WorkerId);
    }
}
