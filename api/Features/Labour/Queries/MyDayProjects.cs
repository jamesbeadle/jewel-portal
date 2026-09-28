using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The worker's project cards for today: every project they are assigned to, with today's
/// sign-in and sign-out times, the cost codes the day may be put against, and the note they have
/// already written today, if any.</summary>
public sealed class MyDayProjects
{
    private readonly JpmsContext context;
    private readonly MyDayCostCodes costCodes;
    private readonly MyDayNotesToday notesToday;

    public MyDayProjects(JpmsContext context, MyDayCostCodes costCodes, MyDayNotesToday notesToday)
    {
        this.context = context;
        this.costCodes = costCodes;
        this.notesToday = notesToday;
    }

    public async Task<IReadOnlyList<MyLabourProject>> ForAsync(
        WorkerEntity worker, string email, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var assigned = await AssignedAsync(worker, cancellationToken);
        var projectIds = assigned.Select(project => project.ProjectId).ToList();
        var attendanceToday = await context.SiteAttendances
            .Where(attendance => attendance.WorkerId == worker.WorkerId
                                 && attendance.WorkDate == today
                                 && projectIds.Contains(attendance.ProjectId))
            .ToListAsync(cancellationToken);
        var codesByProject = await costCodes.ByProjectAsync(projectIds, cancellationToken);
        var notesByProject = await notesToday.ByProjectAsync(email, projectIds, today, cancellationToken);

        return assigned.Select(project =>
        {
            var attendance = attendanceToday.FirstOrDefault(row => row.ProjectId == project.ProjectId);
            var note = notesByProject.GetValueOrDefault(project.ProjectId);
            return new MyLabourProject(
                project.ProjectId, project.ProjectName,
                attendance is not null, attendance?.SignedOutAt is not null, codesByProject[project.ProjectId],
                attendance?.SignedInAt, attendance?.SignedOutAt, note);
        }).ToList();
    }

    private Task<List<AssignedProject>> AssignedAsync(WorkerEntity worker, CancellationToken cancellationToken) =>
        context.ProjectWorkerAssignments
            .Where(assignment => assignment.WorkerId == worker.WorkerId && assignment.IsActive)
            .Join(context.Projects, assignment => assignment.ProjectId, project => project.ProjectId,
                (assignment, project) => new AssignedProject(project.ProjectId, project.Name))
            .OrderBy(project => project.ProjectName)
            .ToListAsync(cancellationToken);
}
