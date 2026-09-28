namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>One worker's sign-in on one day of the reporting week — the daily log's site register
/// (<c>SiteAttendances</c>), named through the worker record and carrying the firm the worker
/// belongs to, so a firm's days on site come off the register rather than off memory.</summary>
public sealed record ContractorsReportSignIn(DateOnly Date, string WorkerName, string ContactEmail, string? SubcontractorId);

internal static class ContractorsReportSignIns
{
    public static async Task<IReadOnlyList<ContractorsReportSignIn>> InWeekAsync(
        JpmsContext context, string projectId, ReportingWeek week, CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(week.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(week.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rows = await context.SiteAttendances.AsNoTracking()
            .Where(row => row.ProjectId == projectId && row.WorkDate >= from && row.WorkDate < to)
            .Join(context.Workers.AsNoTracking(), row => row.WorkerId, worker => worker.WorkerId,
                (row, worker) => new { row.WorkDate, worker.Name, worker.ContactEmail, worker.SubcontractorId })
            .OrderBy(row => row.WorkDate).ThenBy(row => row.Name)
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new ContractorsReportSignIn(DateOnly.FromDateTime(row.WorkDate.UtcDateTime), row.Name, row.ContactEmail, row.SubcontractorId))
            .ToList();
    }
}
