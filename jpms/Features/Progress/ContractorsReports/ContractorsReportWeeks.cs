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

    /// <summary>The report's working days, Friday then Monday to Thursday — the days a firm can be ticked on site.</summary>
    public static IReadOnlyList<DateOnly> WorkingDays(DateOnly periodStart, DateOnly periodEnd)
    {
        var days = new List<DateOnly>();
        for (var day = periodStart; day <= periodEnd; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) days.Add(day);
        }
        return days;
    }
}
