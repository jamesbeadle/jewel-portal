namespace Jewel.JPMS.Services;

/// <summary>The worker's own weeks and months as My day pages through them. Reads answer from
/// what has landed; the panels ask for a week or a month to be loaded and show its arrival.</summary>
public interface IMyWeeksStore
{
    event Action? OnChange;

    MyLabourWeek? Week(DateTimeOffset weekStart);
    Task RefreshWeekAsync(DateTimeOffset weekStart);
    MyLabourMonth? Month(int year, int month);
    Task RefreshMonthAsync(int year, int month);
    /// <summary>Sends the week to the office and keeps the week as it now reads.</summary>
    Task<MyLabourWeek> SubmitWeekAsync(DateTimeOffset weekStart);
}
