using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>When a worker may send a week to the office: the week has elapsed to Friday, no
/// submission is still waiting or signed off, and every working day in it is accounted for — logged
/// on some site, recorded off by the worker, or planned off by the office. The refusal names the
/// first day that is not, so the worker knows which day to fill in.</summary>
public static class MyWeekSubmissionRules
{
    private const string DayFormat = "dddd d MMMM";

    public static string? Refusal(DateTimeOffset monday, DateTimeOffset today, IReadOnlyList<MyWeekDay> days, MyWeekSubmission? submission)
    {
        if (submission is { IsWithTheOffice: true }) return "This week is with the office for review.";
        if (submission is { IsSignedOff: true }) return "The office has signed this week off.";
        var friday = monday.AddDays(LabourWeeks.WorkingDaysInWeek - 1);
        if (friday > today) return "A week is submitted once Friday is logged.";
        var unaccounted = LabourWeeks.WorkingDaysOf(monday).FirstOrDefault(day => !IsAccountedFor(day, days));
        if (unaccounted != default) return $"{unaccounted.ToString(DayFormat)} has nothing logged — fill it in or record it off first.";
        return null;
    }

    private static bool IsAccountedFor(DateTimeOffset day, IReadOnlyList<MyWeekDay> days) =>
        days.Any(row => row.Date == day && (row.IsLogged || row.Kind == MyWeekDayKind.Off || row.IsPlannedOff));
}
