using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's absence entry (the record_worker_absence action): RecordWorkerAbsence keyed by
// worker NAME. There is no HTTP endpoint — the portal's overview grid posts RecordWorkerAbsence
// with a picker-chosen WorkerId — but an AI caller meets workers as names, so this slice resolves
// the name against the register (WorkerNameResolver, shared with SubmitWorkerWeekByName) and then
// delegates to the SAME RecordWorkerAbsenceHandler, recordedByEmail overload included, so the two
// entry paths cannot drift: same one-absence-per-date replace rule, same RecordedBy audit stamp.
// RecordedByEmail arrives stamped server-side (an EmailStamps parameter — never model-supplied).

public sealed class RecordWorkerAbsenceByNameAuthorisation
{
    // Same gate as RecordWorkerAbsenceEndpoint: absence is a ManageWorkers write (a forecast
    // input), NOT an ApproveTimesheets one.
    public bool Allows(SignedInUser user, RecordWorkerAbsenceByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class RecordWorkerAbsenceByNameValidation
{
    // The HTTP endpoint validates nothing beyond a readable body; the only whole-command check
    // worth adding here is the name the resolution needs. Kind is schema-bound to the enum.
    public ValidationOutcome Check(RecordWorkerAbsenceByName command) =>
        string.IsNullOrWhiteSpace(command.WorkerName)
            ? new ValidationOutcome(new[] { "Worker name is required." })
            : ValidationOutcome.Passed;
}

public sealed class RecordWorkerAbsenceByNameHandler : ICommandHandler<RecordWorkerAbsenceByName, WorkerAbsence>
{
    private readonly JpmsContext context;
    private readonly RecordWorkerAbsenceHandler inner;
    public RecordWorkerAbsenceByNameHandler(JpmsContext context, RecordWorkerAbsenceHandler inner)
    { this.context = context; this.inner = inner; }

    public async Task<WorkerAbsence> HandleAsync(RecordWorkerAbsenceByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "recording an absence against them");
        return await inner.HandleAsync(
            new RecordWorkerAbsence(worker.WorkerId, command.Date, command.Kind, command.Note ?? ""),
            command.RecordedByEmail,
            cancellationToken);
    }
}
