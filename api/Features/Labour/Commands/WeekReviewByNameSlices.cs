using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>The connector's leg of the week review and the week planner (2026-10-01): by-name wrappers
/// over the portal's OWN handlers — SignOffWorkerWeekHandler, SendBackWorkerWeekHandler and
/// PlanWorkerDaysHandler — so the two surfaces cannot drift: the same director gate, the same
/// approval door inside the sign-off, the same refusals. Names resolve through WorkerNameResolver and
/// a week is any date in it.</summary>
public sealed class SignOffSubmittedWeekByNameAuthorisation
{
    public bool Allows(SignedInUser user, SignOffSubmittedWeekByName command) =>
        LabourRoleSets.ReviewWorkerWeeks.IncludesAny(user.Roles);
}

public sealed class SignOffSubmittedWeekByNameValidation
{
    public ValidationOutcome Check(SignOffSubmittedWeekByName command) =>
        SubmittedWeekRules.Check(command.WorkerName);
}

public sealed class SignOffSubmittedWeekByNameHandler : ICommandHandler<SignOffSubmittedWeekByName, WorkerWeekSubmission>
{
    private readonly JpmsContext context;
    private readonly SignOffWorkerWeekHandler signOff;
    public SignOffSubmittedWeekByNameHandler(JpmsContext context, SignOffWorkerWeekHandler signOff) { this.context = context; this.signOff = signOff; }

    public async Task<WorkerWeekSubmission> HandleAsync(SignOffSubmittedWeekByName command, CancellationToken cancellationToken)
    {
        var submission = await SubmittedWeekRules.FindAsync(context, command.WorkerName, command.WeekStart, "signing their week off", cancellationToken);
        return await signOff.HandleAsync(new SignOffWorkerWeek(submission.WorkerWeekSubmissionId), command.SignedOffByEmail, cancellationToken);
    }
}

public sealed class SendBackSubmittedWeekByNameAuthorisation
{
    public bool Allows(SignedInUser user, SendBackSubmittedWeekByName command) =>
        LabourRoleSets.ReviewWorkerWeeks.IncludesAny(user.Roles);
}

public sealed class SendBackSubmittedWeekByNameValidation
{
    public ValidationOutcome Check(SendBackSubmittedWeekByName command)
    {
        var outcome = SubmittedWeekRules.Check(command.WorkerName);
        if (!string.IsNullOrWhiteSpace(command.Note)) return outcome;
        return new ValidationOutcome(outcome.Errors.Append("Say what needs changing — the note is what the operative reads.").ToList());
    }
}

public sealed class SendBackSubmittedWeekByNameHandler : ICommandHandler<SendBackSubmittedWeekByName, WorkerWeekSubmission>
{
    private readonly JpmsContext context;
    private readonly SendBackWorkerWeekHandler sendBack;
    public SendBackSubmittedWeekByNameHandler(JpmsContext context, SendBackWorkerWeekHandler sendBack) { this.context = context; this.sendBack = sendBack; }

    public async Task<WorkerWeekSubmission> HandleAsync(SendBackSubmittedWeekByName command, CancellationToken cancellationToken)
    {
        var submission = await SubmittedWeekRules.FindAsync(context, command.WorkerName, command.WeekStart, "sending their week back", cancellationToken);
        return await sendBack.HandleAsync(new SendBackWorkerWeek(submission.WorkerWeekSubmissionId, command.Note), command.ReviewedByEmail, cancellationToken);
    }
}

public sealed class PlanWorkerDaysByNameAuthorisation
{
    public bool Allows(SignedInUser user, PlanWorkerDaysByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class PlanWorkerDaysByNameValidation
{
    public ValidationOutcome Check(PlanWorkerDaysByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        if (command.Dates is not { Count: > 0 }) errors.Add("At least one date is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class PlanWorkerDaysByNameHandler : ICommandHandler<PlanWorkerDaysByName, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly PlanWorkerDaysHandler planner;
    public PlanWorkerDaysByNameHandler(JpmsContext context, PlanWorkerDaysHandler planner) { this.context = context; this.planner = planner; }

    public async Task<Acknowledgement> HandleAsync(PlanWorkerDaysByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "planning their week");
        return await planner.HandleAsync(new PlanWorkerDays(worker.WorkerId, command.Dates, command.IsIn), command.PlannedByEmail, cancellationToken);
    }
}

/// <summary>How a by-name review command finds its submission: the named worker's row for the week
/// any date names, which must exist — a week never submitted has nothing to answer.</summary>
public static class SubmittedWeekRules
{
    public static ValidationOutcome Check(string workerName) =>
        string.IsNullOrWhiteSpace(workerName) ? new ValidationOutcome(new[] { "Worker name is required." }) : ValidationOutcome.Passed;

    public static async Task<WorkerWeekSubmissionEntity> FindAsync(
        JpmsContext context, string workerName, DateTimeOffset weekStart, string activityPhrase, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, workerName, activityPhrase);
        var monday = LabourWeeks.MondayOf(weekStart);
        var submission = await WorkerWeekSubmissions.FindAsync(context, worker.WorkerId, monday, cancellationToken);
        if (submission is null)
            throw new InvalidOperationException($"{worker.Name} has not submitted the week of {monday:dd MMM yyyy} — list_submitted_weeks shows the weeks waiting.");
        return submission;
    }
}
