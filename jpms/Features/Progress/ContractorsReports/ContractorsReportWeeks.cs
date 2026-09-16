namespace Jewel.JPMS.Features.Progress.ContractorsReports;

/// <summary>The reporting week ends on a Thursday; the register offers the most recent one.</summary>
public static class ContractorsReportWeeks
{
    public static DateOnly LastThursday(DateOnly today)
    {
        var day = today;
        while (day.DayOfWeek != DayOfWeek.Thursday) day = day.AddDays(-1);
        return day;
    }

    public static bool IsThursday(DateOnly day) => day.DayOfWeek == DayOfWeek.Thursday;
}
