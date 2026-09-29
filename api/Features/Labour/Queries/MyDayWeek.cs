using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The worker's week as My day lists it: Monday to today, one row per assigned site per
/// working day (a weekend day only when something was recorded on it) — logged, off, or nothing —
/// so the worker sees what they have and have not filed before Friday, and can amend a logged day
/// the office has not approved.</summary>
public sealed class MyDayWeek
{
    private readonly JpmsContext context;
    public MyDayWeek(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<MyWeekDay>> ForAsync(
        WorkerEntity worker, string email, IReadOnlyList<MyLabourProject> cards, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var monday = MondayOf(today);
        var timesheets = await context.Timesheets.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && row.WorkedOn >= monday && row.WorkedOn <= today)
            .ToListAsync(cancellationToken);
        var notes = await context.ProgressUpdates.AsNoTracking()
            .Where(row => row.CreatedByEmail == email && row.WorkDate >= monday && row.WorkDate <= today)
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        var photoCounts = await PhotoCountsAsync(notes.Select(note => note.ProgressUpdateId).ToList(), cancellationToken);
        var attendances = await context.SiteAttendances.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && row.WorkDate >= monday && row.WorkDate <= today)
            .ToListAsync(cancellationToken);
        return Days(monday, today)
            .SelectMany(day => cards.Select(card => RowFor(card, day, timesheets, notes, photoCounts, attendances)))
            .Where(row => row.Kind != MyWeekDayKind.Nothing || IsWorkingDay(row.Date))
            .ToList();
    }

    private static MyWeekDay RowFor(
        MyLabourProject card, DateTimeOffset day, List<TimesheetEntity> timesheets, List<ProgressUpdateEntity> notes,
        IReadOnlyDictionary<string, int> photoCounts, List<SiteAttendanceEntity> attendances)
    {
        var note = notes.FirstOrDefault(row => row.ProjectId == card.ProjectId && row.WorkDate == day);
        var timesheet = timesheets.FirstOrDefault(row => row.ProjectId == card.ProjectId && row.WorkedOn == day);
        if (timesheet is null)
        {
            var kind = note is not null && MyDayNotes.IsOffDay(note.Title) ? MyWeekDayKind.Off : MyWeekDayKind.Nothing;
            return new MyWeekDay(card.ProjectId, card.ProjectName, day, kind, Words: note?.Description ?? "");
        }
        var attendance = attendances.FirstOrDefault(row => row.SiteAttendanceId == timesheet.SiteAttendanceId);
        return new MyWeekDay(
            card.ProjectId, card.ProjectName, day, MyWeekDayKind.Logged, timesheet.TimesheetId, timesheet.Hours, timesheet.CostCode,
            (TimesheetStatus)timesheet.Status, note?.Description ?? "", note is null ? 0 : photoCounts.GetValueOrDefault(note.ProgressUpdateId),
            attendance?.SignedOutAt);
    }

    private async Task<IReadOnlyDictionary<string, int>> PhotoCountsAsync(List<string> noteIds, CancellationToken cancellationToken) =>
        await context.ProgressPhotos.AsNoTracking()
            .Where(photo => noteIds.Contains(photo.ProgressUpdateId))
            .GroupBy(photo => photo.ProgressUpdateId)
            .Select(group => new { ProgressUpdateId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ProgressUpdateId, group => group.Count, cancellationToken);

    private static IEnumerable<DateTimeOffset> Days(DateTimeOffset monday, DateTimeOffset today)
    {
        for (var day = monday; day <= today; day = day.AddDays(1)) yield return day;
    }

    private static DateTimeOffset MondayOf(DateTimeOffset today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysSinceMonday);
    }

    private static bool IsWorkingDay(DateTimeOffset day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
