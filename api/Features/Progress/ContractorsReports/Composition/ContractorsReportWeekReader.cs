using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// The week day by day for the person composing: who filed each day (a worker's daily log, a
/// recorded day off, or an office note), who signed in on site and filed nothing, and the
/// photographs each day holds. A filing is a worker's when its author's email is a worker
/// record's; anyone else is named by their email.
/// </summary>
internal static class ContractorsReportWeekReader
{
    public static async Task<IReadOnlyList<ContractorsReportWeekDay>> ReadAsync(
        JpmsContext context, ReportingWeek week, IReadOnlyList<ContractorsReportUpdate> updates,
        IReadOnlyList<ContractorsReportSignIn> signIns, IReadOnlyList<string> selectedUpdateIds, CancellationToken cancellationToken)
    {
        var authors = updates.Select(update => update.CreatedByEmail).Distinct().ToList();
        var workerNames = await context.Workers.AsNoTracking()
            .Where(worker => authors.Contains(worker.ContactEmail))
            .ToDictionaryAsync(worker => worker.ContactEmail, worker => worker.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        return Days(week, updates, signIns, selectedUpdateIds, workerNames);
    }

    public static IReadOnlyList<ContractorsReportWeekDay> Days(
        ReportingWeek week, IReadOnlyList<ContractorsReportUpdate> updates, IReadOnlyList<ContractorsReportSignIn> signIns,
        IReadOnlyList<string> selectedUpdateIds, IReadOnlyDictionary<string, string> workerNames) =>
        week.Days()
            .Select(day => DayOf(day, updates, signIns, selectedUpdateIds, workerNames))
            .ToList();

    private static ContractorsReportWeekDay DayOf(
        DateOnly day, IReadOnlyList<ContractorsReportUpdate> updates, IReadOnlyList<ContractorsReportSignIn> signIns,
        IReadOnlyList<string> selectedUpdateIds, IReadOnlyDictionary<string, string> workerNames)
    {
        var filed = updates
            .Where(update => update.WorkDate == day)
            .Select(update => FilingOf(update, selectedUpdateIds, workerNames))
            .ToList();
        var authorsThatFiled = updates
            .Where(update => update.WorkDate == day)
            .Select(update => update.CreatedByEmail)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var onSiteNotFiled = signIns
            .Where(signIn => signIn.Date == day && !authorsThatFiled.Contains(signIn.ContactEmail))
            .Select(signIn => signIn.WorkerName)
            .Distinct()
            .ToList();
        return new ContractorsReportWeekDay(day, filed, onSiteNotFiled);
    }

    private static ContractorsReportFiling FilingOf(
        ContractorsReportUpdate update, IReadOnlyList<string> selectedUpdateIds, IReadOnlyDictionary<string, string> workerNames)
    {
        var isWorkers = workerNames.TryGetValue(update.CreatedByEmail, out var workerName);
        var filedBy = isWorkers ? workerName! : update.CreatedByEmail;
        return new ContractorsReportFiling(update.ProgressUpdateId, filedBy, KindOf(update, isWorkers), update.Photos.Count,
            selectedUpdateIds.Contains(update.ProgressUpdateId));
    }

    private static ContractorsReportFilingKind KindOf(ContractorsReportUpdate update, bool isWorkers)
    {
        if (MyDayNotes.IsOffDay(update.Title)) return ContractorsReportFilingKind.OffDay;
        return isWorkers ? ContractorsReportFilingKind.DailyLog : ContractorsReportFilingKind.Note;
    }
}
