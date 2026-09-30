using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>When a day is filed against when it was worked. Today's day is filed from the site
/// card; a day already gone may still be filled in or recorded off from the week, within the
/// late-filing window, and anything written after the working day is marked late for the office.</summary>
public static class MyDayFiling
{
    public static bool IsLate(DateTimeOffset workDate, DateTimeOffset today) => today > workDate;

    public static DateTimeOffset ResolveWorkDate(DateTimeOffset? given, DateTimeOffset today)
    {
        if (given is not { } moment) return today;
        var workDate = SiteClock.WorkDateOf(moment);
        if (workDate == today) return today;
        return PastWorkDate(workDate, today);
    }

    public static DateTimeOffset PastWorkDate(DateTimeOffset workDate, DateTimeOffset today)
    {
        var isOpen = LabourRules.IsOpenForLateFiling(workDate, today);
        if (!isOpen) throw new InvalidOperationException(TooLateOrTooSoon(workDate, today));
        return workDate;
    }

    private static string TooLateOrTooSoon(DateTimeOffset workDate, DateTimeOffset today) =>
        workDate >= today
            ? "Today is logged from your site card, and a day still to come can't be filed yet."
            : $"That day is more than {LabourRules.LateFilingWindow.Days} days ago — ask your Project Manager to add it.";
}
