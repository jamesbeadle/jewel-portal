using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>Everything written about the worker between two dates — timesheets, notes, their
/// photo counts, attendances, the office's absences and the weeks sent for review — loaded once
/// and read day by day as the rows of a week or a month.</summary>
public sealed class MyDayRecords
{
    private readonly List<TimesheetEntity> timesheets;
    private readonly List<ProgressUpdateEntity> notes;
    private readonly IReadOnlyDictionary<string, int> photoCounts;
    private readonly List<SiteAttendanceEntity> attendances;
    private readonly IReadOnlyDictionary<DateTimeOffset, AbsenceKind> absences;
    private readonly HashSet<DateTimeOffset> lockedWeeks;

    private MyDayRecords(
        List<TimesheetEntity> timesheets, List<ProgressUpdateEntity> notes, IReadOnlyDictionary<string, int> photoCounts,
        List<SiteAttendanceEntity> attendances, IReadOnlyDictionary<DateTimeOffset, AbsenceKind> absences, HashSet<DateTimeOffset> lockedWeeks)
    {
        this.timesheets = timesheets;
        this.notes = notes;
        this.photoCounts = photoCounts;
        this.attendances = attendances;
        this.absences = absences;
        this.lockedWeeks = lockedWeeks;
    }

    public IEnumerable<string> ProjectIds =>
        timesheets.Select(row => row.ProjectId).Concat(notes.Select(row => row.ProjectId)).Distinct();

    public static async Task<MyDayRecords> LoadAsync(
        JpmsContext context, WorkerEntity worker, string email, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var timesheets = await context.Timesheets.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && row.WorkedOn >= from && row.WorkedOn <= to)
            .ToListAsync(cancellationToken);
        var notes = await context.ProgressUpdates.AsNoTracking()
            .Where(row => row.CreatedByEmail == email && row.WorkDate >= from && row.WorkDate <= to)
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        var photoCounts = await PhotoCountsAsync(context, notes.Select(note => note.ProgressUpdateId).ToList(), cancellationToken);
        var attendances = await context.SiteAttendances.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && row.WorkDate >= from && row.WorkDate <= to)
            .ToListAsync(cancellationToken);
        var absences = await context.WorkerAbsences.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && row.Date >= from && row.Date <= to)
            .ToDictionaryAsync(row => row.Date, row => (AbsenceKind)row.Kind, cancellationToken);
        var lockedWeeks = await WorkerWeekLock.LockedWeeksAsync(context, worker.WorkerId, from, to, cancellationToken);
        return new MyDayRecords(timesheets, notes, photoCounts, attendances, absences, lockedWeeks);
    }

    public MyWeekDay RowFor(AssignedProject site, DateTimeOffset day)
    {
        var note = notes.FirstOrDefault(row => row.ProjectId == site.ProjectId && row.WorkDate == day);
        var timesheet = timesheets.FirstOrDefault(row => row.ProjectId == site.ProjectId && row.WorkedOn == day);
        var isLocked = lockedWeeks.Contains(LabourWeeks.MondayOf(day));
        var plannedAbsence = absences.TryGetValue(day, out var absence) ? absence : (AbsenceKind?)null;
        if (timesheet is null)
        {
            var kind = note is not null && MyDayNotes.IsOffDay(note.Title) ? MyWeekDayKind.Off : MyWeekDayKind.Nothing;
            return new MyWeekDay(site.ProjectId, site.ProjectName, day, kind, Words: note?.Description ?? "",
                ProgressUpdateId: note?.ProgressUpdateId ?? "", IsInSubmittedWeek: isLocked, PlannedAbsence: plannedAbsence);
        }
        var attendance = attendances.FirstOrDefault(row => row.SiteAttendanceId == timesheet.SiteAttendanceId);
        return new MyWeekDay(
            site.ProjectId, site.ProjectName, day, MyWeekDayKind.Logged, timesheet.TimesheetId, timesheet.Hours, timesheet.CostCode,
            (TimesheetStatus)timesheet.Status, note?.Description ?? "", note is null ? 0 : photoCounts.GetValueOrDefault(note.ProgressUpdateId),
            attendance?.SignedOutAt, note?.ProgressUpdateId ?? "", timesheet.IsFiledLate, isLocked, plannedAbsence);
    }

    private static async Task<IReadOnlyDictionary<string, int>> PhotoCountsAsync(
        JpmsContext context, List<string> noteIds, CancellationToken cancellationToken) =>
        await context.ProgressPhotos.AsNoTracking()
            .Where(photo => noteIds.Contains(photo.ProgressUpdateId))
            .GroupBy(photo => photo.ProgressUpdateId)
            .Select(group => new { ProgressUpdateId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ProgressUpdateId, group => group.Count, cancellationToken);
}
