using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's month-end leg (2026-08-31, the accountant's ask): sign-off and the Xero coding
// run as by-name wrappers over the Labour overview's OWN handlers, the same construction as
// approve_worker_week — so the two surfaces cannot drift: same LabourRoleSets gates as the
// endpoints, same signable rule re-checked at the moment of signing, same skip-and-report gates
// inside the coding run. Names resolve through WorkerNameResolver (shared with week entry);
// weeks normalise to Monday inside the delegated handlers, and the month whose part of the week
// is meant follows the same rule as the overview (LabourWeekParts): the given monthStart, else
// the month of the weekStart date as given.

// ---- sign_off_labour_week ---------------------------------------------------------------------

public sealed class SignOffWorkerWeekByNameAuthorisation
{
    // Same gate as SignOffLabourWeekEndpoint's inline check.
    public bool Allows(SignedInUser user, SignOffWorkerWeekByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class SignOffWorkerWeekByNameValidation
{
    public ValidationOutcome Check(SignOffWorkerWeekByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SignOffWorkerWeekByNameHandler : ICommandHandler<SignOffWorkerWeekByName, LabourWeekSignOff>
{
    private readonly JpmsContext context;
    private readonly SignOffLabourWeekHandler signOff;
    public SignOffWorkerWeekByNameHandler(JpmsContext context, SignOffLabourWeekHandler signOff)
    { this.context = context; this.signOff = signOff; }

    public async Task<LabourWeekSignOff> HandleAsync(SignOffWorkerWeekByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "signing their week off");
        try
        {
            // The overview's own handler: normalises the Monday, re-checks the signable rule at
            // the moment of signing, upserts the marker with the caller stamped.
            return await signOff.HandleAsync(
                new SignOffLabourWeek(worker.WorkerId, command.WeekStart, command.MonthStart),
                command.SignedOffByEmail, cancellationToken);
        }
        catch (WeekNotSignableException refusal)
        {
            // The gateway answers InvalidOperationException as a message, not a 500 — same
            // courtesy the HTTP endpoint gives this refusal with its 409.
            throw new InvalidOperationException(
                $"{worker.Name}'s week cannot be signed off: {refusal.Message} "
                + "view_labour_week shows the days; record_worker_absence covers a genuine absence.");
        }
    }
}

// ---- remove_labour_week_sign_off --------------------------------------------------------------

public sealed class RemoveWorkerWeekSignOffByNameAuthorisation
{
    // Same gate as RemoveLabourWeekSignOffEndpoint's inline check.
    public bool Allows(SignedInUser user, RemoveWorkerWeekSignOffByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class RemoveWorkerWeekSignOffByNameValidation
{
    public ValidationOutcome Check(RemoveWorkerWeekSignOffByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RemoveWorkerWeekSignOffByNameHandler : ICommandHandler<RemoveWorkerWeekSignOffByName, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement> remove;
    public RemoveWorkerWeekSignOffByNameHandler(JpmsContext context, ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement> remove)
    { this.context = context; this.remove = remove; }

    public async Task<Acknowledgement> HandleAsync(RemoveWorkerWeekSignOffByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "removing their week's sign-off");
        return await remove.HandleAsync(
            new RemoveLabourWeekSignOff(worker.WorkerId, command.WeekStart, command.MonthStart), cancellationToken);
    }
}

// ---- run_xero_coding --------------------------------------------------------------------------

public sealed class RunXeroCodingByNameAuthorisation
{
    // Same gate as RunXeroCodingEndpoint's inline check.
    public bool Allows(SignedInUser user, RunXeroCodingByName command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class RunXeroCodingByNameValidation
{
    public ValidationOutcome Check(RunXeroCodingByName command)
    {
        var errors = new List<string>();
        // Same bounds as the endpoint's inline check.
        if (command.Year < 2020 || command.Year > 2100)
            errors.Add("Year must be between 2020 and 2100.");
        if (command.Month < 1 || command.Month > 12)
            errors.Add("Month must be between 1 and 12.");
        if (command.WorkerNames is { Count: > 0 } && command.WorkerNames.Any(string.IsNullOrWhiteSpace))
            errors.Add("workerNames must not contain blank entries — leave the list out to run every worker.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RunXeroCodingByNameHandler : ICommandHandler<RunXeroCodingByName, XeroCodingRunReport>
{
    private readonly JpmsContext context;
    private readonly RunXeroCodingHandler runner;
    public RunXeroCodingByNameHandler(JpmsContext context, RunXeroCodingHandler runner)
    { this.context = context; this.runner = runner; }

    public async Task<XeroCodingRunReport> HandleAsync(RunXeroCodingByName command, CancellationToken cancellationToken)
    {
        // Names → register ids up front, so a typo refuses the whole run with guidance instead of
        // silently running nobody (the id-keyed handler treats an unknown id as "not wanted").
        List<string>? workerIds = null;
        if (command.WorkerNames is { Count: > 0 })
        {
            var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
            workerIds = command.WorkerNames
                .Select(name => WorkerNameResolver.Resolve(workers, name, "running the Xero coding").WorkerId)
                .Distinct()
                .ToList();
        }

        var outcomes = await runner.HandleAsync(
            new RunXeroCoding(command.Year, command.Month, workerIds),
            command.RunByEmail, cancellationToken);
        return new XeroCodingRunReport(command.Year, command.Month, outcomes);
    }
}
