using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The connector's week entry (the submit_worker_week action): SubmitWorkerWeek keyed by worker
// NAME. There is no HTTP endpoint — the portal's Enter-a-week form posts SubmitWorkerWeek with a
// picker-chosen WorkerId — but an AI caller meets workers as names, so this slice resolves the
// name against the register and then delegates to the SAME SubmitWorkerWeekHandler, so the two
// entry paths cannot drift: same skip rules, same Submitted status, same approval queue.
//
// This slice exists because the connector previously exposed only the LEGACY Commercial
// SubmitTimesheet (pre-worker-register: free-typed personEmail, no WorkerId, cost code
// mandatory), which taught models to demand worker emails the portal does not need and produced
// rows the Labour approval refuses ("No worker record"). Found 2026-08-28 when the accountant's
// first connector session did exactly that.
//
// One deliberate loosening against the form's validation: the SAME date may appear on TWO
// DIFFERENT sites ("Wednesday — Coombe/Ravenswood, half day each"), which the handler already
// supports; only an exact date+site duplicate is refused. The form keeps its stricter
// one-site-per-day rule.

public sealed class SubmitWorkerWeekByNameAuthorisation
{
    // Same gate as SubmitWorkerWeekEndpoint's inline check.
    public bool Allows(SignedInUser user, SubmitWorkerWeekByName command) =>
        LabourRoleSets.ApproveTimesheets.IncludesAny(user.Roles);
}

public sealed class SubmitWorkerWeekByNameValidation
{
    // Mirror of SubmitWorkerWeekEndpoint's up-front checks: whole-command validation, so a bad
    // week is refused cleanly rather than half-landed. Deliberately NO cost-code requirement
    // (decision 2026-08-21) — see WorkerWeekDayEntry.
    public ValidationOutcome Check(SubmitWorkerWeekByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName))
            errors.Add("Worker name is required.");
        if (command.Days is not { Count: > 0 })
            errors.Add("At least one day with a site is required.");
        else
        {
            var weekStart = SiteClock.WorkDateOf(command.WeekStart);
            foreach (var day in command.Days)
            {
                var date = SiteClock.WorkDateOf(day.Date);
                if (date < weekStart || date >= weekStart.AddDays(7))
                    errors.Add($"{date:ddd dd MMM} is not in the week commencing {weekStart:dd MMM}.");
                if (string.IsNullOrWhiteSpace(day.ProjectId))
                    errors.Add($"{date:ddd dd MMM} needs a site — projectId comes from list_projects.");
                if (!LabourRules.IsValidHours(day.Hours))
                    errors.Add($"{date:ddd dd MMM}: hours must be in half-hour steps of at least 0.5.");
            }
            if (command.Days.GroupBy(day => (SiteClock.WorkDateOf(day.Date), day.ProjectId))
                    .Any(group => group.Count() > 1))
                errors.Add("Each site can only appear once per day — a day split across two sites is "
                    + "two entries with the same date and different sites.");
        }
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SubmitWorkerWeekByNameHandler : ICommandHandler<SubmitWorkerWeekByName, WorkerWeekResult>
{
    private readonly JpmsContext context;
    private readonly SubmitWorkerWeekHandler inner;
    public SubmitWorkerWeekByNameHandler(JpmsContext context, SubmitWorkerWeekHandler inner)
    { this.context = context; this.inner = inner; }

    public async Task<WorkerWeekResult> HandleAsync(SubmitWorkerWeekByName command, CancellationToken cancellationToken)
    {
        // Name → register resolution is shared with RecordWorkerAbsenceByName (WorkerNameResolver)
        // so the connector's by-name commands cannot drift in how they read a name.
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "logging time against them");
        return await inner.HandleAsync(
            new SubmitWorkerWeek(worker.WorkerId, command.WeekStart, command.Days),
            cancellationToken);
    }
}
