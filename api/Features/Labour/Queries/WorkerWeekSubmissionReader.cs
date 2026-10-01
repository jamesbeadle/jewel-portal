using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>A submitted week as the office reads it: the row, the worker's name, and the week's
/// days as the worker filed them.</summary>
public sealed class WorkerWeekSubmissionReader
{
    private readonly JpmsContext context;
    private readonly MyDayDays days;
    public WorkerWeekSubmissionReader(JpmsContext context, MyDayDays days) { this.context = context; this.days = days; }

    public async Task<WorkerWeekSubmission> ReadAsync(WorkerWeekSubmissionEntity submission, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.AsNoTracking()
            .FirstAsync(row => row.WorkerId == submission.WorkerId, cancellationToken);
        return await ReadAsync(submission, worker, cancellationToken);
    }

    public async Task<WorkerWeekSubmission> ReadAsync(WorkerWeekSubmissionEntity submission, WorkerEntity worker, CancellationToken cancellationToken)
    {
        var sunday = submission.WeekStart.AddDays(LabourWeeks.DaysInWeek - 1);
        var assigned = await AssignedToAsync(worker, cancellationToken);
        var rows = await days.BetweenAsync(worker, worker.ContactEmail, assigned, submission.WeekStart, sunday, cancellationToken);
        return WorkerWeekSubmissions.ToModel(submission, worker.Name, rows);
    }

    private Task<List<AssignedProject>> AssignedToAsync(WorkerEntity worker, CancellationToken cancellationToken) =>
        context.ProjectWorkerAssignments.AsNoTracking()
            .Where(assignment => assignment.WorkerId == worker.WorkerId && assignment.IsActive)
            .Join(context.Projects, assignment => assignment.ProjectId, project => project.ProjectId, (assignment, project) => project)
            .OrderBy(project => project.Name)
            .Select(project => new AssignedProject(project.ProjectId, project.Name))
            .ToListAsync(cancellationToken);
}
