using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's retire_worker: RetireWorker keyed by the worker's NAME, resolved against the
// register as every other by-name labour command is (WorkerNameResolver) — here across active
// and inactive workers alike, since a worker marked inactive by hand is exactly who gets
// retired — then handed to the SAME RetireWorkerHandler the page posts to.

public sealed class RetireWorkerByNameAuthorisation
{
    public bool Allows(SignedInUser user, RetireWorkerByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class RetireWorkerByNameValidation
{
    public ValidationOutcome Check(RetireWorkerByName command) =>
        string.IsNullOrWhiteSpace(command.WorkerName)
            ? new ValidationOutcome(new[] { "Worker name is required." })
            : ValidationOutcome.Passed;
}

public sealed class RetireWorkerByNameHandler : ICommandHandler<RetireWorkerByName, Worker>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<RetireWorker, Worker> inner;
    public RetireWorkerByNameHandler(JpmsContext context, ICommandHandler<RetireWorker, Worker> inner)
    { this.context = context; this.inner = inner; }

    public async Task<Worker> HandleAsync(RetireWorkerByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.ResolveWhetherActiveOrNot(workers, command.WorkerName);
        return await inner.HandleAsync(new RetireWorker(worker.WorkerId), cancellationToken);
    }
}
