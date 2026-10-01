using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The worker's days between two dates, one row per site per working day (a weekend day
/// only when something was recorded on it) — logged, off, or nothing — with the office's planned
/// absence on the day and whether the week is with the office. The sites are the ones the worker
/// is on the list for today and any site a day in the range was recorded on, so an earlier week
/// on a site they have since left still reads.</summary>
public sealed class MyDayDays
{
    private readonly JpmsContext context;
    public MyDayDays(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<MyWeekDay>> BetweenAsync(
        WorkerEntity worker, string email, IReadOnlyList<AssignedProject> assigned,
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var records = await MyDayRecords.LoadAsync(context, worker, email, from, to, cancellationToken);
        var sites = await SitesAsync(assigned, records, cancellationToken);
        return LabourWeeks.Between(from, to)
            .SelectMany(day => sites.Select(site => records.RowFor(site, day)))
            .Where(row => row.Kind != MyWeekDayKind.Nothing || LabourWeeks.IsWorkingDay(row.Date))
            .ToList();
    }

    private async Task<IReadOnlyList<AssignedProject>> SitesAsync(
        IReadOnlyList<AssignedProject> assigned, MyDayRecords records, CancellationToken cancellationToken)
    {
        var assignedIds = assigned.Select(site => site.ProjectId).ToHashSet();
        var otherIds = records.ProjectIds.Where(projectId => !assignedIds.Contains(projectId)).ToList();
        if (otherIds.Count == 0) return assigned;
        var others = await context.Projects.AsNoTracking()
            .Where(project => otherIds.Contains(project.ProjectId))
            .OrderBy(project => project.Name)
            .Select(project => new AssignedProject(project.ProjectId, project.Name))
            .ToListAsync(cancellationToken);
        return assigned.Concat(others).ToList();
    }
}
