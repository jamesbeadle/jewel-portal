using Jewel.JPMS.Api.Features.Documents;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Section 1's shape: the five working days of the reporting week in order — Friday
/// first, carrying anything recorded over the weekend — each with the selected updates that
/// fall on it. Pure, so the shape is pinned by tests.</summary>
internal static class ContractorsReportDays
{
    public static IReadOnlyList<ContractorsReportDay> Group(ReportingWeek week, IReadOnlyList<ContractorsReportUpdate> updates) =>
        week.Days()
            .Where(IsWorkingDay)
            .Select(day => new ContractorsReportDay(day, Heading(day), EntriesOn(day, updates)))
            .ToList();

    public static string Heading(DateOnly day) => day.ToString("dddd d MMMM yyyy", JewelDocumentStyle.Uk);

    private static bool IsWorkingDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static IReadOnlyList<ContractorsReportEntry> EntriesOn(DateOnly day, IReadOnlyList<ContractorsReportUpdate> updates) =>
        updates
            .Where(update => ReportingDayOf(update.WorkDate) == day)
            .Select(update => new ContractorsReportEntry(update.ProgressUpdateId, update.Title, update.Description, update.Photos))
            .ToList();

    private static DateOnly ReportingDayOf(DateOnly workDate) => workDate.DayOfWeek switch
    {
        DayOfWeek.Saturday => workDate.AddDays(-1),
        DayOfWeek.Sunday => workDate.AddDays(-2),
        _ => workDate
    };
}
