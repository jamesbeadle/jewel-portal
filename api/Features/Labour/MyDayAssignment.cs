using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The worker list is the gate onto a site's day: a worker logs, moves or records a day
/// only on a project whose active worker list carries them, and is told to ask their Project
/// Manager otherwise.</summary>
public static class MyDayAssignment
{
    public static async Task EnsureAssignedAsync(JpmsContext context, string projectId, WorkerEntity worker, CancellationToken cancellationToken)
    {
        var isAssigned = await context.ProjectWorkerAssignments.AnyAsync(
            assignment => assignment.ProjectId == projectId && assignment.WorkerId == worker.WorkerId && assignment.IsActive,
            cancellationToken);
        if (!isAssigned) throw new InvalidOperationException("You're not on this project's worker list — ask your Project Manager to add you.");
    }
}
